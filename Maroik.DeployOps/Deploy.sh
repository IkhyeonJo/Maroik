#!/bin/bash

# Strip TypeScript debug artifacts from the custom client scripts before deploy.
# site.js.map files and the trailing `//# sourceMappingURL=` comment are produced only by
# `npm run build:debug` / `npm run watch` for local Chrome DevTools debugging and must never
# ship. Release builds (tsconfig.build.json, used by `npm run build`, the .csproj target and
# the Docker tsbuild stage) already omit them; this is the safety net for a checkout that was
# built in debug mode on the box. See Maroik.Website/TypeScripts/README.md.
WWWROOT="/home/ubuntu/Maroik/Maroik.Website/wwwroot"
if [ -d "$WWWROOT" ]; then
  find "$WWWROOT" -path '*/custom/*' -name '*.js.map' -type f -delete
  find "$WWWROOT" -path '*/custom/*' -name '*.js' -type f -exec \
    sed -i '/^\/\/# sourceMappingURL=/d' {} +
fi

# Target upload directories
paths=(
  "/home/ubuntu/Maroik/Maroik.Website/wwwroot/upload"
  "/home/ubuntu/Maroik/Maroik.FileStorage/upload"
)

for path in "${paths[@]}"; do
  # Remove executable permission from all files (set to 644: rw-r--r--)
  find "$path" -type f -exec chmod 644 {} \;

  # Set 755 permission on all directories (rwxr-xr-x)
  find "$path" -type d -exec chmod 755 {} \;
done

# Set Maroik Logs
mkdir -p /home/ubuntu/Maroik/Maroik.Log
chmod 755 /home/ubuntu/Maroik/Maroik.Log

# apt upgrade auto restart services
sudo sed -i 's/#$nrconf{restart} = '"'"'i'"'"';/$nrconf{restart} = '"'"'a'"'"';/g' /etc/needrestart/needrestart.conf

# Modify logind.conf to ignore lid switch (only once: every deploy reruns this script)
if ! grep -q '^HandleLidSwitch=ignore$' /etc/systemd/logind.conf; then
  echo "HandleLidSwitch=ignore" | sudo tee -a /etc/systemd/logind.conf
  sudo systemctl restart systemd-logind
fi

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Remove what the upgrade replaced (old kernels, their modules and headers in / and /boot, orphaned
# dependencies) and the downloaded .deb files; apt keeps them all otherwise, a little more on every deploy.
sudo apt autoremove --purge -y
sudo apt clean

## Enable HTTPS nextcloud
#sudo nextcloud.enable-https self-signed

## Change nextcloud ports
#sudo snap set nextcloud ports.http=8079
#sudo snap set nextcloud ports.https=8080

# Install Docker Compose
sudo apt install docker-compose -y

# Set permissions for scripts
chmod 755 /home/ubuntu/Maroik/Maroik.Crontab/*.sh

# check .env.prod file
ENV_PROD_PATH="/home/ubuntu/Maroik/.env.prod"
if [ ! -f "$ENV_PROD_PATH" ]; then
  echo "[INFO] File $ENV_PROD_PATH not found."
  echo "[INFO] Please create .env.prod file with required environment variables."
  exit 1
fi

# check the Cloudflare Tunnel token: cloudflared is the site's only way in (no port is published)
if ! grep -q '^TUNNEL_TOKEN=.' "$ENV_PROD_PATH"; then
  echo "[INFO] TUNNEL_TOKEN is not set in $ENV_PROD_PATH."
  echo "[INFO] Copy the connector token from Cloudflare Zero Trust > Networks > Tunnels."
  exit 1
fi

# check Init.sql file
SQL_PROD_PATH="/home/ubuntu/Maroik/Maroik.DB/PostgreSQL/SQL_Init_Script/Production/Init.sql"
if [ ! -f "$SQL_PROD_PATH" ]; then
  echo "[INFO] File $SQL_PROD_PATH not found."
  exit 1
fi

# Read external drive configuration from .env.prod
EXTERNAL_DRIVE_UUID=$(grep "^EXTERNAL_DRIVE_UUID=" "$ENV_PROD_PATH" | cut -d '=' -f2)
EXTERNAL_DRIVE_MOUNT=$(grep "^EXTERNAL_DRIVE_MOUNT=" "$ENV_PROD_PATH" | cut -d '=' -f2)
BACKUP_BASE_DIR=$(grep "^BACKUP_BASE_DIR=" "$ENV_PROD_PATH" | cut -d '=' -f2)

# Set default value if not specified
EXTERNAL_DRIVE_MOUNT="${EXTERNAL_DRIVE_MOUNT:-/home/ubuntu/512GB}"

# Check if external drive UUID is configured
if [ -z "$EXTERNAL_DRIVE_UUID" ] || [ "$EXTERNAL_DRIVE_UUID" == "<CHANGE_ME>" ]; then
  echo "[INFO] External drive UUID not configured in .env.prod"
  exit 1
else
  # Create mount point directory
  sudo mkdir -p "$EXTERNAL_DRIVE_MOUNT"

  # Check if external drive is already mounted
  if mountpoint -q "$EXTERNAL_DRIVE_MOUNT"; then
    echo "[INFO] External drive already mounted at $EXTERNAL_DRIVE_MOUNT"
  else
    # Try to mount the external drive
    echo "[INFO] Mounting external drive with UUID: $EXTERNAL_DRIVE_UUID"
    sudo mount UUID="$EXTERNAL_DRIVE_UUID" "$EXTERNAL_DRIVE_MOUNT"

    # Verify mount succeeded
    if ! mountpoint -q "$EXTERNAL_DRIVE_MOUNT"; then
      echo "[ERROR] Failed to mount external drive"
      echo "[ERROR] Please connect the external drive with UUID: $EXTERNAL_DRIVE_UUID"
      echo "[ERROR] Check device connection and run: sudo blkid"
      exit 1
    fi
    echo "[INFO] External drive mounted successfully at $EXTERNAL_DRIVE_MOUNT"
  fi

  if [ -z "$BACKUP_BASE_DIR" ]; then
    echo "[INFO] BACKUP_BASE_DIR not configured in .env.prod"
    exit 1
  fi

  # Create backup directory (Backup.sh stores Maroik-Backup-YYYY-MM-DD-HH-MM-SS.tar.xz here)
  sudo mkdir -p "$BACKUP_BASE_DIR"
fi

# Start Docker Compose
sudo docker-compose -f /home/ubuntu/Maroik/docker-compose.prod.yml up -d

# The images are built and running: drop the build cache and the intermediate build-stage images
# (sdk / node stages) they leave behind. Images in use by the running containers are kept.
sudo docker builder prune -af
sudo docker image prune -f

# Set up cron jobs
sudo crontab /home/ubuntu/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo systemctl reload cron

# No inbound port at all: the site is reached only through the Cloudflare Tunnel, which
# cloudflared opens outbound (docker-compose.prod.yml), and the server is managed physically.
sudo apt install ufw -y

echo "[+] Resetting firewall rules..."
sudo ufw --force reset
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Ping (ICMP Echo Request) block
if ! grep -q '^net.ipv4.icmp_echo_ignore_all = 1' /etc/sysctl.conf; then
  sudo tee -a /etc/sysctl.conf > /dev/null <<EOF

# Ping block (ICMP Echo Request ignore)
net.ipv4.icmp_echo_ignore_all = 1
EOF
fi

# Apply sysctl settings
sudo sysctl -p

# Enable UFW
sudo ufw --force enable

# Adds the current user to the "docker" group,
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

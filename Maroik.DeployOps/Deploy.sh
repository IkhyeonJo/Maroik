#!/bin/bash

# Target upload directories
paths=(
  "/home/ubuntu/Maroik/Maroik.WebSite/wwwroot/upload"
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

# Modify logind.conf to ignore lid switch
echo "HandleLidSwitch=ignore" | sudo tee -a /etc/systemd/logind.conf
sudo systemctl restart systemd-logind

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

## Enable HTTPS nextcloud
#sudo nextcloud.enable-https self-signed

## Change nextcloud ports
#sudo snap set nextcloud ports.http=8079
#sudo snap set nextcloud ports.https=8080

# Install Docker Compose
sudo apt install docker-compose -y

# Set permissions for scripts
chmod 755 /home/ubuntu/Maroik/Maroik.Crontab/*.sh

CERT_PATH="/home/ubuntu/Maroik/Maroik.SSL/conf"
TARGET_SCRIPT="/home/ubuntu/Maroik/Maroik.Crontab/CertbotScript.sh"
PARAM="issue_wildcard"

# check directory
if [ ! -d "$CERT_PATH" ]; then
  echo "[INFO] Directory $CERT_PATH not found."
  echo "[INFO] Please run $TARGET_SCRIPT with parameter '$PARAM'... manually."
  exit 1
fi

# check .env.prod file
ENV_PROD_PATH="/home/ubuntu/Maroik/.env.prod"
if [ ! -f "$ENV_PROD_PATH" ]; then
  echo "[INFO] File $ENV_PROD_PATH not found."
  echo "[INFO] Please create .env.prod file with required environment variables."
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

  # Create backup directory structure
  sudo mkdir -p "$BACKUP_BASE_DIR/DB"
  sudo mkdir -p "$BACKUP_BASE_DIR/Files"
fi

# Start Docker Compose
sudo docker-compose -f /home/ubuntu/Maroik/docker-compose.prod.yml up -d

# Set up cron jobs
sudo crontab /home/ubuntu/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo systemctl reload cron

# only port 80, 443, [5001, 8080] allowed by ufw
sudo apt install ufw -y
sudo ufw --force reset
sudo ufw default deny incoming
sudo ufw default allow outgoing

CF_IPV4=$(curl -s https://www.cloudflare.com/ips-v4)
CF_IPV6=$(curl -s https://www.cloudflare.com/ips-v6)

# Cloudflare IPv4 allow for ports 80, 443, [5001, 8080]
for ip in $CF_IPV4; do
  sudo ufw allow from $ip to any port 80 proto tcp
  sudo ufw allow from $ip to any port 443 proto tcp
  #sudo ufw allow from $ip to any port 5001 proto tcp
  #sudo ufw allow from $ip to any port 8080 proto tcp
done

# Cloudflare IPv6 allow for ports 80, 443, [5001, 8080]
for ip in $CF_IPV6; do
  sudo ufw allow from $ip to any port 80 proto tcp
  sudo ufw allow from $ip to any port 443 proto tcp
  #sudo ufw allow from $ip to any port 5001 proto tcp
  #sudo ufw allow from $ip to any port 8080 proto tcp
done

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

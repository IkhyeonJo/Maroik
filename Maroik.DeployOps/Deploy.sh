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

  # An empty value would turn the paths below into /DB and /Files at the filesystem root.
  if [ -z "$BACKUP_BASE_DIR" ]; then
    echo "[INFO] BACKUP_BASE_DIR not configured in .env.prod"
    exit 1
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

# Fetch and validate the Cloudflare ranges BEFORE resetting the firewall. `ufw --force reset`
# wipes every existing rule, so a failed/truncated download here must not leave the box on
# "default deny incoming" with no Cloudflare allowance until the next fix -- same rationale as
# Maroik.Crontab/UpdateCloudflareUfw.sh (which reuses this exact fetch-then-reset ordering and,
# via its own 3AM cron entry, will retry this setup later if it's skipped here).
CF_IPV4_URL="https://www.cloudflare.com/ips-v4"
CF_IPV6_URL="https://www.cloudflare.com/ips-v6"

fetch() {
  # --fail: non-2xx is an error (not a 0-byte "success"); --retry: ride out a brief blip.
  curl --fail --silent --show-error --location \
       --retry 3 --retry-delay 5 --max-time 30 "$1"
}

echo "[+] Fetching Cloudflare IP ranges..."
CF_IPV4_LIST=$(fetch "$CF_IPV4_URL") || { echo "[ERROR] Failed to fetch $CF_IPV4_URL."; CF_IPV4_LIST=""; }
CF_IPV6_LIST=$(fetch "$CF_IPV6_URL") || { echo "[ERROR] Failed to fetch $CF_IPV6_URL."; CF_IPV6_LIST=""; }

# Keep only lines that look like an IPv4/IPv6 CIDR; drop anything else (HTML error page, blanks).
CF_RANGES=$(printf '%s\n%s\n' "$CF_IPV4_LIST" "$CF_IPV6_LIST" | grep -E '^[0-9a-fA-F:.]+/[0-9]{1,3}$' || true)

if [ -z "$CF_RANGES" ]; then
  echo "[ERROR] Downloaded list held no CIDR ranges; leaving ufw untouched. Run Maroik.Crontab/UpdateCloudflareUfw.sh manually once connectivity is confirmed."
else
  RANGE_COUNT=$(printf '%s\n' "$CF_RANGES" | wc -l)
  if [ "$RANGE_COUNT" -lt 5 ]; then
    echo "[ERROR] Only $RANGE_COUNT Cloudflare ranges parsed (expected ~22); looks truncated, leaving ufw untouched. Run Maroik.Crontab/UpdateCloudflareUfw.sh manually once connectivity is confirmed."
  else
    echo "[+] $RANGE_COUNT Cloudflare ranges validated."

    echo "[+] Removing old firewall rules..."
    sudo ufw --force reset
    sudo ufw default deny incoming
    sudo ufw default allow outgoing

    echo "[+] Adding Cloudflare IPs..."
    while IFS= read -r ip; do
      [ -z "$ip" ] && continue
      sudo ufw allow from "$ip" to any port 80 proto tcp
      sudo ufw allow from "$ip" to any port 443 proto tcp
      #sudo ufw allow from "$ip" to any port 5001 proto tcp
      #sudo ufw allow from "$ip" to any port 8080 proto tcp
    done <<< "$CF_RANGES"

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
  fi
fi

# Adds the current user to the "docker" group,
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

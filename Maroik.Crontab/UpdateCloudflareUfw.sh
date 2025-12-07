#!/bin/bash

# Read .env.prod file
ENV_FILE="/home/ubuntu/Maroik/.env.prod"

if [ ! -f "$ENV_FILE" ]; then
  echo "[ERROR] Environment file $ENV_FILE not found."
  exit 1
fi

# Read Cloudflare URLs from .env.prod
CF_IPV4_URL=$(grep '^CLOUDFLARE_IPV4_URL=' "$ENV_FILE" | cut -d '=' -f2)
CF_IPV6_URL=$(grep '^CLOUDFLARE_IPV6_URL=' "$ENV_FILE" | cut -d '=' -f2)

# Set defaults if not found
if [ -z "$CF_IPV4_URL" ]; then
  CF_IPV4_URL="https://www.cloudflare.com/ips-v4"
fi
if [ -z "$CF_IPV6_URL" ]; then
  CF_IPV6_URL="https://www.cloudflare.com/ips-v6"
fi

echo "[+] Removing old Cloudflare rules..."
sudo ufw --force reset
sudo ufw default deny incoming
sudo ufw default allow outgoing

echo "[+] Adding Cloudflare IPs..."
for ip in $(curl -s $CF_IPV4_URL) $(curl -s $CF_IPV6_URL); do
    sudo ufw allow from $ip to any port 80 proto tcp
    sudo ufw allow from $ip to any port 443 proto tcp
    #sudo ufw allow from $ip to any port 5001 proto tcp
    #sudo ufw allow from $ip to any port 8080 proto tcp
done

sudo ufw --force enable

echo "[+] Cloudflare UFW rules updated."

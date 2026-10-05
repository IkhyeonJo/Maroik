#!/bin/bash
#
# Rebuilds the UFW ruleset so only Cloudflare's published edge ranges may reach ports 80/443.
#
# NOTE: `ufw --force reset` wipes every existing rule. The Cloudflare ranges are therefore
# fetched and sanity-checked BEFORE the reset — a failed or truncated download aborts the run
# with the firewall left untouched, instead of resetting to "deny all incoming" with no
# Cloudflare allowance until the next cron run.

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

# --- Fetch and validate the ranges first --------------------------------------------------

# --fail: non-2xx is an error (not a 0-byte "success"); --retry: ride out a brief blip.
fetch() {
  curl --fail --silent --show-error --location \
       --retry 3 --retry-delay 5 --max-time 30 "$1"
}

echo "[+] Fetching Cloudflare IP ranges..."
CF_IPV4_LIST=$(fetch "$CF_IPV4_URL") || { echo "[ERROR] Failed to fetch $CF_IPV4_URL; firewall left unchanged."; exit 1; }
CF_IPV6_LIST=$(fetch "$CF_IPV6_URL") || { echo "[ERROR] Failed to fetch $CF_IPV6_URL; firewall left unchanged."; exit 1; }

# Keep only lines that look like an IPv4/IPv6 CIDR; drop anything else (HTML error page, blanks).
CF_RANGES=$(printf '%s\n%s\n' "$CF_IPV4_LIST" "$CF_IPV6_LIST" | grep -E '^[0-9a-fA-F:.]+/[0-9]{1,3}$' || true)

if [ -z "$CF_RANGES" ]; then
  echo "[ERROR] Downloaded list held no CIDR ranges; firewall left unchanged."
  exit 1
fi

RANGE_COUNT=$(printf '%s\n' "$CF_RANGES" | wc -l)
if [ "$RANGE_COUNT" -lt 5 ]; then
  echo "[ERROR] Only $RANGE_COUNT Cloudflare ranges parsed (expected ~22); looks truncated, firewall left unchanged."
  exit 1
fi

echo "[+] $RANGE_COUNT Cloudflare ranges validated."

# --- Rebuild the ruleset ----------------------------------------------------------------

echo "[+] Removing old Cloudflare rules..."
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

sudo ufw --force enable

echo "[+] Cloudflare UFW rules updated."

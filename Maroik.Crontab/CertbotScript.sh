#!/bin/bash

DOMAIN="maroik.com"
EMAIL="***REMOVED***m"
PEM_DIR="/etc/letsencrypt/live/$DOMAIN"
TARGET_DIR="/home/peter-cho/Maroik/Maroik.SSL"

issue_wildcard() {
  docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml run --rm certbot certonly \
    --dns-route53 \
    --config-dir /etc/letsencrypt \
    --work-dir /etc/letsencrypt \
    --logs-dir /var/log/letsencrypt \
    -d $DOMAIN -d "*.$DOMAIN" \
    --non-interactive \
    --agree-tos \
    -m $EMAIL

  if [ -f "$PEM_DIR/fullchain.pem" ]; then
    cp "$PEM_DIR/fullchain.pem" "$TARGET_DIR/fullchain1.pem"
    cp "$PEM_DIR/privkey.pem"   "$TARGET_DIR/privkey1.pem"
    cp "$PEM_DIR/cert.pem"      "$TARGET_DIR/cert1.pem"
    cp "$PEM_DIR/chain.pem"     "$TARGET_DIR/chain1.pem"
    echo "PEM files moved to $TARGET_DIR"
  else
    echo "No certificate issued. Please check logs."
  fi
}

renew() {
  docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml run --rm certbot renew --dns-route53

  if [ -f "$PEM_DIR/fullchain.pem" ]; then
    cp "$PEM_DIR/fullchain.pem" "$TARGET_DIR/fullchain1.pem"
    cp "$PEM_DIR/privkey.pem"   "$TARGET_DIR/privkey1.pem"
    cp "$PEM_DIR/cert.pem"      "$TARGET_DIR/cert1.pem"
    cp "$PEM_DIR/chain.pem"     "$TARGET_DIR/chain1.pem"
    
    docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml restart
    
    echo "Renewed PEM files moved to $TARGET_DIR and docker-compose restarted"
  else
    echo "No certificate renewal required."
  fi
}

# Usage example:
# ./CertbotScript.sh issue_wildcard  (for issuing new certificate)
# ./CertbotScript.sh renew           (for renewing certificate)

case "$1" in
  issue_wildcard)
    issue_wildcard
    ;;
  renew)
    renew
    ;;
  *)
    echo "Usage: $0 {issue_wildcard|renew}"
    exit 1
    ;;
esac

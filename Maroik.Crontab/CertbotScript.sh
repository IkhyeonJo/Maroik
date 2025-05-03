#!/bin/bash

DOMAIN="maroik.com"
EMAIL="***REMOVED***m"
PEM_DIR="/home/peter-cho/Maroik/Maroik.SSL/conf/live/$DOMAIN"
TARGET_DIR="/home/peter-cho/Maroik/Maroik.SSL"
CONTAINER_NAMES=("Maroik.WebSite" "Maroik.WebAPI" "Maroik.FileStorage")
 
 # caution: --force-renewal
  
issue_wildcard() {
  docker-compose -f /home/peter-cho/Maroik/docker-compose.debug.yml run --rm certbot certonly \
    --dns-route53 \
    --config-dir /etc/letsencrypt \
    --work-dir /etc/letsencrypt \
    --logs-dir /var/log/letsencrypt \
    -d $DOMAIN -d "*.$DOMAIN" \
    --non-interactive \
    --agree-tos \
    -m $EMAIL \
    --force-renewal

  if [ -f "$PEM_DIR/fullchain.pem" ]; then
    echo "PEM files moved to $TARGET_DIR"
  else
    echo "No certificate issued. Please check logs."
  fi
}

renew() {
  docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml run --rm certbot renew --dns-route53 --force-renewal --deploy-hook "
    cp \$PEM_DIR/fullchain.pem \$TARGET_DIR/fullchain.pem &&
    cp \$PEM_DIR/privkey.pem \$TARGET_DIR/privkey.pem &&
    echo 'Renewed PEM files'
  "

#  docker restart ${CONTAINER_NAMES[@]}
#  echo 'Docker containers restarted'
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

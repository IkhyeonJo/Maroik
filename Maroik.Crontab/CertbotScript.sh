#!/bin/bash

DOMAIN="maroik.com"
EMAIL="***REMOVED***m"

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
}

renew() {
  docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml run --rm certbot renew --dns-route53 --deploy-hook "
    echo 'Renewed PEM files'
  "
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

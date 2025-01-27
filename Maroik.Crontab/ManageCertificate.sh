#!/bin/bash

CERT_DIR="/home/peter-cho/Maroik/Maroik.SSL/live/maroik.com"
DOMAIN="maroik.com"
EMAIL="***REMOVED***m"
SERVICES_TO_RESTART="Maroik.FileStorage Maroik.WebSite Maroik.WebAPI"

restart_services() {
    echo "Restarting related services..."
    for service in $SERVICES_TO_RESTART; do
        docker restart "$service"
    done
}

issue_certificate() {
    echo "No certificate found. Issuing a new certificate..."
    docker run --rm -v "/home/peter-cho/Maroik/Maroik.SSL:/etc/letsencrypt" \
        certbot/dns-route53 certonly --dns-route53 \
        --email "$EMAIL" --agree-tos --no-eff-email \
        -d "$DOMAIN" -d "*.$DOMAIN"
    restart_services
}

renew_certificate() {
    echo "Attempting to renew the certificate..."
    docker run --rm -v "/home/peter-cho/Maroik/Maroik.SSL:/etc/letsencrypt" \
        certbot/dns-route53 renew
    restart_services
}

check_and_manage_certificates() {
    if [ -d "$CERT_DIR" ]; then
        echo "Certificate directory exists. Checking and renewing if necessary..."
        renew_certificate
    else
        issue_certificate
    fi
}

check_and_manage_certificates

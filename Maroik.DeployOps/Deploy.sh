#!/bin/bash

# apt upgrade auto restart services
sudo sed -i 's/#$nrconf{restart} = '"'"'i'"'"';/$nrconf{restart} = '"'"'a'"'"';/g' /etc/needrestart/needrestart.conf

# Modify logind.conf to ignore lid switch
echo "HandleLidSwitch=ignore" | sudo tee -a /etc/systemd/logind.conf
sudo systemctl restart systemd-logind

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Install Docker Compose
sudo apt install docker-compose -y

# Start Docker Compose
sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml up -d

# Install Python 3
sudo apt install python3 -y

# Set permissions for scripts
chmod 777 /home/peter-cho/Maroik/Maroik.Crontab/*.sh

# Set up cron jobs
sudo crontab /home/peter-cho/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo systemctl reload cron

# Create directories
sudo mkdir -p /home/peter-cho/512GB/

# Mount devices by UUID
sudo mount -U 5CBA6F45BA6F1B32 /home/peter-cho/512GB/

# Adds the current user to the "docker" group, 
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

# Install Nextcloud
sudo apt install nextcloud -y

# Configure Nextcloud to use Port 8080 for HTTP and Port 8080 for HTTPS
sudo sed -i 's/Listen 80/Listen 8080/' /etc/apache2/ports.conf
sudo sed -i 's/Listen 443/Listen 8080/' /etc/apache2/ports.conf

# Install SSL certificate
sudo mkdir -p /etc/apache2/ssl
sudo cp /home/peter-cho/Maroik/Maroik.SSL/maroik.crt /etc/apache2/ssl/maroik.crt
sudo cp /home/peter-cho/Maroik/Maroik.SSL/maroik.key /etc/apache2/ssl/maroik.key

# Configure Apache with SSL
sudo tee /etc/apache2/sites-available/nextcloud_ssl.conf >/dev/null <<EOF
<IfModule mod_ssl.c>
    <VirtualHost *:8080>
        ServerName maroik.iptime.org

        SSLEngine on
        SSLCertificateFile /etc/apache2/ssl/maroik.crt
        SSLCertificateKeyFile /etc/apache2/ssl/maroik.key

        ProxyPreserveHost On
        ProxyPass / http://localhost/nextcloud/
        ProxyPassReverse / http://localhost/nextcloud/
    </VirtualHost>
</IfModule>
EOF

# Enable SSL module and Nextcloud SSL site
sudo a2enmod ssl
sudo a2ensite nextcloud_ssl

# Disable default Apache site
sudo a2dissite 000-default

# Restart Apache
sudo systemctl restart apache2

# Set up admin account
sudo nextcloud.manual-install admin dhdtnsdl12!
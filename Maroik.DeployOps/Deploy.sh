#!/bin/bash

# Set Maroik Logs
rm -rf /home/peter-cho/Maroik/Maroik.Log
mkdir -p /home/peter-cho/Maroik/Maroik.Log
chmod 777 /home/peter-cho/Maroik/Maroik.Log

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

# only port 80, 443, 5001, 8080 allowed by ufw
sudo apt install ufw -y
sudo ufw allow 80
sudo ufw allow 443
sudo ufw allow 5001
#sudo ufw allow 8080
sudo ufw enable

# Adds the current user to the "docker" group, 
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

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

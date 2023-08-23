#!/bin/bash

# run this script as default user

# apt upgrade auto restart services
sudo sed -i 's/#$nrconf{restart} = '"'"'i'"'"';/$nrconf{restart} = '"'"'a'"'"';/g' /etc/needrestart/needrestart.conf

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Install Docker Compose
sudo apt install docker-compose -y

# Start Docker Compose
sudo docker-compose -f ~/Maroik/docker-compose.prod.yml up -d

# Install Python 3
sudo apt install python3 -y

# Set permissions for scripts
chmod 777 ~/Maroik/Maroik.Crontab/*.sh

# Set up cron jobs
sudo crontab ~/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo systemctl reload cron

# Create directories
sudo mkdir -p ~/512GB/

# Mount devices by UUID
sudo mount -U 5CBA6F45BA6F1B32 ~/512GB/

# Adds the current user to the "docker" group, 
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

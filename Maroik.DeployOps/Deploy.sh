#!/bin/bash

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Install Docker Compose
sudo apt install docker-compose -y

# Start Docker Compose
sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.yml up -d

# Install Python 3
sudo apt install python3 -y

# Set permissions for scripts
sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/DBBackup.sh
sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/RebootDockerCompose.sh
sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh
sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/UploadFileBackUp.sh

# Set up cron jobs
sudo crontab /home/peter-cho/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo service cron reload

# Execute AutoMountExternalStorages.sh
/home/peter-cho/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh

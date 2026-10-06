#!/bin/bash

# Bring down the Docker containers defined in docker-compose.prod.yml
sudo docker-compose -f /home/ubuntu/Maroik/docker-compose.prod.yml down

# Stop and remove all Docker containers
sudo docker stop $(sudo docker ps -a -q)
sudo docker rm $(sudo docker ps -a -q)

# Remove all Docker volumes
sudo docker volume rm $(sudo docker volume ls -q)

# Optionally, remove all Docker images (use with caution)
sudo docker rmi $(sudo docker images -q)

# Remove current user's cron jobs
sudo crontab -r

# Disable ufw
sudo ufw disable

# unmount external drive
if [ -f /home/ubuntu/Maroik/.env.prod ]; then
    EXTERNAL_DRIVE_MOUNT=$(grep '^EXTERNAL_DRIVE_MOUNT=' /home/ubuntu/Maroik/.env.prod | cut -d '=' -f2)
    if [ -n "$EXTERNAL_DRIVE_MOUNT" ]; then
        sudo umount "$EXTERNAL_DRIVE_MOUNT"
    fi
fi
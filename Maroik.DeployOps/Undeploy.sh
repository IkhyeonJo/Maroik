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

# Remove what `docker rmi` leaves behind: the build cache of the three images Deploy.sh builds
# (NuGet restore, npm ci and publish layers — a new set on every deploy, never reused after the
# images are gone), dangling images and unused networks. Without this every deploy adds gigabytes
# that nothing ever frees, until the root disk fills up.
sudo docker builder prune -af
sudo docker system prune -af --volumes

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
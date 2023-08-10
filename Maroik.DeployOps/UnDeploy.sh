#!/bin/bash

# Bring down the Docker containers defined in docker-compose.yml
docker-compose -f "~/Maroik/docker-compose.yml" down

# Stop and remove all Docker containers
docker stop $(docker ps -a -q)
docker rm $(docker ps -a -q)

# Remove all Docker volumes
docker volume rm $(docker volume ls -q)

# Optionally, remove all Docker images (use with caution)
docker rmi $(docker images -q)

# Remove root user's cron jobs
sudo crontab -r

# Unmount devices by UUID
#sudo umount ~/32GB/
#sudo umount ~/64GB/
sudo umount ~/512GB/

# Remove directories
#sudo rmdir ~/32GB/
#sudo rmdir ~/64GB/
sudo rmdir ~/512GB/

# Remove Wake-on-LAN (WoL) configuration
sudo systemctl stop wol.service
sudo systemctl disable wol.service
sudo rm /etc/systemd/system/wol.service
sudo systemctl daemon-reload

# Uninstall net-tools, ethtool, and wakeonlan packages
sudo apt remove --purge net-tools ethtool wakeonlan -y
sudo apt autoremove -y

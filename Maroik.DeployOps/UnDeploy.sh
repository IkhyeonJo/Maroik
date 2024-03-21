#!/bin/bash

# Bring down the Docker containers defined in docker-compose.prod.yml
sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.prod.yml down

# Stop and remove all Docker containers
sudo docker stop $(docker ps -a -q)
sudo docker rm $(docker ps -a -q)

# Remove all Docker volumes
sudo docker volume rm $(docker volume ls -q)

# Optionally, remove all Docker images (use with caution)
sudo docker rmi $(docker images -q)

# Remove current user's cron jobs
sudo crontab -r
#!/bin/bash

# Bring down the Docker containers defined in docker-compose.yml
docker-compose -f "/home/peter-cho/Maroik/docker-compose.yml" down

# Stop and remove all Docker containers
docker stop $(docker ps -a -q)
docker rm $(docker ps -a -q)

# Remove all Docker volumes
docker volume rm $(docker volume ls -q)

# Optionally, remove all Docker images (use with caution)
docker rmi $(docker images -q)

# Remove root user's cron jobs
sudo crontab -r

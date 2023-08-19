#!/bin/bash

# Check if docker-compose is already installed
if command -v docker-compose &> /dev/null; then
    echo "docker-compose is already installed."
else
    # Install docker-compose
    echo "Installing docker-compose..."
    sudo apt update -y
    sudo apt install docker-compose -y

    # Run related to "docker" command without "sudo" command
    sudo chmod 666 /var/run/docker.sock

    # Verify installation
    if command -v docker-compose &> /dev/null; then
        echo "docker-compose installation successful."
    else
        echo "Failed to install docker-compose."
    fi
fi
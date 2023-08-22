#!/bin/bash

# Install .NET SDK if it's not installed
if command -v dotnet &> /dev/null; then
    echo ".NET SDK is already installed."
else
    # Install .NET SDK (replace '7.0' with the correct version)
    echo "Installing .NET SDK..."
    sudo apt install dotnet-sdk-7.0 -y
    
    # Verify installation
    if command -v dotnet &> /dev/null; then
        echo ".NET SDK installation successful."
    else
        echo "Failed to install .NET SDK."
    fi
fi

# Check if docker-compose is already installed
if command -v docker-compose &> /dev/null; then
    echo "docker-compose is already installed."
else
    # Install docker-compose
    echo "Installing docker-compose..."
    sudo apt update -y
    sudo apt install docker-compose -y

    # Verify installation
    if command -v docker-compose &> /dev/null; then
        echo "docker-compose installation successful."
    else
        echo "Failed to install docker-compose."
    fi
fi

# Run related to "docker" command without "sudo" command
sudo chmod 666 /var/run/docker.sock
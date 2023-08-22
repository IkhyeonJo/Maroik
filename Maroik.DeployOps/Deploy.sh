#!/bin/bash

# run this script as default user

# apt upgrade auto restart services
sudo sed -i 's/#$nrconf{restart} = '"'"'i'"'"';/$nrconf{restart} = '"'"'a'"'"';/g' /etc/needrestart/needrestart.conf

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Install UFW (if not already installed):
sudo apt install ufw -y

# Enable and Start UFW service:
sudo systemctl enable ufw
sudo systemctl start ufw

# Configure UFW:
# Replace enp1s0f2 with your server's actual network interface name. You can use ip a to determine the interface name.
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow in on enp1s0f2 from 192.168.0.0/16 to any port 22 proto tcp
sudo ufw allow in on enp1s0f2 from 10.0.0.0/8 to any port 22 proto tcp
sudo ufw allow in on enp1s0f2 from 172.16.0.0/12 to any port 22 proto tcp

# Allow incoming traffic on ports 80 (HTTP), 443 (HTTPS), and 5001 (REST API)
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 5001/tcp

# Enable UFW:
echo "y" | sudo ufw enable

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

# Execute AutoMountExternalStorages.sh
~/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh

# # Set up Wake-on-LAN (WoL)
# sudo apt install net-tools ethtool wakeonlan -y
# # Replace 'enp1s0' with your actual Ethernet interface name
# sudo ethtool -s enp1s0 wol g

# # Create and enable WoL service
# echo "[Unit]
# Description=Configure Wake-up on LAN

# [Service]
# Type=oneshot
# ExecStart=/sbin/ethtool -s enp1s0 wol g

# [Install]
# WantedBy=basic.target" | sudo tee /etc/systemd/system/wol.service

# # Enable and start the WoL service
# sudo systemctl daemon-reload
# sudo systemctl enable wol.service
# sudo systemctl start wol.service

# Adds the current user to the "docker" group, 
sudo usermod -a -G docker $USER

# Starts a new shell session with the "docker" group applied.
newgrp docker

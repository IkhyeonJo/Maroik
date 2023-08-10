#!/bin/bash

# Update and upgrade system
sudo apt update -y
sudo apt upgrade -y

# Install Docker Compose
sudo apt install docker-compose -y

# Start Docker Compose
sudo docker-compose -f ~/Maroik/docker-compose.yml up -d

# Install Python 3
sudo apt install python3 -y

# Set permissions for scripts
chmod 755 ~/Maroik/Maroik.Crontab/*.sh

# Set up cron jobs
sudo crontab ~/Maroik/Maroik.Crontab/InitCron

# Reload cron service
sudo systemctl reload cron

# Execute AutoMountExternalStorages.sh
~/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh

# Set up Wake-on-LAN (WoL)
sudo apt install net-tools ethtool wakeonlan -y
# Replace 'enp1s0' with your actual Ethernet interface name
sudo ethtool -s enp1s0 wol g

# Create and enable WoL service
echo "[Unit]
Description=Configure Wake-up on LAN

[Service]
Type=oneshot
ExecStart=/sbin/ethtool -s enp1s0 wol g

[Install]
WantedBy=basic.target" | sudo tee /etc/systemd/system/wol.service

# Enable and start the WoL service
sudo systemctl daemon-reload
sudo systemctl enable wol.service
sudo systemctl start wol.service

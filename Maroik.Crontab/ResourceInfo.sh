#!/bin/bash

SHARED_DIR="/home/peter-cho/Maroik/Maroik.Log"
HOST_OUTPUT_FILE="$SHARED_DIR/HostResourceInfo.txt"
DOCKER_OUTPUT_FILE="$SHARED_DIR/DockerResourceInfo.txt"

# Host CPU Information
echo "Host CPU Information:" > $HOST_OUTPUT_FILE
CPU_USAGE=$(awk -v INTERVAL=1 '
    BEGIN { getline; split($0, prev, " "); total_prev = 0; idle_prev = prev[5]; for (i = 2; i <= NF; i++) total_prev += prev[i] }
    { getline; split($0, curr, " "); total_curr = 0; idle_curr = curr[5]; for (i = 2; i <= NF; i++) total_curr += curr[i] }
    END { print 100 - ((idle_curr - idle_prev) * 100 / (total_curr - total_prev)) }
' <(grep 'cpu ' /proc/stat) <(sleep 1; grep 'cpu ' /proc/stat))
echo "CPU Usage: $CPU_USAGE%" >> $HOST_OUTPUT_FILE

# Host Memory Information (Total / Usage)
echo "Host Memory Information:" >> $HOST_OUTPUT_FILE
MEMORY_USAGE=$(free -h | awk '/^Mem:/ {print $2 " / " $3}')
echo "Memory: $MEMORY_USAGE" >> $HOST_OUTPUT_FILE

# Host Disk Information (Total / Usage)
echo "Host Disk Information:" >> $HOST_OUTPUT_FILE
DISK_USAGE=$(df -h --total | awk '/^total/ {print $2 " / " $3}')
echo "Disk: $DISK_USAGE" >> $HOST_OUTPUT_FILE

# Docker container status and resource usage information
echo "Docker Container Status:" > $DOCKER_OUTPUT_FILE
docker ps --format "table {{.Names}}\t{{.Status}}" >> $DOCKER_OUTPUT_FILE

echo "Docker Resource Usage:" >> $DOCKER_OUTPUT_FILE
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" >> $DOCKER_OUTPUT_FILE

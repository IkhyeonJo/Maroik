#!/bin/bash

SHARED_DIR="/home/peter-cho/Maroik/Maroik.Log"
HOST_OUTPUT_FILE="$SHARED_DIR/HostResourceInfo.txt"
DOCKER_OUTPUT_FILE="$SHARED_DIR/DockerResourceInfo.txt"

echo "Host CPU Information:" > $HOST_OUTPUT_FILE
CPU_MODEL=$(grep -m 1 'model name' /proc/cpuinfo | cut -d: -f2 | sed 's/^[ \t]*//')
echo "CPU Model: $CPU_MODEL" >> $HOST_OUTPUT_FILE

CPU_USAGE=$(awk -v INTERVAL=1 '
    BEGIN { getline; split($0, prev, " "); total_prev = 0; idle_prev = prev[5]; for (i = 2; i <= NF; i++) total_prev += prev[i] }
    { getline; split($0, curr, " "); total_curr = 0; idle_curr = curr[5]; for (i = 2; i <= NF; i++) total_curr += curr[i] }
    END { print 100 - ((idle_curr - idle_prev) * 100 / (total_curr - total_prev)) }
' <(grep 'cpu ' /proc/stat) <(sleep 1; grep 'cpu ' /proc/stat))
echo "CPU Usage: $CPU_USAGE%" >> $HOST_OUTPUT_FILE

echo "Host Memory Information:" >> $HOST_OUTPUT_FILE
free -h >> $HOST_OUTPUT_FILE

echo "Host Disk Information:" >> $HOST_OUTPUT_FILE
df -h >> $HOST_OUTPUT_FILE

echo "Host Network Information:" >> $HOST_OUTPUT_FILE
ip a >> $HOST_OUTPUT_FILE

echo "Docker Container Status:" > $DOCKER_OUTPUT_FILE
docker ps >> $DOCKER_OUTPUT_FILE

echo "Docker Resource Usage:" >> $DOCKER_OUTPUT_FILE
docker stats --no-stream >> $DOCKER_OUTPUT_FILE

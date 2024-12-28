#!/bin/bash

SHARED_DIR="/home/peter-cho/Maroik/Maroik.Log"
HOST_OUTPUT_FILE="$SHARED_DIR/HostResourceInfo.txt"
DOCKER_OUTPUT_FILE="$SHARED_DIR/DockerResourceInfo.txt"

echo "Host CPU Information:" > $HOST_OUTPUT_FILE
CPU_MODEL=$(grep -m 1 'model name' /proc/cpuinfo | cut -d: -f2 | sed 's/^[ \t]*//')
echo "CPU Model: $CPU_MODEL" >> $HOST_OUTPUT_FILE

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

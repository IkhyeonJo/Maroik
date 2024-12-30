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

# Initial network stats (store RX/TX values)
INITIAL_NETWORK_STATS=$(ip -s link)

# Wait for 10 seconds (increase measurement window)
sleep 10
FINAL_NETWORK_STATS=$(ip -s link)

# Extract RX/TX values using awk and ensure we get clean numbers
INITIAL_RX=$(echo "$INITIAL_NETWORK_STATS" | awk '/RX:/ {getline; print $1}' | tr -d -c '0-9')
INITIAL_TX=$(echo "$INITIAL_NETWORK_STATS" | awk '/TX:/ {getline; print $1}' | tr -d -c '0-9')
FINAL_RX=$(echo "$FINAL_NETWORK_STATS" | awk '/RX:/ {getline; print $1}' | tr -d -c '0-9')
FINAL_TX=$(echo "$FINAL_NETWORK_STATS" | awk '/TX:/ {getline; print $1}' | tr -d -c '0-9')

# Remove leading zeroes from extracted values (if any)
INITIAL_RX=$(echo "$INITIAL_RX" | sed 's/^0*//')
FINAL_RX=$(echo "$FINAL_RX" | sed 's/^0*//')
INITIAL_TX=$(echo "$INITIAL_TX" | sed 's/^0*//')
FINAL_TX=$(echo "$FINAL_TX" | sed 's/^0*//')

# Ensure the values are valid numbers and perform the subtraction
if [[ "$INITIAL_RX" =~ ^[0-9]+$ ]] && [[ "$FINAL_RX" =~ ^[0-9]+$ ]]; then
  RX_DIFF=$((FINAL_RX - INITIAL_RX))
else
  RX_DIFF=0
fi

if [[ "$INITIAL_TX" =~ ^[0-9]+$ ]] && [[ "$FINAL_TX" =~ ^[0-9]+$ ]]; then
  TX_DIFF=$((FINAL_TX - INITIAL_TX))
else
  TX_DIFF=0
fi

# Convert to Mbps (1 MB = 1024*1024 bytes, and 1 byte = 8 bits)
RX_MBPS=$(echo "scale=2; ($RX_DIFF * 8) / 1024 / 1024" | bc)
TX_MBPS=$(echo "scale=2; ($TX_DIFF * 8) / 1024 / 1024" | bc)

# Format values to 2 decimal places, rounding if necessary
RX_MBPS=$(printf "%.2f" "$RX_MBPS")
TX_MBPS=$(printf "%.2f" "$TX_MBPS")

# Ensure the TX value is at least 0.00 if it is very small
if (( $(echo "$TX_MBPS < 0.01" | bc -l) )); then
  TX_MBPS="0.00"
fi

echo "Host Network Information:" >> $HOST_OUTPUT_FILE
echo "Network: RX: ${RX_MBPS} Mbps / TX: ${TX_MBPS} Mbps" >> $HOST_OUTPUT_FILE

# Docker container status and resource usage information
echo "Docker Container Status:" > $DOCKER_OUTPUT_FILE
docker ps --format "table {{.Names}}\t{{.Status}}" >> $DOCKER_OUTPUT_FILE

echo "Docker Resource Usage:" >> $DOCKER_OUTPUT_FILE
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" >> $DOCKER_OUTPUT_FILE

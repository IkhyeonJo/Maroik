#!/bin/bash

# =============================================================================
# Maroik Upload Files Backup Script
# =============================================================================
# Purpose: Backup WebSite/FileStorage upload folders to local, then copy to external drive (YYYYMMDD folder)
# External drive capacity check: Delete oldest backup when usage exceeds 50%
# =============================================================================

set -e

# Configuration
ENV_FILE="/home/ubuntu/Maroik/.env.prod"

# Check .env.prod file exists
if [ ! -f "$ENV_FILE" ]; then
    echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - .env.prod file not found: $ENV_FILE"
    exit 1
fi

# Read configuration from .env.prod
MAROIK_BASE_DIR=$(grep "^MAROIK_BASE_DIR=" "$ENV_FILE" | cut -d '=' -f2)
BACKUP_BASE_DIR=$(grep "^BACKUP_BASE_DIR=" "$ENV_FILE" | cut -d '=' -f2)
EXTERNAL_DRIVE_MOUNT=$(grep "^EXTERNAL_DRIVE_MOUNT=" "$ENV_FILE" | cut -d '=' -f2)

# Set defaults
MAROIK_BASE_DIR="${MAROIK_BASE_DIR:-/home/ubuntu/Maroik}"
EXTERNAL_DRIVE_MOUNT="${EXTERNAL_DRIVE_MOUNT:-/home/ubuntu/512GB}"

# Step 1: Backup files to local directory
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Starting file backup to local directory..."

CURRENT_DATETIME=$(date '+%Y%m%d_%H%M%S')

# Clean up old temporary tar.xz files in Maroik base directory
if [ -d "$MAROIK_BASE_DIR" ]; then
    find "$MAROIK_BASE_DIR" -maxdepth 1 -name "*.tar.xz" -type f -delete 2>/dev/null || true
    find "$MAROIK_BASE_DIR" -maxdepth 1 -type d -name "website-upload-*" -exec rm -rf {} + 2>/dev/null || true
    find "$MAROIK_BASE_DIR" -maxdepth 1 -type d -name "file-storage-upload-*" -exec rm -rf {} + 2>/dev/null || true
fi

# Backup Website Upload Files
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Backing up WebSite upload files..."

WEBSITE_BACKUP_NAME="website-upload-${CURRENT_DATETIME}"
WEBSITE_TEMP_DIR="$MAROIK_BASE_DIR/$WEBSITE_BACKUP_NAME"
WEBSITE_BACKUP_FILE="$MAROIK_BASE_DIR/${WEBSITE_BACKUP_NAME}.tar.xz"

# Copy files from container
sudo docker cp Maroik.WebSite:/app/wwwroot/upload "$WEBSITE_TEMP_DIR" 2>/dev/null || true

if [ -d "$WEBSITE_TEMP_DIR" ]; then
    # Compress
    tar cfJ "$WEBSITE_BACKUP_FILE" -C "$MAROIK_BASE_DIR" "$WEBSITE_BACKUP_NAME"

    # Remove temp directory
    rm -rf "$WEBSITE_TEMP_DIR"

    WEBSITE_SIZE=$(du -h "$WEBSITE_BACKUP_FILE" | cut -f1)
    echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - WebSite upload backup completed"
    echo "[INFO] Local backup file: $WEBSITE_BACKUP_FILE"
    echo "[INFO] Backup size: $WEBSITE_SIZE"
else
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - WebSite upload directory not found or empty"
fi

# Backup FileStorage Upload Files
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Backing up FileStorage upload files..."

FILESTORAGE_BACKUP_NAME="file-storage-upload-${CURRENT_DATETIME}"
FILESTORAGE_TEMP_DIR="$MAROIK_BASE_DIR/$FILESTORAGE_BACKUP_NAME"
FILESTORAGE_BACKUP_FILE="$MAROIK_BASE_DIR/${FILESTORAGE_BACKUP_NAME}.tar.xz"

# Copy files from container
sudo docker cp Maroik.FileStorage:/app/upload "$FILESTORAGE_TEMP_DIR" 2>/dev/null || true

if [ -d "$FILESTORAGE_TEMP_DIR" ]; then
    # Compress
    tar cfJ "$FILESTORAGE_BACKUP_FILE" -C "$MAROIK_BASE_DIR" "$FILESTORAGE_BACKUP_NAME"

    # Remove temp directory
    rm -rf "$FILESTORAGE_TEMP_DIR"

    FILESTORAGE_SIZE=$(du -h "$FILESTORAGE_BACKUP_FILE" | cut -f1)
    echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - FileStorage upload backup completed"
    echo "[INFO] Local backup file: $FILESTORAGE_BACKUP_FILE"
    echo "[INFO] Backup size: $FILESTORAGE_SIZE"
else
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - FileStorage upload directory not found or empty"
fi

# Step 2: Copy to external drive (if configured and mounted)
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Preparing to copy backups to external drive..."

if [ -z "$BACKUP_BASE_DIR" ]; then
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - BACKUP_BASE_DIR not configured, skipping external drive backup"
    exit 1
fi

if ! mountpoint -q "$EXTERNAL_DRIVE_MOUNT"; then
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - External drive not mounted: $EXTERNAL_DRIVE_MOUNT"
    echo "[INFO] Local backups completed but not copied to external drive"
    exit 1
fi

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Copying backups to external drive..."

# Create backup base directory if not exists
FILES_BACKUP_DIR="$BACKUP_BASE_DIR/Files"
if [ ! -d "$FILES_BACKUP_DIR" ]; then
    mkdir -p "$FILES_BACKUP_DIR"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Created Files backup directory: $FILES_BACKUP_DIR"
fi

# Check disk usage (50% threshold)
USAGE=$(df "$EXTERNAL_DRIVE_MOUNT" | tail -1 | awk '{print $5}' | sed 's/%//')
THRESHOLD=50

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - External drive disk usage: ${USAGE}%"

if [ "$USAGE" -gt "$THRESHOLD" ]; then
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - Disk usage exceeds ${THRESHOLD}%"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Cleaning up old backups..."

    # Find and delete oldest backup folder
    OLDEST=$(find "$FILES_BACKUP_DIR" -maxdepth 1 -type d -name "[0-9]*" | sort | head -1)

    if [ -n "$OLDEST" ]; then
        echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Deleting oldest backup: $OLDEST"
        rm -rf "$OLDEST"
        echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Cleanup completed"
    else
        echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - No old backups found to delete"
    fi
fi

# Create date folder on external drive (YYYYMMDD format for folder)
CURRENT_DATE=$(date '+%Y%m%d')
EXTERNAL_BACKUP_FOLDER="$FILES_BACKUP_DIR/$CURRENT_DATE"
if [ ! -d "$EXTERNAL_BACKUP_FOLDER" ]; then
    mkdir -p "$EXTERNAL_BACKUP_FOLDER"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Created backup folder: $EXTERNAL_BACKUP_FOLDER"
fi

# Copy backup files to external drive
COPY_SUCCESS=true

if [ -f "$WEBSITE_BACKUP_FILE" ]; then
    cp "$WEBSITE_BACKUP_FILE" "$EXTERNAL_BACKUP_FOLDER/"
    if [ $? -eq 0 ]; then
        echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - WebSite backup copied to external drive"
        rm -f "$WEBSITE_BACKUP_FILE"
    else
        echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - Failed to copy WebSite backup to external drive"
        COPY_SUCCESS=false
    fi
fi

if [ -f "$FILESTORAGE_BACKUP_FILE" ]; then
    cp "$FILESTORAGE_BACKUP_FILE" "$EXTERNAL_BACKUP_FOLDER/"
    if [ $? -eq 0 ]; then
        echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - FileStorage backup copied to external drive"
        rm -f "$FILESTORAGE_BACKUP_FILE"
    else
        echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - Failed to copy FileStorage backup to external drive"
        COPY_SUCCESS=false
    fi
fi

if [ "$COPY_SUCCESS" = true ]; then
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - All backups copied successfully"
    echo "[INFO] External backup location: $EXTERNAL_BACKUP_FOLDER"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Local backup files cleaned up"
else
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - Some backups failed to copy to external drive"
    echo "[INFO] Local backup files kept in: $MAROIK_BASE_DIR"
fi

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - File backup script completed"

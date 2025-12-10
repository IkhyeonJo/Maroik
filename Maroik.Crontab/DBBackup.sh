#!/bin/bash

# =============================================================================
# Maroik PostgreSQL Database Backup Script
# =============================================================================
# Purpose: Backup PostgreSQL DB to local, then copy to external drive (YYYYMMDD folder)
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
POSTGRES_USER=$(grep "^POSTGRES_USER=" "$ENV_FILE" | cut -d '=' -f2)
POSTGRES_PASSWORD=$(grep "^POSTGRES_PASSWORD=" "$ENV_FILE" | cut -d '=' -f2)
POSTGRES_DB=$(grep "^POSTGRES_DB=" "$ENV_FILE" | cut -d '=' -f2)
MAROIK_BASE_DIR=$(grep "^MAROIK_BASE_DIR=" "$ENV_FILE" | cut -d '=' -f2)
BACKUP_BASE_DIR=$(grep "^BACKUP_BASE_DIR=" "$ENV_FILE" | cut -d '=' -f2)
EXTERNAL_DRIVE_MOUNT=$(grep "^EXTERNAL_DRIVE_MOUNT=" "$ENV_FILE" | cut -d '=' -f2)

# Set defaults
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_DB="${POSTGRES_DB:-Maroik}"
MAROIK_BASE_DIR="${MAROIK_BASE_DIR:-/home/ubuntu/Maroik}"
EXTERNAL_DRIVE_MOUNT="${EXTERNAL_DRIVE_MOUNT:-/home/ubuntu/512GB}"

# Step 1: Backup database to local directory
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Starting PostgreSQL backup to local directory..."

CURRENT_DATETIME=$(date '+%Y%m%d_%H%M%S')
LOCAL_BACKUP_FILE="$MAROIK_BASE_DIR/maroik-db-${CURRENT_DATETIME}.sql"

# Clean up old temporary SQL files in Maroik base directory
if [ -d "$MAROIK_BASE_DIR" ]; then
    find "$MAROIK_BASE_DIR" -maxdepth 1 -name "*.sql" -type f -delete 2>/dev/null || true
    find "$MAROIK_BASE_DIR" -maxdepth 1 -name "*.sql.gz" -type f -delete 2>/dev/null || true
fi

# Escape special characters in password
POSTGRES_PASSWORD_ESCAPED=$(echo "$POSTGRES_PASSWORD" | sed 's/!/\\!/g' | sed 's/@/\\@/g')

# Create backup using docker exec
sudo docker exec -i Maroik.PostgreSQL /bin/bash -c \
    "PGPASSWORD='${POSTGRES_PASSWORD_ESCAPED}' pg_dump --username ${POSTGRES_USER} \"${POSTGRES_DB}\"" \
    > "$LOCAL_BACKUP_FILE"

if [ $? -ne 0 ]; then
    echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - Database backup failed"
    exit 1
fi

# Get backup file size
BACKUP_SIZE=$(du -h "$LOCAL_BACKUP_FILE" | cut -f1)
echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - Database backup completed"
echo "[INFO] Local backup file: $LOCAL_BACKUP_FILE"
echo "[INFO] Backup size: $BACKUP_SIZE"

# Compress backup
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Compressing backup..."
gzip -f "$LOCAL_BACKUP_FILE"
COMPRESSED_FILE="${LOCAL_BACKUP_FILE}.gz"
COMPRESSED_SIZE=$(du -h "$COMPRESSED_FILE" | cut -f1)
echo "[INFO] Compressed file: $COMPRESSED_FILE"
echo "[INFO] Compressed size: $COMPRESSED_SIZE"

# Step 2: Copy to external drive (if configured and mounted)
echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Preparing to copy backup to external drive..."

if [ -z "$BACKUP_BASE_DIR" ]; then
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - BACKUP_BASE_DIR not configured, skipping external drive backup"
    exit 1
fi

if ! mountpoint -q "$EXTERNAL_DRIVE_MOUNT"; then
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - External drive not mounted: $EXTERNAL_DRIVE_MOUNT"
    echo "[INFO] Local backup completed but not copied to external drive"
    exit 1
fi

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Copying backup to external drive..."

# Create backup base directory if not exists
DB_BACKUP_DIR="$BACKUP_BASE_DIR/DB"
if [ ! -d "$DB_BACKUP_DIR" ]; then
    mkdir -p "$DB_BACKUP_DIR"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Created DB backup directory: $DB_BACKUP_DIR"
fi

# Check disk usage (50% threshold)
USAGE=$(df "$EXTERNAL_DRIVE_MOUNT" | tail -1 | awk '{print $5}' | sed 's/%//')
THRESHOLD=50

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - External drive disk usage: ${USAGE}%"

if [ "$USAGE" -gt "$THRESHOLD" ]; then
    echo "[WARNING] $(date '+%Y-%m-%d %H:%M:%S') - Disk usage exceeds ${THRESHOLD}%"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Cleaning up old backups..."

    # Find and delete oldest backup folder
    OLDEST=$(find "$DB_BACKUP_DIR" -maxdepth 1 -type d -name "[0-9]*" | sort | head -1)

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
EXTERNAL_BACKUP_FOLDER="$DB_BACKUP_DIR/$CURRENT_DATE"
if [ ! -d "$EXTERNAL_BACKUP_FOLDER" ]; then
    mkdir -p "$EXTERNAL_BACKUP_FOLDER"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Created backup folder: $EXTERNAL_BACKUP_FOLDER"
fi

# Copy compressed backup to external drive
cp "$COMPRESSED_FILE" "$EXTERNAL_BACKUP_FOLDER/"

if [ $? -eq 0 ]; then
    echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - Backup copied to external drive"
    echo "[INFO] External backup location: $EXTERNAL_BACKUP_FOLDER/$(basename $COMPRESSED_FILE)"

    # Clean up local backup file
    rm -f "$COMPRESSED_FILE"
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - Local backup file cleaned up"
else
    echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - Failed to copy backup to external drive"
    echo "[INFO] Local backup file kept at: $COMPRESSED_FILE"
    exit 1
fi

echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - DB backup script completed"

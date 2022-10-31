#! /bin/bash

python3 - << 'EOF'

import os

try:
    os.system('sudo apt update -y && sudo apt upgrade -y && sudo apt install docker-compose -y && sudo docker-compose up -d && sudo apt install python3 -y && sudo chmod 755 DBBackup.sh && sudo chmod 755 RebootDockerCompose.sh && sudo chmod 755 AutoMountExternalStorages.sh && sudo chmod 755 UploadFileBackUp.sh && sudo crontab InitCron && sudo service cron reload && ./AutoMountExternalStorages.sh')
except:
    pass

EOF

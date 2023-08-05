#! /bin/bash

python3 - << 'EOF'

import os

try:
    os.system('sudo apt update -y && sudo apt upgrade -y && sudo apt install docker-compose -y && sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.yml up -d && sudo apt install python3 -y && sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/DBBackup.sh && sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/RebootDockerCompose.sh && sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh && sudo chmod 755 /home/peter-cho/Maroik/Maroik.Crontab/UploadFileBackUp.sh && sudo crontab /home/peter-cho/Maroik/Maroik.Crontab/InitCron && sudo service cron reload && /home/peter-cho/Maroik/Maroik.Crontab/AutoMountExternalStorages.sh')
except:
    pass

EOF

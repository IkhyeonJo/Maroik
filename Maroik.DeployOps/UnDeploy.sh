#! /bin/bash

python3 - << 'EOF'

import os

try:
    os.system('sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.yml down && sudo docker volume rm $(docker volume ls -q) && sudo docker rmi -f $(docker images -a -q) && sudo docker rm -vf $(docker ps -a -q) && sudo crontab -r')
except:
    pass

EOF

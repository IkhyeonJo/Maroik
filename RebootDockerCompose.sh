#! /bin/bash

python3 - << 'EOF'

import os

os.system('sudo docker-compose -f /home/ec2-user/Maroik/docker-compose.yml up -d')

EOF

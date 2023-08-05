#! /bin/bash

python3 - << 'EOF'

import os

os.system('sudo docker-compose -f /home/peter-cho/Maroik/docker-compose.yml up -d')

EOF

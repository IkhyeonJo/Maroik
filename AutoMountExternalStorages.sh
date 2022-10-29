#! /bin/bash

python3 - << 'EOF'

import os

try:
    os.system('sudo mkdir /home/ec2-user/32GB/')
except:
    pass

try:
    os.system('sudo mkdir /home/ec2-user/64GB/')
except:
    pass

try:
    os.system('sudo mkdir /home/ec2-user/512GB/')
except:
    pass

try:
    os.system('sudo mount -U 08767C7F767C6F76 /home/ec2-user/32GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
except:
    pass

try:
    os.system('sudo mount -U 88B6AD06B6ACF5BA /home/ec2-user/64GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
except:
    pass

try:
    os.system('sudo mount -U 829C7DD19C7DBFE9 /home/ec2-user/512GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
except:
    pass

EOF

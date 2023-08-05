#! /bin/bash

python3 - << 'EOF'

import os

#try:
#    os.system('sudo mkdir /home/peter-cho/32GB/')
#except:
#    pass

#try:
#    os.system('sudo mkdir /home/peter-cho/64GB/')
#except:
#    pass

try:
    os.system('sudo mkdir /home/peter-cho/512GB/')
except:
    pass

#try:
#    os.system('sudo mount -U 6E98950E9894D5C5 /home/peter-cho/32GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
#except:
#    pass

#try:
#    os.system('sudo mount -U 88B6AD06B6ACF5BA /home/peter-cho/64GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
#except:
#    pass

try:
    os.system('sudo mount -U 5CBA6F45BA6F1B32 /home/peter-cho/512GB/') # To check each device's UUID, please type [sudo blkid] command in linux terminal.
except:
    pass

EOF

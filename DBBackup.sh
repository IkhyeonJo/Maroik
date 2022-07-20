#! /bin/bash

python3 - << 'EOF'

import os

os.system('sudo docker exec MyLaboratory.MariaDB /usr/bin/mysqldump -u root --password=***REMOVED*** MyLaboratory > /home/ec2-user/MyLaboratory/SQLBackup.sql')

EOF

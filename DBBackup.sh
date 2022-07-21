#! /bin/bash

python3 - << 'EOF'

import glob
import os
import smtplib
from email.encoders import encode_base64
from email.header import Header
from email.mime.base import MIMEBase
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.utils import formatdate
from datetime import datetime

garbage_sql_files = glob.glob('*.sql')
for garbage_sql_file in garbage_sql_files:
    os.remove(f'/home/ec2-user/MyLaboratory/{garbage_sql_file}')

current_date = str(datetime.today().strftime("%Y%m%d"))
backup_sql_file_name = f'Init-{current_date}.sql'
os.system(f'sudo docker exec MyLaboratory.MariaDB /usr/bin/mysqldump -u root --password=***REMOVED*** MyLaboratory > /home/ec2-user/MyLaboratory/{backup_sql_file_name}')

with open(f'/home/ec2-user/MyLaboratory/{backup_sql_file_name}', 'r', encoding='utf-8') as original: data = original.read()
new_add_line = '''DROP DATABASE IF EXISTS MyLaboratory;

CREATE DATABASE MyLaboratory default charset=utf8mb4 collate utf8mb4_unicode_ci;

USE MyLaboratory;

'''
with open(f'/home/ec2-user/MyLaboratory/{backup_sql_file_name}', 'w', encoding='utf-8') as modified: modified.write(new_add_line + data)

msg = MIMEMultipart()

msg['From'] = '***REMOVED***'
msg['To'] = '***REMOVED***'
msg['Date'] = formatdate(localtime=True)
msg['Subject'] = Header(s=f'MyLaboratory MariaDB SQL Backup ({current_date})', charset='utf-8')
body = MIMEText(f'MyLaboratory MariaDB SQL Backup ({current_date})', _charset='utf-8')
msg.attach(body)

files = list()
files.append(f'/home/ec2-user/MyLaboratory/{backup_sql_file_name}')

for f in files:
    part = MIMEBase('application', "octet-stream")
    part.set_payload(open(f, "rb").read())
    encode_base64(part)
    part.add_header('Content-Disposition', 'attachment; filename="%s"' % os.path.basename(f))
    msg.attach(part)

mailServer = smtplib.SMTP_SSL('smtp.naver.com')
mailServer.login('***REMOVED***', '***REMOVED***')
mailServer.send_message(msg)
mailServer.quit()

EOF


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

# Read .env.prod file
env_file_path = '/home/ubuntu/Maroik/.env.prod'
env_vars = {}

if not os.path.exists(env_file_path):
    print(f'[ERROR] Environment file {env_file_path} not found.')
    exit(1)

with open(env_file_path, 'r') as f:
    for line in f:
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            key, value = line.split('=', 1)
            env_vars[key] = value

# Get configuration values
postgres_user = env_vars.get('POSTGRES_USER', 'postgres')
postgres_password = env_vars.get('POSTGRES_PASSWORD', '')
postgres_db = env_vars.get('POSTGRES_DB', 'Maroik')
backup_email_from = env_vars.get('BACKUP_EMAIL_FROM', '')
backup_email_to = env_vars.get('BACKUP_EMAIL_TO', '')
backup_email_password = env_vars.get('BACKUP_EMAIL_PASSWORD', '')
backup_smtp_host = env_vars.get('BACKUP_SMTP_HOST', 'smtp.naver.com')

garbage_sql_files = glob.glob('/home/ubuntu/Maroik/*.sql')
for garbage_sql_file in garbage_sql_files:
    os.remove(garbage_sql_file)

current_date = str(datetime.today().strftime("%Y%m%d"))
backup_sql_file_name = f'Init-{current_date}.sql'

# Escape special characters for shell command
postgres_password_escaped = postgres_password.replace('!', '\!')
postgres_password_escaped = postgres_password_escaped.replace('@', '\@')

os.system(f'sudo docker exec -i Maroik.PostgreSQL /bin/bash -c "PGPASSWORD={postgres_password_escaped} pg_dump --username {postgres_user} \\"{postgres_db}\\"" > /home/ubuntu/Maroik/{backup_sql_file_name}')

msg = MIMEMultipart()

msg['From'] = backup_email_from
msg['To'] = backup_email_to
msg['Date'] = formatdate(localtime=True)
msg['Subject'] = Header(s=f'Maroik PostgreSQL SQL Backup ({current_date})', charset='utf-8')
body = MIMEText(f'Maroik PostgreSQL SQL Backup ({current_date})', _charset='utf-8')
msg.attach(body)

files = list()
files.append(os.path.expanduser(f'/home/ubuntu/Maroik/{backup_sql_file_name}'))

for f in files:
    part = MIMEBase('application', "octet-stream")
    part.set_payload(open(f, "rb").read())
    encode_base64(part)
    part.add_header('Content-Disposition', 'attachment; filename="%s"' % os.path.basename(f))
    msg.attach(part)

mailServer = smtplib.SMTP_SSL(backup_smtp_host)
mailServer.login(backup_email_from, backup_email_password)
mailServer.send_message(msg)
mailServer.quit()

EOF

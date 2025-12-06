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
maroik_base_dir = env_vars.get('MAROIK_BASE_DIR', '/home/ubuntu/Maroik')
file_backup_email_from = env_vars.get('FILE_BACKUP_EMAIL_FROM', '')
file_backup_email_to = env_vars.get('FILE_BACKUP_EMAIL_TO', '')
file_backup_email_password = env_vars.get('FILE_BACKUP_EMAIL_PASSWORD', '')
file_backup_smtp_host = env_vars.get('FILE_BACKUP_SMTP_HOST', 'smtp.naver.com')

garbage_tar_xz_files = glob.glob(f'{maroik_base_dir}/*.tar.xz')
for garbage_tar_xz_file in garbage_tar_xz_files:
    os.remove(garbage_tar_xz_file)

current_date = str(datetime.today().strftime("%Y%m%d"))
website_backup_upload_file_name = f'website-upload-{current_date}'
os.system(f'sudo docker cp Maroik.WebSite:/app/wwwroot/upload {maroik_base_dir}/{website_backup_upload_file_name}')
os.system(f'sudo tar cfJ {maroik_base_dir}/{website_backup_upload_file_name}.tar.xz {maroik_base_dir}/{website_backup_upload_file_name}')
os.system(f'sudo rm -rf {maroik_base_dir}/{website_backup_upload_file_name}')

file_storage_backup_upload_file_name = f'file-storage-upload-{current_date}'
os.system(f'sudo docker cp Maroik.FileStorage:/app/upload {maroik_base_dir}/{file_storage_backup_upload_file_name}')
os.system(f'sudo tar cfJ {maroik_base_dir}/{file_storage_backup_upload_file_name}.tar.xz {maroik_base_dir}/{file_storage_backup_upload_file_name}')
os.system(f'sudo rm -rf {maroik_base_dir}/{file_storage_backup_upload_file_name}')

msg = MIMEMultipart()

msg['From'] = file_backup_email_from
msg['To'] = file_backup_email_to
msg['Date'] = formatdate(localtime=True)
msg['Subject'] = Header(s=f'Maroik.WebSite Upload File Backup ({current_date})', charset='utf-8')
body = MIMEText(f'Maroik.WebSite Upload File Backup ({current_date})', _charset='utf-8')
msg.attach(body)

files = list()
files.append(f'{maroik_base_dir}/{website_backup_upload_file_name}.tar.xz')
files.append(f'{maroik_base_dir}/{file_storage_backup_upload_file_name}.tar.xz')

for f in files:
    part = MIMEBase('application', "octet-stream")
    part.set_payload(open(f, "rb").read())
    encode_base64(part)
    part.add_header('Content-Disposition', 'attachment; filename="%s"' % os.path.basename(f))
    msg.attach(part)

mailServer = smtplib.SMTP_SSL(file_backup_smtp_host)
mailServer.login(file_backup_email_from, file_backup_email_password)
mailServer.send_message(msg)
mailServer.quit()

EOF

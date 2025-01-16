Restore the Backup: Restore the updated backup file:

Keep in mind db password, host, port(now it's exposed port), db and backup file name (to restore from)
```
PGPASSWORD="yourpassword" psql -h localhost -p 5460 -U postgres -d YourDb < ./backups/backup-file-name.sql
```



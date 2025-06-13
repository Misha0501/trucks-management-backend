Before deploying the application make sure there is a folder under /var/www/storage

Create the Storage Directory
```js
sudo mkdir -p /var/www/storage
```

Make It Writable
Quick & Easy (Development)
```
sudo chmod -R 777 /var/www/storage
```

Recommended (Production)
```
sudo chown -R $USER:www-data /var/www/storage
sudo chmod -R 775 /var/www/storage
```

Mount in docker-compose.yaml
Volumes Section
```yaml
volumes:
  app_storage:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /var/www/storage
```

Service Volume Mount
```yaml
services:
  truckmanagement:
    volumes:
      - app_storage:/app/storage
```

Reference in appsettings.json

```
"Storage": {
  "BasePath": "/app/storage",
  "BasePathCompanies": "/app/storage/Companies",
  "TmpPath": "/app/storage/Tmp",
  "SignedContractsPath": "/app/storage/SignedContracts"
}
```
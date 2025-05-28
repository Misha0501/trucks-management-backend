in local development add file. 
```
docker-compose.override.yml
```
with content
```yaml
version: '3.8'
services:
  truckmanagement:
    volumes:
      - ./TruckManagement/Storage/SignedContracts:/app/storage/signed-contracts
```
This will override the default docker-compose file and mount the local directory to the container. 
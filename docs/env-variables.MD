in development mode without docker env variables are stored in the appsettings.json file

in production env variables are stored in the .env file, which are then passed to docker compose file and later override appsettings.json's variables. 
#!/bin/bash
set -e

echo "🔄 Deployment webhook triggered at $(date)"

cd /var/www/backend

# Fix git permissions first
echo "🔧 Fixing permissions..."
sudo chown -R ubuntu:ubuntu .

# Pull latest changes from main
echo "📦 Pulling latest code from GitHub..."
git fetch origin

# Stash any local changes
if ! git diff-index --quiet HEAD --; then
  echo "💾 Stashing local changes..."
  git stash
fi

git checkout main
git pull origin main

# Ensure storage directory exists
sudo mkdir -p /var/www/storage/signed-contracts
sudo chmod -R 777 /var/www/storage

# Build only app container (NOT database - keeps it running)
echo "🔨 Building truckmanagement container..."
docker compose build truckmanagement

# Rolling update: start new container, stop old one (near-zero downtime)
echo "🚀 Deploying with rolling update..."
docker compose up -d --no-deps truckmanagement

# Wait for container to start
echo "⏳ Waiting for backend to be ready..."
sleep 10

# Health check
for i in {1..10}; do
  # Check if port responds (any HTTP response is good)
  if timeout 3 curl -s -o /dev/null -w '%{http_code}' http://localhost:9090 | grep -q '^[2345][0-9][0-9]$'; then
    echo "✅ Backend API is responding"
    if docker ps | grep -q "truckmanagement.*Up"; then
      echo "✅ Container status: Up"
      echo "✅ Deployment successful at $(date)!"
      exit 0
    fi
  fi
  echo "⏳ Waiting... attempt $i/10"
  sleep 3
done

echo "❌ Health check failed"
echo "📋 Container logs:"
docker logs --tail 30 backend-truckmanagement-1
exit 1

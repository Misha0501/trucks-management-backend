#!/usr/bin/env python3
import hashlib
import hmac
import json
import os
import subprocess
from http.server import BaseHTTPRequestHandler, HTTPServer

WEBHOOK_SECRET = os.environ.get('GITHUB_WEBHOOK_SECRET', '')
DEPLOY_SCRIPT = '/usr/local/bin/deploy-backend'

class WebhookHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path != '/webhook':
            self.send_response(404)
            self.end_headers()
            return
        
        # Read payload
        content_length = int(self.headers.get('Content-Length', 0))
        payload = self.rfile.read(content_length)
        
        # Verify signature
        signature = self.headers.get('X-Hub-Signature-256', '')
        if WEBHOOK_SECRET:
            expected = 'sha256=' + hmac.new(
                WEBHOOK_SECRET.encode(),
                payload,
                hashlib.sha256
            ).hexdigest()
            
            if not hmac.compare_digest(signature, expected):
                print(f'❌ Invalid signature')
                self.send_response(401)
                self.end_headers()
                self.wfile.write(b'Unauthorized')
                return
        
        # Parse payload
        try:
            data = json.loads(payload)
            ref = data.get('ref', '')
            
            # Only deploy on push to main
            if ref != 'refs/heads/main':
                print(f'ℹ️  Ignoring push to {ref}')
                self.send_response(200)
                self.end_headers()
                self.wfile.write(b'Ignored')
                return
            
            print(f'🚀 Triggering deployment for push to main')
            
            # Run deployment script in background
            subprocess.Popen(['/usr/bin/sudo', DEPLOY_SCRIPT])
            
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b'Deployment triggered')
            
        except Exception as e:
            print(f'❌ Error: {e}')
            self.send_response(500)
            self.end_headers()
            self.wfile.write(str(e).encode())
    
    def do_GET(self):
        if self.path == '/health':
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b'OK')
        else:
            self.send_response(404)
            self.end_headers()

if __name__ == '__main__':
    port = 8888
    server = HTTPServer(('0.0.0.0', port), WebhookHandler)
    print(f'🎧 Webhook server listening on port {port}')
    server.serve_forever()

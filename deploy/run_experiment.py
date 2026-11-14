#!/usr/bin/env python3
"""
Experiment: OAuth2 Token Caching and Clock Skew Behavior

Part 1: Token validity with Keycloak down
Part 2: Clock skew and token expiration
"""

import requests
import json
import time
import subprocess
from datetime import datetime
import base64

class ExperimentRunner:
    def __init__(self):
        self.results = []
        self.access_token = None
        self.keycloak_url = "http://localhost:8180/auth/realms/orderhub/protocol/openid-connect"
        self.api_url = "http://localhost:5000"
        self.gateway_url = "http://localhost:5000"
        
    def log(self, step, message, level="INFO"):
        timestamp = datetime.now().isoformat()
        log_entry = f"[{timestamp}] [{step}] [{level}] {message}"
        print(log_entry)
        self.results.append({
            "timestamp": timestamp,
            "step": step,
            "level": level,
            "message": message
        })
    
    def get_token(self, username="alice", password="alice123"):
        """Obtain an access token from Keycloak"""
        self.log("1.1", f"Logging in as {username}...")
        try:
            response = requests.post(
                f"{self.keycloak_url}/token",
                data={
                    "grant_type": "password",
                    "client_id": "orderhub-cli",
                    "client_secret": "orderhub-cli-secret",
                    "username": username,
                    "password": password,
                    "scope": "openid profile"
                },
                timeout=10
            )
            
            if response.status_code == 200:
                token_data = response.json()
                self.access_token = token_data.get("access_token")
                expires_in = token_data.get("expires_in")
                
                # Decode token to see claims
                parts = self.access_token.split('.')
                if len(parts) == 3:
                    # Decode payload (add padding if needed)
                    payload = parts[1]
                    payload += '=' * (4 - len(payload) % 4)
                    try:
                        decoded = json.loads(base64.urlsafe_b64decode(payload))
                        exp_time = datetime.fromtimestamp(decoded.get("exp", 0))
                        self.log("1.1", f"Token obtained. Expires at: {exp_time.isoformat()}", "SUCCESS")
                        return True
                    except Exception as e:
                        self.log("1.1", f"Token obtained but could not decode: {e}", "WARNING")
                        return True
                else:
                    self.log("1.1", f"Token obtained but malformed", "WARNING")
                    return True
            else:
                self.log("1.1", f"Login failed: HTTP {response.status_code}: {response.text}", "ERROR")
                return False
        except Exception as e:
            self.log("1.1", f"Exception during login: {e}", "ERROR")
            return False
    
    def test_api_call(self, description):
        """Test an API call with the current token"""
        if not self.access_token:
            self.log("TEST", f"{description}: No token available", "WARNING")
            return None
        
        try:
            headers = {"Authorization": f"Bearer {self.access_token}"}
            response = requests.get(
                f"{self.api_url}/health/live",
                headers=headers,
                timeout=5
            )
            self.log("TEST", f"{description}: HTTP {response.status_code}", "INFO")
            return response.status_code
        except requests.exceptions.ConnectionError as e:
            self.log("TEST", f"{description}: Connection failed (expected if Keycloak is down)", "INFO")
            return None
        except Exception as e:
            self.log("TEST", f"{description}: Exception: {e}", "ERROR")
            return None
    
    def stop_keycloak(self):
        """Stop the Keycloak container"""
        self.log("1.3", "Stopping Keycloak container...")
        try:
            result = subprocess.run(
                ["docker-compose", "-f", "docker-compose.yml", "-f", "docker-compose.services.yml", 
                 "stop", "orderhub-keycloak"],
                cwd=".",
                capture_output=True,
                timeout=30
            )
            if result.returncode == 0:
                self.log("1.3", "Keycloak stopped", "SUCCESS")
                time.sleep(3)  # Wait for container to stop
                return True
            else:
                self.log("1.3", f"Failed to stop Keycloak: {result.stderr.decode()}", "ERROR")
                return False
        except Exception as e:
            self.log("1.3", f"Exception stopping Keycloak: {e}", "ERROR")
            return False
    
    def start_keycloak(self):
        """Start the Keycloak container"""
        self.log("1.6", "Starting Keycloak container...")
        try:
            result = subprocess.run(
                ["docker-compose", "-f", "docker-compose.yml", "-f", "docker-compose.services.yml",
                 "start", "orderhub-keycloak"],
                cwd=".",
                capture_output=True,
                timeout=30
            )
            if result.returncode == 0:
                self.log("1.6", "Keycloak start command sent", "SUCCESS")
                
                # Wait for Keycloak to be healthy
                self.log("1.6", "Waiting for Keycloak to be ready...")
                for attempt in range(30):
                    try:
                        response = requests.get("http://localhost:8180/health", timeout=2)
                        if response.status_code in [200, 503]:
                            self.log("1.6", "Keycloak is responding", "SUCCESS")
                            time.sleep(3)
                            return True
                    except:
                        pass
                    time.sleep(2)
                
                self.log("1.6", "Keycloak did not respond in time but continuing", "WARNING")
                return True
            else:
                self.log("1.6", f"Failed to start Keycloak: {result.stderr.decode()}", "ERROR")
                return False
        except Exception as e:
            self.log("1.6", f"Exception starting Keycloak: {e}", "ERROR")
            return False
    
    def test_new_login(self, description):
        """Attempt a new login"""
        self.log("LOGIN_TEST", description + "...")
        try:
            response = requests.post(
                f"{self.keycloak_url}/token",
                data={
                    "grant_type": "password",
                    "client_id": "orderhub-cli",
                    "client_secret": "orderhub-cli-secret",
                    "username": "bob",
                    "password": "bob123",
                    "scope": "openid profile"
                },
                timeout=5
            )
            self.log("LOGIN_TEST", f"{description}: HTTP {response.status_code}", "INFO")
            return response.status_code
        except requests.exceptions.ConnectionError:
            self.log("LOGIN_TEST", f"{description}: Connection failed (expected - Keycloak down)", "SUCCESS")
            return None
        except Exception as e:
            self.log("LOGIN_TEST", f"{description}: Exception: {e}", "INFO")
            return None
    
    def run_part1(self):
        """Execute Part 1: Token validity with Keycloak down"""
        print("\n" + "="*60)
        print("PART 1: Token Validity with Keycloak Down")
        print("="*60 + "\n")
        
        # Step 1: Get token with Keycloak UP
        if not self.get_token("alice", "alice123"):
            self.log("1.1", "Failed to obtain initial token. Aborting Part 1.", "ERROR")
            return False
        
        # Step 2: Test API with token (Keycloak UP)
        time.sleep(2)
        code1 = self.test_api_call("1.2: API call with token (Keycloak UP)")
        
        # Step 3: Stop Keycloak
        if not self.stop_keycloak():
            self.log("PART1", "Failed to stop Keycloak", "ERROR")
            return False
        
        # Step 4: Test API with same token (Keycloak DOWN - JWKS should be cached)
        time.sleep(2)
        code2 = self.test_api_call("1.4: API call with token (Keycloak DOWN, using cached JWKS)")
        
        # Step 5: Try new login (should fail)
        time.sleep(1)
        self.test_new_login("1.5: New login attempt (Keycloak DOWN)")
        
        # Step 6: Restart Keycloak
        if not self.start_keycloak():
            self.log("PART1", "Failed to restart Keycloak", "WARNING")
        
        # Step 7: Verify login works again
        time.sleep(3)
        self.test_new_login("1.7: New login attempt (Keycloak restarted)")
        
        # Summary
        print("\n" + "-"*60)
        print("PART 1 SUMMARY:")
        print(f"  API call with token (Keycloak UP):   HTTP {code1}")
        print(f"  API call with token (Keycloak DOWN): HTTP {code2}")
        print(f"  Expected: Both should return 200 if JWKS caching works")
        print("-"*60 + "\n")
        
        return True
    
    def run_part2_note(self):
        """Document Part 2 expectations"""
        print("\n" + "="*60)
        print("PART 2: Clock Skew and Token Expiration")
        print("="*60 + "\n")
        
        self.log("2.0", "Part 2 requires temporary configuration changes to Gateway JWT settings", "NOTE")
        self.log("2.0", "Expected behavior: With ClockSkew=30s and token lifetime=30s", "NOTE")
        self.log("2.0", "  - At T+0s:   Token issued", "NOTE")
        self.log("2.0", "  - At T+30s:  Token expires but still accepted (within ClockSkew)", "NOTE")
        self.log("2.0", "  - At T+60s:  Token past expiration + skew, should be rejected (401)", "NOTE")
        
        print("\n" + "-"*60)
        print("PART 2 NOTES:")
        print("  To fully test Part 2, the Gateway's JwtBearer configuration")
        print("  would need to be modified temporarily:")
        print("    - Set ClockSkew to 30 seconds")
        print("    - Create a token with 30 second lifetime")
        print("  This requires code changes and rebuild.")
        print("-"*60 + "\n")
    
    def run(self):
        """Run the full experiment"""
        print("\n" + "="*80)
        print("EXPERIMENT: OAuth2 Token Caching and Clock Skew Behavior")
        print("="*80)
        print(f"Started: {datetime.now().isoformat()}\n")
        
        self.run_part1()
        self.run_part2_note()
        
        # Save results
        self.save_results()
        
        print("\n" + "="*80)
        print(f"Experiment completed: {datetime.now().isoformat()}")
        print("="*80 + "\n")
    
    def save_results(self):
        """Save results to JSON file"""
        results_file = "experiment-results.json"
        with open(results_file, 'w') as f:
            json.dump(self.results, f, indent=2)
        self.log("SAVE", f"Results saved to {results_file}", "SUCCESS")

if __name__ == "__main__":
    runner = ExperimentRunner()
    runner.run()

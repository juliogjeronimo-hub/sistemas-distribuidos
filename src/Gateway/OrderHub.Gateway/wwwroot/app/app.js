/**
 * OrderHub - OAuth 2.0 PKCE Client (pure JavaScript, no libraries)
 * 
 * Implements Authorization Code flow with PKCE S256 using crypto.subtle.
 * Tokens stored in memory; sessionStorage stores only code_verifier and state.
 */

(function() {
    'use strict';

    // Configuration
    const KEYCLOAK_URL = 'http://localhost:8180';
    const REALM = 'orderhub';
    const CLIENT_ID = 'orderhub-web';
    const REDIRECT_URI = 'http://localhost:5000/app/';
    const SCOPES = 'openid profile email';
    const GATEWAY_URL = 'http://localhost:5000';

    // Token storage (memory)
    let accessToken = null;
    let refreshToken = null;
    let tokenExpiry = null;
    let idToken = null;

    // UI elements
    const btnLogin = document.getElementById('btn-login');
    const btnLogout = document.getElementById('btn-logout');
    const btnRefresh = document.getElementById('btn-refresh');
    const btnMe = document.getElementById('btn-me');
    const btnProducts = document.getElementById('btn-products');
    const authStatus = document.getElementById('auth-status');
    const apiStatus = document.getElementById('api-status');
    const tokenPayload = document.getElementById('token-payload');
    const tokenStatus = document.getElementById('token-status');
    const httpResponse = document.getElementById('http-response');

    /**
     * PKCE helper functions using crypto.subtle
     */
    const pkce = {
        /**
         * Generate a random string for code_verifier
         */
        generateVerifier: async function(length = 43) {
            const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~';
            let result = '';
            const randomValues = new Uint8Array(length);
            crypto.getRandomValues(randomValues);
            for (let i = 0; i < length; i++) {
                result += chars[randomValues[i] % chars.length];
            }
            return result;
        },

        /**
         * Generate SHA-256 hash of the input
         */
        sha256: async function(data) {
            const encoder = new TextEncoder();
            const hashBuffer = await crypto.subtle.digest('SHA-256', encoder.encode(data));
            return hashBuffer;
        },

        /**
         * Base64URL encode (no padding, no +/, use -_)
         */
        base64UrlEncode: function(buffer) {
            const bytes = new Uint8Array(buffer);
            let binary = '';
            for (let i = 0; i < bytes.length; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            return btoa(binary)
                .replace(/\+/g, '-')
                .replace(/\//g, '_')
                .replace(/=/g, '');
        },

        /**
         * Generate code_challenge from verifier using S256
         */
        generateChallenge: async function(verifier) {
            const hashBuffer = await this.sha256(verifier);
            return this.base64UrlEncode(hashBuffer);
        }
    };

    /**
     * State management for CSRF protection
     */
    const stateManager = {
        generate: async function() {
            const state = await pkce.generateVerifier(32);
            sessionStorage.setItem('oauth_state', state);
            return state;
        },

        validate: function(receivedState) {
            const savedState = sessionStorage.getItem('oauth_state');
            if (!savedState || receivedState !== savedState) {
                throw new Error('State mismatch - possible CSRF attack');
            }
            sessionStorage.removeItem('oauth_state');
            return true;
        }
    };

    /**
     * Parse JWT payload without verification (for display only)
     */
    function parseJwtPayload(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) return null;
            const payload = parts[1];
            // Base64URL decode
            let base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
            const padLength = (4 - base64.length % 4) % 4;
            base64 += '='.repeat(padLength);
            const json = atob(base64);
            return JSON.parse(json);
        } catch (e) {
            return null;
        }
    }

    /**
     * Update UI based on authentication state
     */
    function updateAuthState() {
        const isAuthenticated = !!accessToken;
        
        btnLogin.disabled = isAuthenticated;
        btnLogout.disabled = !isAuthenticated;
        btnRefresh.disabled = !refreshToken || !isAuthenticated;
        btnMe.disabled = !isAuthenticated;
        btnProducts.disabled = !isAuthenticated;

        if (isAuthenticated) {
            authStatus.textContent = 'Authenticated';
            updateTokenDisplay();
        } else {
            authStatus.textContent = 'Not authenticated';
            tokenPayload.textContent = '-- not authenticated --';
            tokenStatus.textContent = '';
        }
    }

    /**
     * Display decoded token payload
     */
    function updateTokenDisplay() {
        if (accessToken) {
            const payload = parseJwtPayload(accessToken);
            if (payload) {
                tokenPayload.textContent = JSON.stringify(payload, null, 2);
                // Calculate remaining time
                if (payload.exp) {
                    const now = Math.floor(Date.now() / 1000);
                    const remaining = payload.exp - now;
                    if (remaining > 0) {
                        tokenStatus.textContent = `Expires in ${remaining}s`;
                    } else {
                        tokenStatus.textContent = 'Token expired';
                    }
                }
            }
        }
    }

    /**
     * Display HTTP response in UI
     */
    function displayResponse(status, data) {
        const statusText = status ? `HTTP ${status}` : 'Request';
        const display = data !== undefined 
            ? `${statusText}\n\n${JSON.stringify(data, null, 2)}`
            : statusText;
        httpResponse.textContent = display;
    }

    /**
     * Make authenticated API request
     */
    async function apiRequest(endpoint, options = {}) {
        const headers = {
            'Accept': 'application/json',
            ...options.headers
        };

        if (accessToken) {
            headers['Authorization'] = `Bearer ${accessToken}`;
        }

        try {
            const response = await fetch(`${GATEWAY_URL}${endpoint}`, {
                ...options,
                headers
            });

            apiStatus.textContent = `GET ${endpoint} - ${response.status}`;

            if (response.status === 401 && refreshToken) {
                // Token might be expired, try refresh
                await refreshAccessToken();
                // Retry request
                return apiRequest(endpoint, options);
            }

            let data = null;
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = await response.text();
            }

            displayResponse(response.status, data);
            return { ok: response.ok, status: response.status, data };
        } catch (error) {
            apiStatus.textContent = `GET ${endpoint} - Error`;
            displayResponse(null, { error: error.message });
            throw error;
        }
    }

    /**
     * Initiate OAuth 2.0 Authorization Code flow with PKCE
     */
    async function login() {
        try {
            // Generate PKCE values
            const codeVerifier = await pkce.generateVerifier();
            const codeChallenge = await pkce.generateChallenge(codeVerifier);
            const state = await stateManager.generate();

            // Store code_verifier for token exchange
            sessionStorage.setItem('oauth_code_verifier', codeVerifier);

            // Build authorization URL
            const params = new URLSearchParams({
                client_id: CLIENT_ID,
                redirect_uri: REDIRECT_URI,
                response_type: 'code',
                scope: SCOPES,
                code_challenge: codeChallenge,
                code_challenge_method: 'S256',
                state: state
            });

            const authUrl = `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/auth?${params}`;

            // Redirect to Keycloak
            window.location.href = authUrl;
        } catch (error) {
            console.error('Login error:', error);
            displayResponse(null, { error: error.message });
        }
    }

    /**
     * Exchange authorization code for tokens
     */
    async function handleCallback() {
        const params = new URLSearchParams(window.location.search);
        const code = params.get('code');
        const state = params.get('state');
        const error = params.get('error');

        if (error) {
            displayResponse(null, { error: `OAuth error: ${error}`, description: params.get('error_description') });
            return false;
        }

        if (!code) return false;

        try {
            // Validate state
            stateManager.validate(state);

            // Get stored code_verifier
            const codeVerifier = sessionStorage.getItem('oauth_code_verifier');
            if (!codeVerifier) {
                throw new Error('Code verifier not found in session storage');
            }

            // Exchange code for tokens
            const tokenParams = new URLSearchParams({
                grant_type: 'authorization_code',
                client_id: CLIENT_ID,
                code: code,
                redirect_uri: REDIRECT_URI,
                code_verifier: codeVerifier
            });

            const response = await fetch(`${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: tokenParams
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error_description || errorData.error || 'Token exchange failed');
            }

            const tokens = await response.json();

            // Store tokens in memory
            accessToken = tokens.access_token;
            refreshToken = tokens.refresh_token;
            idToken = tokens.id_token;

            // Clean up session storage
            sessionStorage.removeItem('oauth_code_verifier');
            sessionStorage.removeItem('oauth_state');

            // Clear URL
            window.history.replaceState({}, document.title, window.location.pathname);

            updateAuthState();
            apiStatus.textContent = 'Login successful';
            return true;
        } catch (error) {
            console.error('Token exchange error:', error);
            displayResponse(null, { error: error.message });
            return false;
        }
    }

    /**
     * Refresh access token using refresh token
     */
    async function refreshAccessToken() {
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }

        try {
            const params = new URLSearchParams({
                grant_type: 'refresh_token',
                client_id: CLIENT_ID,
                refresh_token: refreshToken
            });

            const response = await fetch(`${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: params
            });

            if (!response.ok) {
                // Refresh failed - user needs to login again
                logout();
                throw new Error('Token refresh failed - please login again');
            }

            const tokens = await response.json();

            accessToken = tokens.access_token;
            refreshToken = tokens.refresh_token || refreshToken;
            idToken = tokens.id_token || idToken;

            updateAuthState();
            apiStatus.textContent = 'Token refreshed';
            return true;
        } catch (error) {
            console.error('Refresh error:', error);
            throw error;
        }
    }

    /**
     * Logout - call Keycloak end_session endpoint
     */
    async function logout() {
        try {
            // Build logout URL
            const params = new URLSearchParams({
                client_id: CLIENT_ID,
                post_logout_redirect_uri: REDIRECT_URI
            });

            // Clear memory storage
            accessToken = null;
            refreshToken = null;
            idToken = null;
            tokenExpiry = null;

            updateAuthState();
            apiStatus.textContent = 'Logged out';

            // If we have an id_token, include it for proper logout
            if (idToken) {
                params.set('id_token_hint', idToken);
                idToken = null;
            }

            // Redirect to Keycloak logout
            const logoutUrl = `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/logout?${params}`;
            window.location.href = logoutUrl;
        } catch (error) {
            console.error('Logout error:', error);
        }
    }

    /**
     * Initialize event handlers
     */
    function initEventHandlers() {
        btnLogin.addEventListener('click', login);
        btnLogout.addEventListener('click', logout);
        btnRefresh.addEventListener('click', () => refreshAccessToken().catch(() => {}));
        btnMe.addEventListener('click', () => apiRequest('/me'));
        btnProducts.addEventListener('click', () => apiRequest('/catalog/v1/products'));
    }

    /**
     * Initialize the application
     */
    async function init() {
        // Check if we're returning from OAuth callback
        const hasCode = new URLSearchParams(window.location.search).has('code');

        if (hasCode) {
            // Handle callback
            await handleCallback();
        }

        // Set up event handlers
        initEventHandlers();

        // Update UI
        updateAuthState();
    }

    // Start the application when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
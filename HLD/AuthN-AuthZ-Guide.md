# Authentication & Authorization — SSE Interview Guide

---

## Core Concepts

- **Authentication (AuthN):** Verifying *who* you are (identity).
- **Authorization (AuthZ):** Verifying *what* you're allowed to do (permissions).

---

## 1. JWT — JSON Web Token

### What it is
A stateless, self-contained token format used to transmit claims between parties. Commonly used for API authentication.

### Structure
```
Header.Payload.Signature
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1c2VyMTIzIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```
- **Header:** Algorithm + token type
- **Payload:** Claims (sub, exp, roles, custom data)
- **Signature:** HMAC or RSA signature to verify integrity

### Flow
```
Client → POST /login (credentials)
Server → validates → returns JWT
Client → sends JWT in Authorization: Bearer <token> header
Server → verifies signature → extracts claims → grants access
```

### Key Properties
- **Stateless:** Server doesn't store session; all info is in the token.
- **Expiry:** `exp` claim enforces token lifetime (short-lived = more secure).
- **Refresh tokens:** Long-lived token used to get new access tokens without re-login.

### Signing Algorithms
| Algorithm | Type | Use Case |
|---|---|---|
| HS256 | Symmetric (shared secret) | Internal services, single server |
| RS256 | Asymmetric (private/public key) | Distributed systems, third-party verification |
| ES256 | Elliptic curve | High-security, compact tokens |

### Security Considerations
- Always validate `exp`, `iss`, `aud` claims.
- Use short-lived access tokens (5–15 min) + refresh tokens.
- Store tokens in `httpOnly` cookies (not localStorage) to prevent XSS.
- Use RS256 in distributed systems — public key can be shared safely.
- Implement token revocation via a blocklist or short expiry + refresh rotation.

### When to Use
- Stateless REST APIs
- Microservices inter-service auth
- Mobile app authentication

---

## 2. OAuth 2.0

### What it is
An **authorization framework** (not authentication) that allows a third-party application to obtain limited access to a resource on behalf of a user, without exposing credentials.

### Key Roles
| Role | Description |
|---|---|
| Resource Owner | The user who owns the data |
| Client | The application requesting access |
| Authorization Server | Issues tokens (e.g., Auth0, Okta, AWS Cognito) |
| Resource Server | API that holds the protected resources |

### Grant Types (Flows)

**Authorization Code Flow** (most secure, for web/mobile apps)
```
User → Client → Authorization Server (login + consent)
Auth Server → returns authorization code to redirect URI
Client → exchanges code for access token (server-to-server)
Client → uses access token to call Resource Server

1. Browser
      ↓
2. Google Authorization Server
      ↓
3. Browser receives Authorization Code via redirect
      ↓
4. Browser sends Authorization Code to Your Backend
      ↓
5. Your Backend sends Authorization Code to Google Token Endpoint
      ↓
6. Google returns Access Token (and possibly Refresh Token, ID Token)
      ↓
7. Your Backend uses Access Token to call Google APIs
```

**Client Credentials Flow** (machine-to-machine)
```
Service A → POST /token (client_id + client_secret)
Auth Server → returns access token
Service A → calls Service B API with token
```

**PKCE (Proof Key for Code Exchange)** — extension for public clients (SPAs, mobile)
- Prevents authorization code interception attacks
- Client generates `code_verifier` + `code_challenge` (SHA256 hash)
- Auth server validates the verifier when exchanging the code

### Token Types
- **Access Token:** Short-lived, used to access APIs (JWT or opaque)
- **Refresh Token:** Long-lived, used to get new access tokens
- **ID Token:** Not part of OAuth 2.0 — added by OIDC (see below)

### Scopes
Define what the client is allowed to do:
```
scope=read:profile write:orders
```

### Security Considerations
- Always use PKCE for public clients.
- Validate `state` parameter to prevent CSRF.
- Use short-lived access tokens.
- Never expose `client_secret` in frontend code.
- Rotate refresh tokens on each use (refresh token rotation).

### When to Use
- Third-party integrations ("Login with Google/GitHub")
- Delegated access (app acting on behalf of user)
- M2M service-to-service authorization

---

## 3. OIDC — OpenID Connect

### What it is
An **identity layer built on top of OAuth 2.0**. While OAuth 2.0 handles authorization, OIDC adds authentication — it tells you *who* the user is.

### What it Adds to OAuth 2.0
- **ID Token:** A JWT containing user identity claims (`sub`, `email`, `name`, `picture`)
- **UserInfo Endpoint:** `/userinfo` — fetch additional user profile data
- **Standard Claims:** Defined set of user attributes (unlike custom JWT claims)
- **Discovery Document:** `/.well-known/openid-configuration` — auto-discovery of endpoints

### ID Token Claims
```json
{
  "iss": "https://accounts.google.com",
  "sub": "user-unique-id",
  "aud": "your-client-id",
  "exp": 1700000000,
  "email": "<user-email>",
  "name": "<user-name>"
}
```

### OIDC vs OAuth 2.0
| | OAuth 2.0 | OIDC |
|---|---|---|
| Purpose | Authorization | Authentication + Authorization |
| Token | Access Token | Access Token + ID Token |
| Answers | "What can this app do?" | "Who is this user?" |
| Use case | API access delegation | Login / SSO |

### Flow (Authorization Code + OIDC)
```
Client requests scope=openid profile email
Auth Server returns: authorization code
Client exchanges code → gets access_token + id_token
Client validates id_token signature (using JWKS endpoint)
Client reads user identity from id_token claims
```

### When to Use
- Federated login ("Sign in with Google/Apple/Microsoft")
- Building SSO across your own applications
- Any time you need to know *who* the user is, not just *what* they can access

---

## 4. SSO — Single Sign-On

### What it is
A mechanism that allows a user to authenticate **once** and gain access to **multiple applications** without re-authenticating.

### How it Works (High Level)
```
User → App A (not logged in) → redirected to Identity Provider (IdP)
User logs in at IdP → IdP creates session + issues token/assertion
User → App B → IdP sees existing session → issues token without re-login
```

### SSO Protocols

**SAML 2.0 (Security Assertion Markup Language)**
- XML-based, enterprise-focused
- IdP sends a signed XML assertion to the Service Provider (SP)
- Common in enterprise apps (Salesforce, Workday, AWS IAM Identity Center)
- Flow: SP-initiated or IdP-initiated

**OIDC-based SSO**
- Modern, JSON/JWT-based
- Used by consumer identity providers (Google, Apple, Microsoft)
- Better for web/mobile apps

**CAS (Central Authentication Service)**
- Older protocol, used in university/enterprise environments
- Less common in modern systems

### SSO Components
| Component | Role |
|---|---|
| Identity Provider (IdP) | Authenticates users, issues assertions (Okta, Azure AD, Cognito) |
| Service Provider (SP) | The application relying on IdP for auth |
| Session Cookie | IdP sets a session cookie so re-auth isn't needed |

### SSO in AWS
- **IAM Identity Center (SSO):** Centralized access to multiple AWS accounts and apps
- **Amazon Cognito:** OIDC/SAML federation for user pools
- **ALB + OIDC:** Application Load Balancer can authenticate users via OIDC before forwarding requests

### Security Considerations
- SSO is a high-value target — compromise of IdP = compromise of all apps.
- Enforce MFA at the IdP level.
- Implement session timeouts and re-authentication for sensitive operations.
- Use signed and encrypted SAML assertions.

### When to Use
- Enterprise environments with many internal apps
- Reducing password fatigue and improving UX
- Centralized access control and audit logging

---

## 5. API Key Authentication

### What it is
A simple shared secret (string) passed in request headers or query params to identify and authenticate a client.

### Usage
```
GET /api/data
X-API-Key: <api-key-value>
```

### Characteristics
- Stateless, simple to implement
- No user identity — identifies the *application*, not the user
- No expiry by default (must be managed manually)

### Security Considerations
- Rotate keys regularly.
- Never expose in client-side code or URLs (use headers).
- Rate-limit per API key.
- Use alongside HTTPS only.

### When to Use
- Server-to-server integrations
- Public APIs with simple access control
- Webhooks

---

## 6. mTLS — Mutual TLS

### What it is
Both client and server present X.509 certificates to authenticate each other. Standard TLS only authenticates the server; mTLS authenticates both sides.

### Flow
```
Client → presents client certificate
Server → verifies client cert against trusted CA
Server → presents server certificate
Client → verifies server cert
Encrypted channel established with mutual trust
```

### When to Use
- Zero-trust microservice-to-microservice communication
- High-security B2B integrations
- Service meshes (Istio, AWS App Mesh use mTLS by default)

---

## 7. Session-Based Authentication

### What it is
Server creates a session after login, stores it server-side, and returns a session ID cookie to the client.

### Flow
```
Client → POST /login
Server → creates session in store (Redis/DB) → sets Set-Cookie: sessionId=abc
Client → sends cookie on every request
Server → looks up session → validates → grants access
```

### vs JWT
| | Session-Based | JWT |
|---|---|---|
| State | Stateful (server stores session) | Stateless |
| Revocation | Instant (delete session) | Hard (need blocklist) |
| Scalability | Requires shared session store | Scales easily |
| Best for | Traditional web apps | APIs, microservices |

---

## 8. RBAC vs ABAC vs ReBAC

### RBAC — Role-Based Access Control
Permissions assigned to roles; users assigned to roles.
```
User → Role: admin → Permissions: read, write, delete
User → Role: viewer → Permissions: read
```
- Simple, easy to audit
- Coarse-grained — doesn't handle context well

### ABAC — Attribute-Based Access Control
Permissions based on attributes of user, resource, and environment.
```
Allow if: user.department == resource.department AND time == business_hours
```
- Fine-grained, flexible
- Complex policy management
- Used in AWS IAM (condition keys), OPA (Open Policy Agent)

### ReBAC — Relationship-Based Access Control
Permissions based on relationships between entities (used by Google Zanzibar, AWS Verified Permissions).
```
User can edit Document if User is owner of Document
User can view Document if User is member of Group that has viewer on Document
```
- Handles complex hierarchical permissions
- Used in Google Drive, GitHub, Notion

---

## Quick Comparison Summary

| Mechanism | AuthN | AuthZ | Stateless | Best For |
|---|---|---|---|---|
| JWT | ✅ | ✅ (claims) | ✅ | APIs, microservices |
| OAuth 2.0 | ❌ | ✅ | ✅ | Delegated access, M2M |
| OIDC | ✅ | ✅ | ✅ | Federated login, SSO |
| SSO (SAML) | ✅ | ✅ | ❌ (IdP session) | Enterprise apps |
| API Key | Partial | ✅ | ✅ | Simple server-to-server |
| mTLS | ✅ | ❌ | ✅ | Zero-trust service mesh |
| Session | ✅ | ✅ | ❌ | Traditional web apps |
| RBAC/ABAC | ❌ | ✅ | N/A | Access control models |

---

## AWS Services Mapping

| Use Case | AWS Service |
|---|---|
| User auth + JWT/OIDC | Amazon Cognito User Pools |
| Federated identity (Google, SAML) | Cognito Identity Pools / IAM Identity Center |
| M2M OAuth (API Gateway) | Cognito + Lambda Authorizer |
| SSO across AWS accounts | IAM Identity Center |
| mTLS for microservices | AWS App Mesh / API Gateway mTLS |
| Fine-grained AuthZ | AWS Verified Permissions (Cedar policy language) |
| API Key management | API Gateway API Keys |

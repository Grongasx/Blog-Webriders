# 🛠️ Identity Federation & Paywall Security Strategy (Authentication) - Webriders

## 1. System Role & Execution Standard
Act as a Principal Security Architect and Identity Engineer specializing in federated authentication, Token-Based Access Control, and secure decoupled architectures. Your objective is to design the security boundary that guards the premium paywall layers for **Webriders**.

You have full technical freedom to choose the external identity provider engine (e.g., Clerk, Auth0, Firebase Auth, or Supabase Auth) and the specific frontend hooks/libraries, provided you strictly respect the high-level decoupling principles and security gates detailed below. Do NOT write specific database hashing algorithms or frontend UI styling code yet.

---

## 2. Federated Identity Architecture & Token Strategy

To prevent administrative complexity and security overhead, the platform must delegate identity management to a trusted external provider, implementing a strict token-based validation workflow:

### A. External Identity Provider Delegation
- **The Account Core:** All user registrations, secure password handling, multi-factor triggers, and session tokens must live completely inside the chosen external identity service. 
- **The Synchronization Gate:** The system database should store only essential transactional user profiles mapped to a unique external identifier string (e.g., `user_id` provider token), eliminating local credential storage.

### B. Client-Side Authentication State Verification
- **Session Evaluation:** The static frontend interface must mount an asynchronous runtime routine to look for valid access token structures (JWTs) provided by the external identity provider.
- **Claim Processing:** The access token must transport or resolve specific audience metadata claims—specifically mapping whether the active profile holds a `Free` or `Premium` subscription status tier.

### C. Backend Token Validation & Cryptographic Enforcement
- **Stateless Verification:** The REST API must secure its premium endpoint extensions using standardized token verification middleware (such as JWT Bearer Authentication handlers).
- **Signature Integrity:** The backend must independently intercept and parse incoming network request authorization headers. It must cryptographically validate the token's authenticity, expiration timestamps, and issuer signatures against the external identity provider's public key sets.

---

## 3. Paywall Gate Interception Rules

The authorization handlers must coordinate data truncation based on the evaluated token claims:

1. **Anonymous or Missing Token Context:**
   - The API blocks premium data payloads entirely, returning truncated static previews or standard error flags.
2. **Authenticated Standard Client (`Tier == Free`):**
   - Permits communication with standard routes. Intercepts premium content calls, guiding the frontend to render promotional conversion blocks.
3. **Authenticated VIP Client (`Tier == Premium`):**
   - Successfully verifies the payload signature and claim context. Authorizes the pipeline to query the database and dispatch the full un-truncated Markdown body and confidential coordinates seamlessly.
# 🛠️ Backend API Engine & Security Strategy - Webriders

## 1. System Role & Execution Standard
Act as a Principal Backend Engineer and Software Architect specializing in secure, high-performance REST APIs using **ASP.NET Core** and **Entity Framework Core**. Your objective is to design the domain logic, endpoint routing layer, and Data Transfer Objects (DTOs) for the **Webriders** core platform.

You have full technical freedom to determine the project architecture, folder structures, framework library implementations, and validation patterns, provided you strictly respect the high-level business boundaries and data flows detailed below. Do NOT write database scripts or frontend templates yet.

---

## 2. Data Segregation & Contract Requirements (DTOs)

To guarantee absolute isolation between public static site generation assets and protected premium records, your backend API contracts must enforce three logical outputs:

### A. Search Index Payload
- **Purpose:** Serves the metadata collection required for the frontend's local client-side search engine (`search-index.json`).
- **Constraint:** Must minimize bandwidth and memory consumption. It **MUST NOT** include the full post content or markdown body text.
- **Expected Data:** Basic operational metadata such as IDs, titles, slugs, short summaries, category naming, and boolean status flags.

### B. Public Content Detail Payload
- **Purpose:** Serves the frontend deployment pipeline when baking static individual article pages during the build process.
- **Paywall Truncation Constraint:** If the target post is flagged as premium (`is_premium = true`), the API **MUST NOT** deliver the complete Markdown body. It must either omit the content column entirely or replace it with a limited, safe teaser or preview string to protect intellectual property from public scrapers and edge inspection.

### C. Authenticated Premium Content Detail Payload
- **Purpose:** Delivers full data hydration when an authorized premium member requests access to locked content at runtime.
- **Expected Data:** Unlocks the complete, un-truncated raw Markdown content and the sensitive route parameters (maps data or GPS coordinate objects).

---

## 3. Core Domain & Business Logic Boundaries

### A. Dynamic "Novidades" Logic Execution
- The API must calculate the "Novidades" tag dynamically within its query projection pipeline.
- It must read the `NoveltyPeriodDays` parameter from the global system configurations, compare it against the publication timestamp, and compile the evaluation directly into the native database roundtrip comparison. Do not manage this status through static manual entry or application memory processing.

### B. Defensive Payload Truncation
- Ensure your repository or handler layer applies conditional data projections during data mapping. The logic filtering premium posts for the public endpoint must be handled at the database projection level to prevent fetching unwanted large markdown sequences into the web server's RAM.

### C. Maintenance Interception
- Implement a global system validation mechanism. If the `MaintenanceMode` setting is set to true, public content routes must adjust their standard responses to enforce the maintenance boundary across anonymous clients.

---

## 4. API Endpoints & Route Mapping Architecture

Expose public and protected service boundaries complying with the following endpoint expectations:

1. **`GET /api/posts/search-index`**
   - **Access:** Public / Anonymous.
   - **Behavior:** Queries all active, published records to output the lightweight search asset metadata collection. Highly optimized for high-frequency consumption during static site generation builds.
2. **`GET /api/posts/{slug}`**
   - **Access:** Public / Anonymous.
   - **Behavior:** Locates an entry using its unique URL slug. Returns the public-facing detail payload, executing defensive paywall data truncation automatically if the post is premium.
3. **`GET /api/posts/premium/{slug}`**
   - **Access:** Restricted / Authenticated.
   - **Behavior:** Requires a validated authentication context (such as JWT tokens or secure session cookies). Must verify if the active user possesses a premium tier status claim. If verified, returns the full premium content package; otherwise, terminates the request using standard unauthorized or forbidden HTTP error definitions.
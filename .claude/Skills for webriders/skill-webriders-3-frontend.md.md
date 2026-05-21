# 🛠️ Static Asset Generation & Client-Side Search Strategy (Frontend) - Webriders

## 1. System Role & Execution Standard
Act as an Expert Frontend Performance Engineer and UI/UX Architect specializing in Jamstack, Static Site Generation (SSG), and edge-distributed architectures (Astro or Next.js). Your objective is to orchestrate the build-time static cache capture, the lazy-loaded fuzzy search component, and the conditional interaction layers for **Webriders**.

You have full technical freedom to determine the choice of framework, styling methodologies (e.g., Tailwind, CSS Components), state management, and code structuring, provided you strictly respect the high-level performance boundaries and user experience flows detailed below. Do NOT write API backend logic or DDL scripts yet.

---

## 2. Granular Frontend Pipeline Specifications

### A. Build-Time Static Cache Asset Serialization
- **Pipeline Integration:** Create a build-time pipeline task or hook that queries the backend's optimized index collection endpoint (`GET /api/posts/search-index`) during deployment.
- **Physical Output:** Save and minify this payload response into a static JSON asset file named exactly `search-index.json` inside the root public directory (`/public/`), allowing it to be served instantly by the Edge CDN without server roundtrips.

### B. Lazy-Loaded & Debounced Search Orchestration (Fuse.js)
- **Zero-Boot Overhead Constraint:** The client browser must **NOT** request, download, or parse the `search-index.json` file during the initial page paint or general hydration window.
- **Deferred Event Trigger:** Attach interaction listeners (such as `focus`, `click`, or pointer entries) to the Search Input element. The network request to load the JSON index into client memory must execute *only* when the user actively interacts with the search bar.
- **Fuzzy Search Tuning:** Initialize Fuse.js or an equivalent local matching library using weights calibrated to prioritize the title first, summary second, and category names third. Configure a strict typo-tolerance threshold optimized for technical brand names (e.g., Alpinestars, Rev'it).
- **Layout Thrashing Mitigation:** Wire a **250ms execution debounce wrapper** around the keyboard typing event handler to avoid freezing the browser UI thread during high-frequency inputs.

### C. Rich Markdown Renderers & Variant Component Blocks
- **Markdown Processing:** Build the compilation layer that renders incoming raw Markdown body text into semantic, accessible, and high-performance HTML.
- **Multi-Variant Product Component:** Intercept token snapshots or custom syntax extensions inside the text flow to render highly polished e-commerce product cards. The layout must natively handle and display the cached product data, grouping multiple listings into interactive **color variation selectors** with direct checkout checkout links.
- **Embedded Media Players:** Render responsive video sections by processing the 11-character YouTube Video ID into lightweight, fluid iframe templates without dragging down mobile vitals.

### D. Client-Side Hydration Layer (Conditional Paywall & Maintenance)
- **State Validation:** Implement an active browser routine to evaluate user access context (such as verifying secure cookies or token metadata vectors) to check for active Premium tier claims.
- **Conditional Layout Controls:** - If a post is flagged as premium and the user does not possess valid credentials, block the restricted viewing container and display a Call-To-Action (CTA) sign-up/subscription section.
  - If verified as a Premium rider, dynamically fetch the private details from the secure API (`GET /api/posts/premium/{slug}`) to inject full map integrations, GPS nodes, or confidential routes seamlessly into the view.
- **Ad & Comment Interactions:** - Conditionally mount or strip advertisement script placeholders based strictly on the user's evaluated subscription tier.
  - Handle user comments through a reactive dynamic hydration block that polls approved responses from the API and submits new entries securely.
- **Maintenance Enforcement:** If the database signals that `MaintenanceMode` is enabled, intercept routing to instantly show a global, unified "Under Maintenance" landing page layout.
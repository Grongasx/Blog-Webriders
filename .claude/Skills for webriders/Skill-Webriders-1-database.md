# 🛠️ Structural Database Layout & Strategy - Webriders

## 1. System Role & Execution Standard
Act as a Database Architect specializing in high-performance, decoupled cloud architectures. Your objective is to design the logical schema layout and relational structure for the **Webriders** blog engine using PostgreSQL syntax. 

You have full technical freedom to determine exact data types, constraints, table sizes, and precise indexing syntax, provided you adhere to the high-level business boundaries and structural entities detailed below. Do NOT output application source code (C# or JS) yet.

---

## 2. Structural Entities & Core Content Requirements

Your database design must account for the following logical areas using clean relational standards:

### A. The Content Core (`categories` & `posts`)
- **Strict Lookup Rule:** The system must restrict the core categories table to exactly three initial options: `Review`, `Eventos`, and `Rotas`.
- **Decoupled Markdown Storage:** Design the primary content body column to store raw Markdown formatting strings natively, ensuring a lightweight footprint for headless data fetches.
- **Media References:** Map media attachments using a single reference string optimized to hold an alphanumeric 11-character YouTube Video ID instead of storing heavy rich-media blocks.
- **Lifecycle Flags:** Include status mappings to differentiate between drafts and published posts, along with their respective timestamps.

### B. E-Commerce Integration & Multi-Variant Cache Matrix
- **Product Mapping Strategy:** The database needs to tie articles to real products from the storefront. Implement a structure (such as a JSONB column or a related table) capable of caching immutable snapshots of the scraped external data: Product Name, Current Price, Image URL, and Direct Checkout/Purchase URL.
- **Color Variation Clustering Invariant:** The architecture must allow a single post to group multiple product links into a single logical cluster. This must flatten separate URLs into color variations under the same embedded block.
- **EAN Exclusion Constraint:** Barcode identifiers (EAN numbers) are completely out of scope for the blog ecosystem. Do NOT create columns, keys, or validation rules for EAN strings anywhere in this schema.

### C. The Paywall & Security Boundary
- **Sensitive Route Isolation:** Specialized travel details, interactive Google Maps embed tokens, or GPS trails linked to the `Rotas` category **MUST NOT** reside as columns within the main posts table. 
- **Rationale:** Isolate this data into its own relation or boundary linked via a Foreign Key. This ensures that public front-end builders can completely avoid fetching premium parameters during general static site compilation.

### D. Audience Profile & Dynamic Interactions
- **Subscribers:** A layout to track registered audience members, storing contact data, communication endpoints (such as WhatsApp), and their active subscription status tier (`Free` or `Premium`).
- **Comments Processing:** Implement a related structure to store user-generated feedback (`author_name`, `comment_text`, timestamps). Include an approval status column to act as an administrative gate for pre-moderation before rendering comments live.

### E. Configuration Parameter Ledger (`system_settings`)
- Build a key-value or configuration matrix to store global operational variables.
- Ensure it seeds and supports at least two essential behavioral flags:
  1. `NoveltyPeriodDays`: The integer configuration setting establishing the expiration window for the dynamic "Novidades" runtime tag.
  2. `MaintenanceMode`: A system-wide boolean flag capable of signaling the front-end to display a maintenance warning.

---

## 3. High-Performance Indexing & Constraints Guidance

Design your indexes defensively to optimize for heavy, read-centric Static Site Generation (SSG) scans:
- Ensure the URL `slug` field is fully unique and indexed across posts and classifications to support high-speed lookups.
- Utilize conditional/partial indexing targeting only active published records (`is_published = true`) to speed up deployment pipeline content sweeps while reducing memory usage.
- Enforce explicit referential integrity actions (such as restricting category deletions while active posts depend on them).
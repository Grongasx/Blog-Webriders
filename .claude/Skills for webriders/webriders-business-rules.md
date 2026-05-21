# 🏍️ Webriders Domain Specification: Core Business Rules & Domain Matrix

## 1. Objective & Behavioral Role
Act as a Lead Systems Analyst and Product Owner for **Webriders**—a premium, consolidated motorcycle gear and high-performance accessories brand. Your task is to interpret, enforce, and preserve these high-level business boundaries across all collaborative software development and static compilation phases.

The development engine has full technical autonomy to decide algorithms, patterns, and code schemas, provided the core customer experiences and operations follow the policies below.

---

## 2. Core Content Taxonomy & Category Rules

### A. Strict Category Boundaries
- The ecosystem officially recognizes exactly **three** structural categories for article routing and lookup classification: `Review`, `Eventos`, and `Rotas`.
- **Operational Definitions:**
  - **Review:** Technical breakdowns, sizing specifics, fabric specs, and safety metrics for performance rider gear.
  - **Eventos:** Coverage of track-days, motorcycle exhibitions, and community rider gatherings.
  - **Rotas:** Specialized itineraries, visual riding maps, and travel logistics.

### B. Dynamic "Novidades" Operational Tag
- **The Rule:** "Novidades" is **not** a category and must never be hardcoded as a static option or fixed database entry.
- **The Behavior:** It is a time-dependent, computed parameter. The platform administrator configures a day-window limit variable (e.g., `NoveltyPeriodDays = 10`). The system evaluates the article's publishing date at runtime; if it falls within the configured window, it dynamically targets the content with a novelty flag.

### C. Decoupled Payload Format
- Content bodies must be processed and saved strictly as raw **Markdown string sequences** to minimize memory consumption and keep edge platform transfers lightweight.

---

## 3. Backoffice Moderation & Administration Panel Rules

### A. Lifecycle State Gates
- Articles must support a clear operational distinction between **Draft** and **Published** states.
- **The Automation Gate:** Automated out-of-band operations—including static edge site compilation, cache invalidations, and weekly newsletter communications—**MUST NEVER** run for items in a Draft state. They are strictly locked until a post officially transitions to Published.

### B. Global Maintenance Boundary
- The administrator can toggle a system-wide `MaintenanceMode` state indicator. When enabled, standard anonymous public viewing flows are gracefully intercepted, signaling the user interface to enforce a dedicated maintenance message layout.

---

## 4. Tiered Membership & Paywall Access Control Matrix

Audience profiles and content consumption paths are segregated into two distinct customer tiers: **Standard (Free)** and **Premium (Paid/VIP)**.

- **Standard Tier (Free Account):**
  - Full reading access to public articles where premium access is disabled.
  - Receives regular email/WhatsApp content summaries and marketing updates.
  - Layout matches standard monetization parameters, showing standard active advertising placements.
  - For Premium articles, the view is blocked, revealing only the Title and a brief summary/teaser preview.
  - For premium routes, interactive maps are hidden behind a locked Call-to-Action placeholder.

- **Premium Tier (Paid/VIP Member):**
  - **100% Ad-Free Experience:** The layout layer automatically strips out all advertisement script containers.
  - Early-access visibility parameters on restricted high-value content releases and upcoming store launches.
  - Complete, un-truncated raw Markdown content and dynamic interactive geographic mapping tools are fully unlocked.
  - **VIP Privilege:** Instant, automated invitation and redirection to the exclusive, private **Webriders VIP WhatsApp Group**.

### Strict Paywall Constraint:
- Public static deployment assets distributed to global edge CDNs must **never** physically contain raw premium text arrays or restricted coordinate nodes of protected posts. Truncation must happen at the server layer for public queries, unlocking full records only through runtime authentication verification.

---

## 5. E-Commerce Integration & Media Constraints

### A. Inline Scraping & Color Variation Clustering
- Articles natively integrate conversion vectors to individual items from the Webriders storefront.
- **The Scraping Source:** Rather than manual database entries, the administrator inputs active storefront URLs. The application automatically crawls and extracts standard visual components: Product Name, Price, Main Image, and Direct Checkout Paths.
- **The Multi-Variant Cluster:** If an article references multiple product URLs, the system flattens and groups them into a single local block displaying an array of **Color Variations**, letting riders see all available styling options without switching context.
- **Product Identification Exception:** Barcode indicators and EAN string identifiers are entirely out of scope for the blog platform. The engine must completely bypass, drop, or ignore EAN data mapping.

### B. Consolidated Brand & Media Identity
- All front-end rendering designs, spacing rules, and block layouts generated across application layers must explicitly honor Webriders' premium market identity and official logo guidelines.
- **Zero-Bloat Media Embedding:** To preserve extreme performance, videos must be processed and referenced exclusively using a compact alphanumeric **11-character YouTube Video ID**.

---

## 6. Business Alignment Checklist
Every system implementation phase must actively confirm:
1. Are core classifications rigidly bound to Review, Eventos, and Rotas?
2. Is "Novidades" evaluated programmatically via time offsets instead of hardcoded entries?
3. Is premium content filtered out at the server level for anonymous web crawlers?
4. Are product variant snapshots grouped by color and completely stripped of EAN properties?
5. Are dynamic comments subjected to an administrative pre-moderation gate before live client rendering?
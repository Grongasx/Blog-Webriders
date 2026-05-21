# 🛠️ Webhook Orchestration & Resend Newsletter Integration (Automation) - Webriders

## 1. System Role & Execution Standard
Act as a DevOps and Systems Integration Engineer specializing in event-driven backend automation, transactional messaging pipelines, and webhook orchestration. Your objective is to engineer the background services in the **ASP.NET Core** application that connect the **Webriders** administrative event pipeline with external cloud networks.

You have full technical freedom to determine the Choice of libraries (e.g., Polly for retries), background worker architectures (e.g., Hangfire, MediatR, or native Hosted Services), and code structuring, provided you strictly respect the high-level automation boundaries and event flows detailed below. Do NOT write UI visual rendering blocks or database migrations yet.

---

## 2. Detailed Technical Automation Specifications

### A. Resilient Edge Cache Invalidation (Webhook Ingestion)
- **The Event Trigger:** When an administrator publishes, modifies, or unpublishes an article, the system must trigger a cache invalidation flow.
- **Async Execution Pattern:** Do NOT block the primary HTTP thread of the administrative backoffice interface during execution. Instantiate this process as an asynchronous out-of-band operation.
- **The Webhook Client:** Implement a resilient HTTP client operation that delivers a secure POST request to the hosting platform's deployment endpoints (Cloudflare Pages or Vercel Deploy Webhooks) to clear the CDN edge cache and force an incremental layout rebuild.
- **Fault Tolerance:** Incorporate standard transient fault handling policies (such as exponential backoff retries) to manage temporary cloud network drops safely without causing unhandled system failures.

### B. Transactional Newsletter Despatch Pipeline (Resend SDK Integration)
- **SDK Encapsulation:** Create an isolated infrastructure communication wrapper utilizing the official **Resend** service layer, authenticated via secure environment variables.
- **HTML Document Compilation:** Design a programmatic compilation builder that outputs responsive HTML email body templates strictly aligned with Webriders' established brand guidelines. The layout engine must dynamically map recently published content highlights and multi-variant product card snapshots safely.
- **Batch Processing Rule:** To protect network resources and prevent connection choking, implement an asynchronous processing mechanism that chunks the target subscription lists into discrete recipient batches before dispatching the payloads to the API grid.

### C. Tiered Communication Routing Matrix
The background notification engine must execute conditional payload mapping depending strictly on the audience subscription level:

#### 1. Standard Subscriber Target Group (Free Tier)
- **Payload Composition:** Standard weekly blog recaps, open store announcements, and basic layout structures.
- **Communication Vector:** Delivered via standard automated notification routines and standard email grids.

#### 2. Premium Subscriber Target Group (Paid/VIP Tier)
- **Payload Composition:** Injects high-value components into the communication payload, including priority product reservation paths, early access content links, and advanced route insights.
- **The Confidential Token Gate:** If the background automation identifies a newly registered or authenticated Premium profile, it must dynamically inject the secure onboarding attributes—specifically the exclusive, non-public automated redirection invitation URL to the private **Webriders VIP WhatsApp Group**.
- **Strict Security Constraint:** These private links and VIP channels must **NEVER** be sent to standard communication queues or exposed within public static frontend content files.
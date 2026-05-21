# 🛠️ Subscription Billing & Stripe Integration Strategy (Fintech) - Webriders

## 1. System Role & Execution Standard
Act as a Principal Billing Engineer and Integration Architect specializing in subscription economics, recurring payment gateways, and event-driven financial syncs. Your objective is to design the automated monthly recurring checkout flow and subscription lifecycle management using the **Stripe** ecosystem for **Webriders**.

You have full technical freedom to choose the client-side checkout redirection patterns, official Stripe SDK structures, and background job retry logic, provided you strictly respect the high-level asynchronous boundary, monthly billing structure, and tier lifecycles detailed below. Do NOT write explicit financial database ledgers or checkout UI forms yet.

---

## 2. Monthly Recurring Lifecycle & Webhook Architecture

To achieve low-latency tier access and absolute separation from synchronous payment verification, the application must process membership changes via asynchronous Stripe Webhooks:

### A. Stripe Hosted Monthly Checkout Flow
- **The Monthly Gateway:** Instead of handling raw card credentials locally, the application layer must offload payment collection completely to secure Stripe Checkout hosted pages configured strictly for **monthly recurring cycles**.
- **Cancel-At-Any-Time Policy:** The checkout properties and customer configurations must allow the rider full autonomy to cancel the active subscription at any moment through the customer portal or external triggers without manual administrator intervention.
- **The Customer Metadata Bridge:** When initiating a checkout session, the system must forward essential tracking references—such as the unique internal user identification token—within Stripe's metadata mapping parameters to secure reliable backend attribution later.

### B. Event-Driven Webhook Handler Enforcement
- **The Ingestion Endpoint:** Expose a secure, unauthenticated backend REST route (e.g., `/api/webhooks/stripe`) designed specifically to capture incoming event payloads pushed directly from Stripe's cloud servers.
- **Cryptographic Signature Verification:** The handler must enforce strict signature parsing using Stripe's official verification keys to guard the system against counterfeit payload attacks.

### C. Tier Lifecycle State Synchronization
The integration service must parse incoming financial transaction events to update user subscription scopes inside the system database automatically:

1. **`checkout.session.completed` & `invoice.payment_succeeded`:**
   - **The Action:** Captures successful checkout updates, first-time monthly activations, or automatic recurring subscription renewals. Inside a single database transaction, locate the customer profile using the metadata reference and elevate their access tier status strictly to `Premium`.
2. **`customer.subscription.deleted` & `invoice.payment_failed`:**
   - **The Action:** Signals that a rider has actively canceled their monthly tier subscription or that automated billing retries have failed over multiple intervals. The system must instantly downgrade the target customer record profile status back to `Free`, immediately revoking premium capabilities.

---

## 3. High-Value Onboarding Automations

- **The VIP Group Admission Link:** When the system successfully processes a premium upgrade event, the background workflow must trigger the automation services to immediately make the exclusive, non-public automated redirection invitation URL to the private **Webriders VIP WhatsApp Group** available to the user profile.
- **The Security Invariant:** If the customer cancellation event occurs (`customer.subscription.deleted`), access to this VIP link and any active premium detail fetching parameters must be immediately blocked at the server layer.
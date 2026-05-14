# SaaS Multi-Tenant Platform

A full-stack SaaS platform starter built with **Angular** and **ASP.NET Core/C#**. The project is separated into a frontend application and backend API, with a structure that keeps routing, components, services, endpoint groups, contracts, domain models, and security concerns out of one-file implementations.

The app demonstrates common SaaS foundations: organization-based multi-tenancy, JWT authentication, role-aware access checks, team invitations, Stripe Checkout-style subscriptions, hashed API keys, and usage analytics.

![Operations overview](docs/screenshots/overview.png)

## Highlights

- Clean frontend/backend separation
- Angular feature routes instead of one giant component
- Shared Angular API client, session store, and dashboard store
- ASP.NET Core endpoint groups instead of one giant `Program.cs`
- Separate backend contracts, domain records, services, persistence, and security helpers
- Organization-scoped dashboard APIs using `X-Organization-ID`
- JWT login, registration, and session restore
- Owner/Admin checks for privileged actions
- Stripe Checkout-shaped billing using Price IDs
- Idempotent Stripe webhook processing
- One-time API key display with SHA-256 storage
- Usage ingestion with `X-API-Key`
- Docker Compose support
- README screenshots stored in `docs/screenshots`

## Screenshots

### Authentication

![Authentication screen](docs/screenshots/authentication.png)

### Overview

![Overview dashboard](docs/screenshots/overview.png)

### Usage Analytics

![Usage analytics](docs/screenshots/usage.png)

### Billing

![Billing plans](docs/screenshots/billing.png)

### Team Management

![Team management](docs/screenshots/team.png)

### API Keys

![API key management](docs/screenshots/api-keys.png)

## Tech Stack

### Frontend

- Angular
- TypeScript
- Angular Router
- Angular Reactive Forms
- Angular HttpClient
- Signals for local UI/application state
- CSS

### Backend

- ASP.NET Core Web API
- C#
- Minimal API endpoint groups
- JWT token generation and validation
- PBKDF2 password hashing
- SHA-256 API key hashing
- In-memory store for demo/local development

### Infrastructure

- Docker
- Docker Compose
- Stripe Billing/Checkout-compatible integration shape

## Senior-Level Project Structure

```text
.
├── backend/
│   ├── Contracts/
│   │   ├── Requests.cs              # Request DTOs for API input
│   │   └── Responses.cs             # Response DTOs sent to the frontend
│   ├── Domain/
│   │   └── Entities.cs              # Core SaaS domain records and enums
│   ├── Endpoints/
│   │   ├── ApiKeyEndpoints.cs       # API key list/create/revoke routes
│   │   ├── AuthEndpoints.cs         # Register/login/me routes
│   │   ├── BillingEndpoints.cs      # Subscription, checkout, webhook routes
│   │   ├── HealthEndpoints.cs       # Health check route
│   │   ├── OrganizationEndpoints.cs # Organization routes
│   │   ├── TeamEndpoints.cs         # Member and invite routes
│   │   └── UsageEndpoints.cs        # Usage summary and ingest routes
│   ├── Persistence/
│   │   ├── PlatformStore.cs         # Demo in-memory store
│   │   └── SeedData.cs              # Demo tenant/user/usage seed data
│   ├── Security/
│   │   ├── CurrentUser.cs           # Request auth context resolver
│   │   ├── PasswordHasher.cs        # PBKDF2 password hashing
│   │   └── TokenService.cs          # JWT create/validate service
│   ├── Services/
│   │   ├── ApiKeyService.cs         # API key generation/authentication
│   │   ├── BillingService.cs        # Checkout-session boundary
│   │   └── StripeWebhookService.cs  # Webhook event handling/idempotency
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile
│   ├── Program.cs                   # Composition root only
│   ├── SaaS.Api.csproj
│   ├── appsettings.Development.json
│   └── appsettings.json
├── frontend/
│   ├── public/
│   │   └── dashboard-bg.svg
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── api-client.service.ts   # Typed HTTP boundary
│   │   │   │   ├── dashboard.store.ts      # Dashboard data/actions
│   │   │   │   ├── models.ts               # Shared frontend interfaces
│   │   │   │   └── session.store.ts        # Auth/session state
│   │   │   ├── features/
│   │   │   │   ├── api-keys/
│   │   │   │   │   ├── api-keys.component.html
│   │   │   │   │   └── api-keys.component.ts
│   │   │   │   ├── auth/
│   │   │   │   │   ├── auth.component.html
│   │   │   │   │   └── auth.component.ts
│   │   │   │   ├── billing/
│   │   │   │   │   ├── billing.component.html
│   │   │   │   │   └── billing.component.ts
│   │   │   │   ├── dashboard/
│   │   │   │   │   ├── dashboard.component.html
│   │   │   │   │   └── dashboard.component.ts
│   │   │   │   ├── overview/
│   │   │   │   │   ├── overview.component.html
│   │   │   │   │   └── overview.component.ts
│   │   │   │   ├── team/
│   │   │   │   │   ├── team.component.html
│   │   │   │   │   └── team.component.ts
│   │   │   │   └── usage/
│   │   │   │       ├── usage.component.html
│   │   │   │       └── usage.component.ts
│   │   │   ├── app.config.ts        # App providers
│   │   │   ├── app.css
│   │   │   ├── app.html             # Router outlet only
│   │   │   ├── app.routes.ts        # Route configuration
│   │   │   └── app.ts               # Root shell component
│   │   ├── index.html
│   │   ├── main.ts
│   │   └── styles.css               # Shared app styling
│   ├── Dockerfile
│   ├── angular.json
│   ├── package-lock.json
│   ├── package.json
│   └── tsconfig.json
├── docs/
│   └── screenshots/
├── .env.example
├── .gitignore
├── docker-compose.yml
├── README.md
└── SaaSMultiTenantPlatform.slnx
```

## Architecture

### Backend Architecture

`Program.cs` is now a composition root. It configures services, middleware, endpoint groups, and seed data only.

Endpoint files own HTTP route mapping and request/response orchestration:

- `AuthEndpoints` handles register, login, and session restore.
- `OrganizationEndpoints` handles tenant lookup and creation.
- `TeamEndpoints` handles members and invitations.
- `BillingEndpoints` handles subscription state, checkout creation, and webhooks.
- `ApiKeyEndpoints` handles API key lifecycle.
- `UsageEndpoints` handles usage analytics and ingestion.

Business support code lives outside endpoint files:

- `TokenService` creates and validates JWTs.
- `CurrentUser` resolves the authenticated user and organization context from each request.
- `PasswordHasher` handles password hashing and verification.
- `ApiKeyService` generates API keys and authenticates usage-ingestion requests.
- `BillingService` owns the Checkout Session boundary.
- `StripeWebhookService` owns webhook event handling and idempotency.
- `PlatformStore` is the current in-memory persistence layer.

This structure makes it much easier to replace the demo store with Entity Framework Core later without rewriting the API surface.

### Frontend Architecture

The frontend is route-driven and split by feature.

Routes:

```text
/auth
/overview
/usage
/billing
/team
/api-keys
```

Core services:

- `ApiClient` is the typed HTTP layer.
- `SessionStore` owns login, registration, JWT storage, session restore, logout, and auth headers.
- `DashboardStore` owns subscription, team, usage, API keys, checkout, invites, and sample usage actions.

Feature components stay focused on UI and forms:

- `AuthComponent`
- `DashboardComponent`
- `OverviewComponent`
- `UsageComponent`
- `BillingComponent`
- `TeamComponent`
- `ApiKeysComponent`

## Local Setup

### Prerequisites

- .NET SDK 10 or newer
- Node.js 22 or newer
- npm
- Docker Desktop, optional
- Stripe account, optional for real billing integration

### Clone

```bash
git clone https://github.com/brianmahlatini/SaaS-Multi-Tenant-Platform.git
cd SaaS-Multi-Tenant-Platform
```

### Configure Environment

Copy `.env.example` if you want environment-variable configuration:

```bash
cp .env.example .env
```

Important values:

```text
Jwt__SigningKey=replace-with-a-long-random-value-at-least-32-chars
Cors__Origins__0=http://localhost:4200
Stripe__ApiKey=sk_test_replace_me
Stripe__WebhookSecret=whsec_replace_me
Stripe__ProPriceId=price_pro_replace_me
Stripe__EnterprisePriceId=price_enterprise_replace_me
```

The demo also has development defaults in `backend/appsettings.json`.

### Install Dependencies

Backend:

```bash
dotnet restore backend/SaaS.Api.csproj
```

Frontend:

```bash
cd frontend
npm install
cd ..
```

### Run Locally

Start the backend:

```bash
dotnet run --project backend/SaaS.Api.csproj --launch-profile http
```

Start the frontend:

```bash
cd frontend
npm start
```

Open:

```text
Frontend: http://localhost:4200
Backend health: http://localhost:5000/api/health
OpenAPI JSON: http://localhost:5000/openapi/v1.json
```

## Demo Account

```text
Email: owner@example.com
Password: ChangeMe123!
Organization: Acme Cloud
Role: Owner
Plan: Pro
```

## Docker

Run both apps:

```bash
docker compose up --build
```

Stop services:

```bash
docker compose down
```

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Create a user and first organization |
| POST | `/api/auth/login` | Issue JWT token |
| GET | `/api/auth/me` | Restore current authenticated session |

### Organizations

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/organizations/list` | List user organizations |
| POST | `/api/organizations` | Create a new organization |
| GET | `/api/organizations/current` | Get active organization |

### Team

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/users` | List organization members |
| POST | `/api/users/invite` | Queue teammate invitation |

### Billing

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/billing/subscription` | Get current subscription |
| POST | `/api/billing/checkout` | Create Checkout-shaped session |
| POST | `/api/billing/webhook` | Receive Stripe events |

### API Keys

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/api-keys` | List organization API keys |
| POST | `/api/api-keys` | Generate API key |
| DELETE | `/api/api-keys/{id}` | Revoke API key |

### Usage

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/usage` | Usage totals, trend, and activity |
| POST | `/api/usage/ingest` | Ingest usage using API key |

## Example Usage Ingestion

Create an API key in the dashboard, copy the one-time secret, then send usage:

```bash
curl -X POST http://localhost:5000/api/usage/ingest \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sk_test_your_key" \
  -d "{\"path\":\"/v1/events\",\"method\":\"POST\",\"statusCode\":202,\"units\":3}"
```

## Stripe Billing Notes

This project is structured around Stripe Billing plus Checkout Sessions for subscriptions.

Current demo behavior:

- Billing requests accept `pro` and `enterprise`.
- Price IDs come from configuration.
- `BillingService` returns a Checkout-shaped test URL so the demo runs without Stripe credentials.
- `StripeWebhookService` stores processed event IDs to prevent duplicate processing.

Production work to add:

- Add Stripe.net.
- Replace demo checkout URL generation with `SessionService.Create`.
- Use Checkout `mode=subscription`.
- Pass `organization_id` and `plan` metadata.
- Verify webhook signatures with `Stripe__WebhookSecret`.
- Add Stripe Customer Portal for self-service billing.

## Security Notes

- Passwords are hashed with PBKDF2.
- API keys are shown once and stored as SHA-256 hashes.
- Dashboard requests require JWT Bearer tokens.
- Organization access is checked through memberships.
- Owner/Admin-only operations are enforced in backend endpoints.
- Stripe event IDs are stored for webhook idempotency.
- `.env` is ignored by Git.

## Development Commands

Backend build:

```bash
dotnet build backend/SaaS.Api.csproj
```

Frontend build:

```bash
cd frontend
npm run build
```

Run frontend:

```bash
cd frontend
npm start
```

Run backend:

```bash
dotnet run --project backend/SaaS.Api.csproj --launch-profile http
```

## Current Demo Limits

- Data is in memory and resets when the backend restarts.
- Invitations are recorded but not emailed.
- Checkout is integration-shaped but does not call Stripe.net yet.
- Webhook signature verification is a production next step.
- Automated tests are not included yet.

## Production Roadmap

- Replace `PlatformStore` with PostgreSQL and Entity Framework Core.
- Add database migrations and repository/query abstractions.
- Add ASP.NET Core authentication and authorization middleware policies.
- Add refresh tokens or secure short-lived session handling.
- Add real email delivery for invitations.
- Integrate Stripe.net Checkout and signature-verified webhooks.
- Add Stripe Customer Portal.
- Add API-key rate limiting by plan.
- Add backend unit/integration tests.
- Add Angular component and route tests.
- Add CI/CD for build, test, Docker image publish, and deployment.
- Move secrets to a managed secret store.
- Add structured logging, monitoring, tracing, and error tracking.

## License

This project is provided as a portfolio and learning SaaS platform starter. Add a license before using it commercially.

# SaaS Multi-Tenant Platform

A full-stack SaaS starter built with **Angular** on the frontend and **ASP.NET Core/C#** on the backend. The app demonstrates the core building blocks of a production SaaS product: organization-based multi-tenancy, JWT authentication, role-aware team management, subscription billing, API key generation, and API usage analytics.

The repository is intentionally split into separate frontend and backend projects so each side can be developed, deployed, and scaled independently.

![Operations overview](docs/screenshots/overview.png)

## Features

- Multi-tenant organizations with organization-scoped dashboard APIs
- Demo owner account with seeded organization and usage history
- JWT login, registration, and session restore
- Role-aware access checks for Owner, Admin, and Member-style actions
- Team member listing and invitation workflow placeholder
- Stripe Checkout-shaped subscription upgrade flow using Stripe Price IDs
- Stripe webhook endpoint with idempotent event processing
- Free, Pro, and Enterprise billing plan UI
- Secure API key generation with one-time secret display
- SHA-256 API key storage, revocation, and last-used tracking
- API usage ingestion endpoint using `X-API-Key`
- Usage summary metrics and recent API activity table
- Docker Compose setup for running frontend and backend together
- Detailed `.env.example` for local configuration

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
- RxJS
- Angular Reactive Forms
- Angular HttpClient
- CSS

### Backend

- ASP.NET Core Web API
- C#
- Minimal APIs
- JWT tokens
- PBKDF2 password hashing
- SHA-256 API key hashing
- In-memory data store for local demo behavior

### Tooling and Infrastructure

- .NET SDK
- Node.js and npm
- Docker
- Docker Compose
- Stripe Checkout-compatible billing design

## Project Folder Structure

```text
.
├── backend/
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile
│   ├── Program.cs
│   ├── SaaS.Api.csproj
│   ├── appsettings.Development.json
│   └── appsettings.json
├── docs/
│   └── screenshots/
│       ├── api-keys.png
│       ├── authentication.png
│       ├── billing.png
│       ├── overview.png
│       ├── team.png
│       └── usage.png
├── frontend/
│   ├── public/
│   │   └── dashboard-bg.svg
│   ├── src/
│   │   ├── app/
│   │   │   ├── app.config.ts
│   │   │   ├── app.css
│   │   │   ├── app.html
│   │   │   └── app.ts
│   │   ├── index.html
│   │   ├── main.ts
│   │   └── styles.css
│   ├── Dockerfile
│   ├── angular.json
│   ├── package-lock.json
│   ├── package.json
│   ├── tsconfig.app.json
│   ├── tsconfig.json
│   └── tsconfig.spec.json
├── .env.example
├── .gitignore
├── docker-compose.yml
├── README.md
└── SaaSMultiTenantPlatform.slnx
```

## Architecture

The backend exposes REST-style API endpoints under `/api`. Dashboard endpoints use JWT Bearer authentication and are scoped to the active organization through the `X-Organization-ID` header.

API usage ingestion is intentionally separate from dashboard authentication. External clients submit usage events with an API key using the `X-API-Key` header.

The frontend stores the JWT in `localStorage`, restores the current user session on page load, and sends authenticated requests to the ASP.NET Core API. The dashboard is organized into five main views:

- **Overview**: usage totals, requests, errors, team count, usage trend, and subscription status
- **Usage**: recent API activity and sample usage ingestion
- **Billing**: Free, Pro, and Enterprise plans with Checkout buttons
- **Team**: current members and invitation form
- **API Keys**: key generation, one-time secret display, and revocation

## Local Setup

### Prerequisites

- .NET SDK 10 or newer
- Node.js 22 or newer
- npm
- Docker Desktop, optional
- Stripe account, optional for real billing integration

### 1. Clone the Repository

```bash
git clone https://github.com/brianmahlatini/SaaS-Multi-Tenant-Platform.git
cd SaaS-Multi-Tenant-Platform
```

### 2. Configure Environment

Copy the example env file if you want to use environment variables:

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

The app also includes development defaults in `backend/appsettings.json`, so it can run locally without Stripe credentials.

### 3. Restore Backend Dependencies

```bash
dotnet restore backend/SaaS.Api.csproj
```

### 4. Install Frontend Dependencies

```bash
cd frontend
npm install
cd ..
```

### 5. Run the Backend

```bash
dotnet run --project backend/SaaS.Api.csproj --launch-profile http
```

The backend runs at:

```text
http://localhost:5000
```

Health check:

```text
http://localhost:5000/api/health
```

OpenAPI JSON:

```text
http://localhost:5000/openapi/v1.json
```

### 6. Run the Frontend

```bash
cd frontend
npm start
```

The frontend runs at:

```text
http://localhost:4200
```

## Demo Login

The backend seeds a demo tenant on startup:

```text
Email: owner@example.com
Password: ChangeMe123!
Organization: Acme Cloud
Plan: Pro
Role: Owner
```

## Docker Setup

Run both applications together:

```bash
docker compose up --build
```

Services:

- Frontend: `http://localhost:4200`
- Backend: `http://localhost:5000`

Stop the stack:

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
| GET | `/api/organizations/list` | List organizations for the current user |
| POST | `/api/organizations` | Create a new organization |
| GET | `/api/organizations/current` | Get active organization |

### Team

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/users` | List organization members |
| POST | `/api/users/invite` | Queue an invitation for a teammate |

### Billing

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/billing/subscription` | Get current organization subscription |
| POST | `/api/billing/checkout` | Create a Checkout-shaped subscription session |
| POST | `/api/billing/webhook` | Receive Stripe webhook events |

### API Keys

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/api-keys` | List organization API keys |
| POST | `/api/api-keys` | Generate a new API key |
| DELETE | `/api/api-keys/{id}` | Revoke an API key |

### Usage

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/usage` | Get usage totals, trend, and recent events |
| POST | `/api/usage/ingest` | Ingest usage using an API key |

## Example Usage Ingestion

Create an API key in the dashboard, copy the one-time secret, and send a usage event:

```bash
curl -X POST http://localhost:5000/api/usage/ingest \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sk_test_your_key" \
  -d "{\"path\":\"/v1/events\",\"method\":\"POST\",\"statusCode\":202,\"units\":3}"
```

## Stripe Billing Notes

This starter follows the recommended SaaS pattern of using Stripe Billing with Checkout Sessions for subscriptions.

Current local behavior:

- The backend accepts `pro` and `enterprise` checkout requests.
- Price IDs are read from configuration.
- A Checkout-shaped test URL is returned so the demo can run without real Stripe credentials.
- Webhook events are tracked by event ID to avoid duplicate processing.

Production Stripe work to add:

- Install and configure Stripe.net.
- Replace the demo Checkout URL generation with `SessionService.Create`.
- Use `mode=subscription`.
- Pass `organization_id` and `plan` in Checkout metadata.
- Verify `Stripe-Signature` using `Stripe__WebhookSecret`.
- Use real Stripe Price IDs for Pro and Enterprise.

## Security Notes

- Passwords are hashed with PBKDF2 before storage.
- API keys are shown once and stored as SHA-256 hashes.
- Dashboard APIs require JWT Bearer tokens.
- Organization access is checked through memberships.
- Owner/Admin actions are protected in backend endpoints.
- Stripe webhook event IDs are stored to prevent duplicate processing.
- `.env` is ignored and should never be committed.

## Development Commands

Build backend:

```bash
dotnet build backend/SaaS.Api.csproj
```

Build frontend:

```bash
cd frontend
npm run build
```

Run frontend dev server:

```bash
cd frontend
npm start
```

Run backend dev server:

```bash
dotnet run --project backend/SaaS.Api.csproj --launch-profile http
```

## Current Demo Limitations

This repository is a working starter, but it keeps the first version lightweight:

- Data is stored in memory and resets when the backend restarts.
- Team invitations are recorded but not emailed.
- Stripe Checkout is shaped for integration but does not call Stripe.net yet.
- Webhook signature verification is listed as a production next step.
- Automated tests are not included yet.

## Production Roadmap

Before deploying as a real SaaS product:

- Replace the in-memory store with PostgreSQL and Entity Framework Core.
- Add ASP.NET Core authentication and authorization middleware policies.
- Add refresh tokens or short-lived access tokens with secure refresh flow.
- Add email delivery for invitations.
- Add Stripe.net Checkout Session creation and signature-verified webhooks.
- Add subscription customer portal support.
- Add database migrations and seed scripts.
- Add rate limiting for API-key usage ingestion.
- Add backend and frontend automated tests.
- Add CI/CD with build, test, and container publish steps.
- Move secrets to a managed secret store.
- Configure production CORS, HTTPS, logging, monitoring, and error tracking.

## License

This project is provided as a portfolio and learning SaaS platform starter. Add a license before using it commercially.

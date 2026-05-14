# SaaS Multi-Tenant Platform

A full-stack SaaS platform starter built with **Angular** and **ASP.NET Core/C#**. The project is separated into a frontend application and backend API, with feature routes, endpoint groups, services, contracts, security helpers, caching, background jobs, queue messaging, real-time updates, rate limiting, and monitoring.

![Operations overview](docs/screenshots/overview.png)

## Implemented Features

- Multi-tenant organizations with organization-scoped dashboard APIs
- JWT login, registration, and session restore
- Owner/Admin/Member role model with privileged endpoint checks
- Angular routed dashboard: `/auth`, `/overview`, `/usage`, `/billing`, `/team`, `/api-keys`
- Stripe Checkout-shaped subscription flow using Stripe Price IDs
- Stripe webhook endpoint with idempotent event handling
- API key generation with one-time secret display
- SHA-256 API key storage, revocation, and last-used tracking
- API usage ingestion using `X-API-Key`
- Redis-backed distributed caching for usage summaries
- Hosted background job queue for async tasks
- RabbitMQ event publishing and consumer service
- SignalR WebSocket hub for real-time usage updates
- ASP.NET Core rate limiting for dashboard and ingest APIs
- Structured request logging middleware
- Lightweight monitoring metrics endpoint
- Docker Compose services for backend, frontend, PostgreSQL, MongoDB, Redis, and RabbitMQ

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
- Angular Signals
- Microsoft SignalR client
- CSS

### Backend

- ASP.NET Core Web API
- C#
- Minimal API endpoint groups
- JWT tokens
- SignalR
- Redis distributed cache via `IDistributedCache`
- RabbitMQ event bus
- Hosted background services
- ASP.NET Core rate limiting
- PBKDF2 password hashing
- SHA-256 API key hashing

### Infrastructure

- Docker
- Docker Compose
- Redis
- PostgreSQL
- MongoDB
- Prisma schema/tooling
- RabbitMQ with management UI
- Stripe Billing/Checkout-compatible boundary

## Project Structure

```text
.
|-- backend/
|   |-- Contracts/
|   |   |-- Requests.cs
|   |   `-- Responses.cs
|   |-- Domain/
|   |   `-- Entities.cs
|   |-- Endpoints/
|   |   |-- ApiKeyEndpoints.cs
|   |   |-- AuthEndpoints.cs
|   |   |-- BillingEndpoints.cs
|   |   |-- HealthEndpoints.cs
|   |   |-- OrganizationEndpoints.cs
|   |   |-- TeamEndpoints.cs
|   |   `-- UsageEndpoints.cs
|   |-- Infrastructure/
|   |   |-- Caching/
|   |   |   `-- CacheKeys.cs
|   |   |-- Jobs/
|   |   |   |-- BackgroundJobQueue.cs
|   |   |   |-- IBackgroundJobQueue.cs
|   |   |   `-- QueuedHostedService.cs
|   |   |-- Messaging/
|   |   |   |-- IEventBus.cs
|   |   |   |-- PlatformEvent.cs
|   |   |   |-- RabbitMqConsumerService.cs
|   |   |   |-- RabbitMqEventBus.cs
|   |   |   `-- RabbitMqOptions.cs
|   |   `-- Monitoring/
|   |       |-- AppMetrics.cs
|   |       `-- RequestLoggingMiddleware.cs
|   |-- Persistence/
|   |   |-- PlatformStore.cs
|   |   `-- SeedData.cs
|   |-- Realtime/
|   |   `-- RealtimeHub.cs
|   |-- Security/
|   |   |-- CurrentUser.cs
|   |   |-- PasswordHasher.cs
|   |   `-- TokenService.cs
|   |-- Services/
|   |   |-- ApiKeyService.cs
|   |   |-- BillingService.cs
|   |   `-- StripeWebhookService.cs
|   |-- Dockerfile
|   |-- Program.cs
|   |-- SaaS.Api.csproj
|   `-- appsettings.json
|-- frontend/
|   |-- public/
|   |   `-- dashboard-bg.svg
|   |-- src/
|   |   |-- app/
|   |   |   |-- core/
|   |   |   |   |-- api-client.service.ts
|   |   |   |   |-- dashboard.store.ts
|   |   |   |   |-- models.ts
|   |   |   |   |-- realtime.service.ts
|   |   |   |   `-- session.store.ts
|   |   |   |-- features/
|   |   |   |   |-- api-keys/
|   |   |   |   |-- auth/
|   |   |   |   |-- billing/
|   |   |   |   |-- dashboard/
|   |   |   |   |-- overview/
|   |   |   |   |-- team/
|   |   |   |   `-- usage/
|   |   |   |-- app.config.ts
|   |   |   |-- app.routes.ts
|   |   |   |-- app.ts
|   |   |   `-- app.html
|   |   |-- index.html
|   |   |-- main.ts
|   |   `-- styles.css
|   |-- Dockerfile
|   |-- angular.json
|   |-- package-lock.json
|   `-- package.json
|-- database/
|   |-- package.json
|   `-- prisma/
|       `-- schema.prisma
|-- docs/
|   `-- screenshots/
|-- .env.example
|-- docker-compose.yml
|-- README.md
`-- SaaSMultiTenantPlatform.slnx
```

## Backend Architecture

`Program.cs` is the composition root only. It registers services, configures CORS, SignalR, Redis caching, RabbitMQ options, rate limiter policies, hosted workers, and endpoint groups.

Endpoint groups own routing:

- `AuthEndpoints` handles register, login, and session restore.
- `OrganizationEndpoints` handles tenant lookup and creation.
- `TeamEndpoints` handles members and invitations.
- `BillingEndpoints` handles subscription state, checkout, and webhooks.
- `ApiKeyEndpoints` handles API key lifecycle.
- `UsageEndpoints` handles cached usage analytics and API-key ingestion.
- `HealthEndpoints` exposes health and monitoring metrics.

Infrastructure code is separated:

- `Caching` contains cache key conventions.
- `Jobs` contains a channel-backed background job queue and hosted worker.
- `Messaging` contains RabbitMQ event publishing and consumption.
- `Monitoring` contains request logging and in-process metrics.
- `Realtime` contains the SignalR hub.
- `Persistence/Postgres` contains the EF Core PostgreSQL context and projection service.
- `Persistence/Mongo` contains MongoDB usage and audit document storage.

### Database Split

PostgreSQL is used for transactional SaaS data:

- users
- organizations
- memberships and roles
- invitations
- subscriptions
- API keys
- processed Stripe webhook event IDs

MongoDB is used where document storage is a better fit:

- high-volume usage event documents
- audit/event documents emitted by the platform

Prisma is included under `database/prisma/schema.prisma` as a schema/tooling contract for the PostgreSQL model. The ASP.NET Core runtime uses EF Core/Npgsql because Prisma is a Node.js ORM and is not the runtime data layer for this C# backend.

## Frontend Architecture

The Angular app is route-driven and split by feature.

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

- `ApiClient` is the typed HTTP boundary.
- `SessionStore` owns JWT/session state and auth headers.
- `DashboardStore` owns dashboard data and user actions.
- `RealtimeService` owns the SignalR connection and refreshes usage after real-time events.

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
- Docker Desktop, optional for PostgreSQL/MongoDB/Redis/RabbitMQ stack
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
ConnectionStrings__Redis=localhost:6379
RabbitMQ__Enabled=true
RabbitMQ__HostName=localhost
RabbitMQ__Port=5672
RabbitMQ__UserName=guest
RabbitMQ__Password=guest
RabbitMQ__QueueName=saas-platform-events
Stripe__ApiKey=sk_test_replace_me
Stripe__WebhookSecret=whsec_replace_me
Stripe__ProPriceId=price_pro_replace_me
Stripe__EnterprisePriceId=price_enterprise_replace_me
```

The backend falls back to the in-memory demo store when no PostgreSQL connection string is configured, and it falls back to in-memory distributed cache when no Redis connection string is configured. MongoDB writes are skipped when no MongoDB connection string is configured. RabbitMQ is disabled by default in `appsettings.json` and enabled in Docker Compose.

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

### Run Locally Without Docker

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
Monitoring metrics: http://localhost:5000/api/monitoring/metrics
SignalR hub: http://localhost:5000/hubs/realtime
OpenAPI JSON: http://localhost:5000/openapi/v1.json
```

## Docker

Run the full stack:

```bash
docker compose up --build
```

Services:

```text
Frontend: http://localhost:4200
Backend: http://localhost:5000
PostgreSQL: localhost:5432
MongoDB: localhost:27017
Redis: localhost:6379
RabbitMQ: localhost:5672
RabbitMQ Management UI: http://localhost:15672
RabbitMQ login: guest / guest
PostgreSQL login: saas / saas_password
```

Stop services:

```bash
docker compose down
```

## Demo Accounts

```text
Owner
Email: owner@example.com
Password: ChangeMe123!
Access: Full workspace access, billing, team, API keys, usage

Admin
Email: admin@example.com
Password: ChangeMe123!
Access: Full operational access for team, API keys, usage, and billing actions

Member
Email: member@example.com
Password: ChangeMe123!
Access: Read/monitor workspace data; restricted from owner/admin actions

Organization: Acme Cloud
Plan: Pro
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
| POST | `/api/users/invite` | Queue teammate invitation job and publish event |

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
| GET | `/api/usage` | Cached usage totals, trend, and activity |
| POST | `/api/usage/ingest` | Ingest usage using API key, invalidate cache, publish event, and notify SignalR clients |

### Monitoring

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/health` | API health check |
| GET | `/api/monitoring/metrics` | Uptime, request counts, failed requests, and request counts by path |

## Example Usage Ingestion

Create an API key in the dashboard, copy the one-time secret, then send usage:

```bash
curl -X POST http://localhost:5000/api/usage/ingest \
  -H "Content-Type: application/json" \
  -H "X-API-Key: sk_test_your_key" \
  -d "{\"path\":\"/v1/events\",\"method\":\"POST\",\"statusCode\":202,\"units\":3}"
```

## Infrastructure Details

### Redis Caching

`UsageEndpoints` caches usage summaries with `IDistributedCache`. When usage is ingested, the organization usage cache key is invalidated. If Redis is configured, the cache is distributed. If not, the app uses in-memory distributed cache for local development.

### PostgreSQL

`PlatformDbContext` defines relational tables for the core SaaS model. `PostgresProjectionService` syncs the running store into PostgreSQL and can hydrate the app from PostgreSQL when a database already contains data. This keeps local demo mode simple while providing a real PostgreSQL persistence path.

### MongoDB

`MongoUsageService` writes usage events and audit events as documents. This keeps high-volume event-style data separate from transactional account and billing records.

### Prisma

`database/prisma/schema.prisma` mirrors the PostgreSQL model as a Prisma schema. It is included for schema review, Prisma tooling, and teams that want a Node-based data tooling workflow alongside the ASP.NET backend. The ASP.NET runtime still uses EF Core/Npgsql.

### Background Jobs

`IBackgroundJobQueue` and `QueuedHostedService` provide a lightweight hosted background job system. Team invitations enqueue an async invitation job after the API request stores the invitation.

### RabbitMQ

`RabbitMqEventBus` publishes platform events such as `team.invitation.created` and `usage.ingested`. `RabbitMqConsumerService` consumes from the configured queue and logs processed messages. RabbitMQ is enabled in Docker Compose.

### WebSockets

`RealtimeHub` is a SignalR hub at `/hubs/realtime`. The Angular `RealtimeService` connects after login, joins the organization group, and refreshes usage when `usageUpdated` events arrive.

### API Rate Limiting

The backend defines two policies:

- `dashboard`: 120 requests per minute per remote IP
- `ingest`: 60 requests per minute per API key or remote IP

### Logging and Monitoring

`RequestLoggingMiddleware` logs method, path, status code, and elapsed time for every request. `AppMetrics` tracks uptime, total requests, failed requests, and request counts by path.

## Stripe Billing Notes

This project is structured around Stripe Billing plus Checkout Sessions for subscriptions.

Current demo behavior:

- Billing requests accept `pro` and `enterprise`.
- Price IDs come from configuration.
- `BillingService` returns a Checkout-shaped test URL so the demo runs without Stripe credentials.
- `StripeWebhookService` stores processed event IDs to prevent duplicate processing.

Production work to add:

- Add real Stripe.net Checkout Session creation.
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
- Rate limits protect dashboard and ingest endpoints.
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

- `PlatformStore` is still the in-process working projection; PostgreSQL is used as the durable relational projection when configured.
- Invitations are queued and logged but not sent through a real email provider.
- Checkout is integration-shaped but does not call Stripe.net yet.
- Webhook signature verification is a production next step.
- Automated tests are not included yet.

## Production Roadmap

- Replace the projection-style `PlatformStore` bridge with direct repository/query services over PostgreSQL.
- Add EF Core migrations instead of `EnsureCreated`.
- Add ASP.NET Core authentication and authorization middleware policies.
- Add refresh tokens or secure short-lived session handling.
- Add real email delivery for invitations.
- Integrate Stripe.net Checkout and signature-verified webhooks.
- Add Stripe Customer Portal.
- Add durable job storage or MassTransit for larger distributed workflows.
- Add API-key rate plans per subscription tier.
- Add backend unit/integration tests.
- Add Angular component and route tests.
- Add CI/CD for build, test, Docker image publish, and deployment.
- Move secrets to a managed secret store.
- Add production OpenTelemetry/Sentry/Application Insights export.

## License

This project is provided as a portfolio and learning SaaS platform starter. Add a license before using it commercially.

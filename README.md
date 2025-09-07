A clean, modular e-commerce Web API with ASP.NET Core 8, Entity Framework Core, Identity, JWT + Refresh tokens, rate limiting, lockout policies, Redis caching, Cloudinary uploads, and Stripe payments—a .NET port of your earlier Express app.

✨ Features

Authentication & Authorization

ASP.NET Core Identity (password hashs, lockout, email uniqueness)

JWT access token + refresh token rotation (persisted; hashed; revocation)

Role-based auth (admin, user)

Rate limiting on auth endpoints, password policies, lockout on bad attempts

Catalog

Products (CRUD, images via Cloudinary, categories, ratings, stock)

Reviews (create/update/delete, average rating calc)

Listings with search/sort/paging + Redis response caching

Orders

Place, process (Processing → Shipped → Delivered), delete

Inventory stock reduction

My orders, all orders (admin)

Redis caching & invalidation

Payments

Stripe Payment Intents

Coupons (CRUD, validate/apply)

Totals: subtotal, tax, shipping, discount

Dashboards

Admin KPIs: revenue, users, products, orders (M/M delta)

Pie/Bar/Line charts data endpoints

Ops

Swagger (Dev), rate limiting, CORS, logging, health

User Secrets for local secrets; env variables for prod

Dockerfile ready

🗺 Architecture

Swagger UI


Admin Dashboard (sample web client)


Postman — Auth Flow


🔐 Configuration & Secrets

Use User Secrets for local dev, and env vars for staging/prod.

appsettings.json (keep keys only; values via secrets/env)
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "Jwt": {
    "Issuer": "",
    "Audience": "",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "SigningKey": ""
  },
  "Redis": { "ConnectionString": "" },
  "Stripe": { "ApiKey": "" },
  "Cloudinary": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": ""
  },
  "RateLimiting": { "PermitLimit": 5, "WindowSeconds": 10, "QueueLimit": 0 },
  "AllowedHosts": "*"
}

User Secrets (Development)
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=EcomDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Issuer" "https://localhost"
dotnet user-secrets set "Jwt:Audience" "https://localhost"
dotnet user-secrets set "Jwt:SigningKey" "<long-random-256bit>"
dotnet user-secrets set "Stripe:ApiKey" "<stripe-secret>"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
dotnet user-secrets set "Cloudinary:CloudName" "<name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<secret>"


Note: Secrets are automatically loaded in Development (no extra code needed) when your .csproj contains a <UserSecretsId>.

🧰 Setup

Restore & build

dotnet restore
dotnet build


Database

dotnet ef database update


(If you haven’t created migrations yet)

dotnet ef migrations add Initial
dotnet ef database update


Run

dotnet run --project src/EcomApi/EcomApi.csproj


Swagger (Dev): https://localhost:5001/swagger

🧑‍💻 Authentication Module
Identity Policies (sample)

Password: min length 8, uppercase/lowercase/digit/special

Lockout: 5 failed attempts → 15 minutes

Unique email

JWT + Refresh

Access token: short lived (e.g., 15 min)

Refresh token: 7 days (rotated; hashed; revocable)

Stored in RefreshTokens with: TokenHash, UserId, Expires, IsRevoked, ReplacedByTokenHash, CreatedByIp

sequenceDiagram
  Client->>API: POST /auth/login (email, pwd)
  API->>Identity: Validate + lockout policy
  API->>API: Issue AccessToken (JWT)
  API->>SQL: Create RefreshToken (hashed)
  API-->>Client: { accessToken, refreshToken }

  Client->>API: POST /auth/refresh (refreshToken)
  API->>SQL: Validate & not revoked & not expired
  API->>SQL: Revoke old, add new refresh token
  API-->>Client: { accessToken, refreshToken }


Endpoints (Auth):

POST /api/auth/register

POST /api/auth/login

POST /api/auth/refresh

POST /api/auth/logout (revoke refresh token)

GET /api/auth/me (requires Authorization: Bearer <token>)

🧾 Modules & Endpoints
Users

GET /api/users (admin)

GET /api/users/{id} (admin)

DELETE /api/users/{id} (admin)

Products

POST /api/products (admin; multipart for photos)

GET /api/products (filters: search, category, price, sort, page)

GET /api/products/latest

GET /api/products/categories

GET /api/products/admin-products (admin)

GET /api/products/{id}

PUT /api/products/{id} (admin; optional photos)

DELETE /api/products/{id} (admin)

Reviews

GET /api/products/reviews/{productId}

POST /api/products/review/new/{productId}

DELETE /api/products/review/{reviewId}

Orders

POST /api/orders/new

GET /api/orders/my

GET /api/orders/all (admin)

GET /api/orders/{id}

PUT /api/orders/{id} (admin → process status)

DELETE /api/orders/{id} (admin)

Payments

POST /api/payment/create (Stripe PaymentIntent)

GET /api/payment/discount?coupon=CODE

POST /api/payment/coupon/new (admin)

GET /api/payment/coupon/all (admin)

GET|PUT|DELETE /api/payment/coupon/{id} (admin)

Dashboard

GET /api/dashboard/stats (admin)

GET /api/dashboard/pie (admin)

GET /api/dashboard/bar (admin)

GET /api/dashboard/line (admin)

🧪 Quick cURL (Auth)
# Register
curl -X POST https://localhost:5001/api/auth/register \
 -H "Content-Type: application/json" \
 -d '{"id":"user-1","email":"a@b.com","password":"P@ssw0rd!","photo":"...","gender":"male","dob":"2000-01-01"}'

# Login
curl -X POST https://localhost:5001/api/auth/login \
 -H "Content-Type: application/json" \
 -d '{"email":"a@b.com","password":"P@ssw0rd!"}'

# Refresh
curl -X POST https://localhost:5001/api/auth/refresh \
 -H "Content-Type: application/json" \
 -d '{"refreshToken":"<raw-token-from-login>"}'

🚦 Rate Limiting & Lockout

Rate limiting (e.g., PermitLimit=5 per WindowSeconds=10) applied on:

/api/auth/login, /api/auth/register, /api/auth/refresh

Identity lockout: after 5 failed logins → locked 15 minutes

Tune these in appsettings.json (values come from User Secrets in dev).

🧊 Redis Caching Strategy

Keys:

latest-products, categories, all-products

product-{id}, reviews-{productId}

my-orders-{userId}, all-orders, order-{id}

admin-stats, admin-pie-charts, admin-bar-charts, admin-line-charts

Invalidation on create/update/delete of products, orders, reviews

☁️ Cloudinary & Stripe

Cloudinary upload via multipart (photos[]) → returns public_id + secure_url

Stripe: Payment Intent created using computed totals
(tax 18%, shipping free > 1000, else 200) — tweakable.

🐳 Docker

Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./src/EcomApi/EcomApi.csproj
RUN dotnet publish ./src/EcomApi/EcomApi.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcomApi.dll"]


Run with env vars for prod secrets:

docker build -t ecomapi .
docker run -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=sql;Database=EcomDb;User Id=sa;Password=..." \
  -e "Jwt__SigningKey=..." \
  -e "Stripe__ApiKey=..." \
  -e "Redis__ConnectionString=redis:6379" \
  ecomapi

✅ Git Hygiene

Never commit secrets. .gitignore already excludes secrets.json.

Commit early, meaningful messages:

feat(auth): add JWT+refresh with rotation & revocation

feat(products): upload images to Cloudinary

perf(cache): add Redis caching for product listing

fix(auth): correct lockout response message

chore(ci): add build & test workflow

Create feature branches: feat/auth, feat/orders, fix/refresh-rotation and PR into develop → merge to main when stable.

🧰 Troubleshooting

JWT invalid? Check Jwt:SigningKey, issuer/audience, system clock drift.

Login lockout too aggressive? Adjust Identity lockout settings.

Redis not hit? Verify keys & TTL; ensure invalidation covers all write paths.

SQL trust issue locally? Add TrustServerCertificate=True to connection string.

📄 License

MIT – use freely, contributions welcome.

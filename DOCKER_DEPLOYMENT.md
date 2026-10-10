# MONIC Universal Docker Deployment Guide

This guide covers building, fine-tuning, and deploying **MONIC** across multiple environments:
- 💻 **Local Docker (Windows / macOS / Linux)**
- 🚀 **Coolify (Self-Hosted PaaS)**
- ☁️ **Render (Cloud Web Service)**
- 🐧 **Standalone Linux VPS (Ubuntu / Debian / CentOS)**
- 🟧 **Amazon Web Services (AWS ECS / App Runner / EC2)**

---

## 🏗️ Architecture

```
┌────────────────────────────────────────────────────────┐
│               Reverse Proxy / Cloud CDN                │
│       (Coolify Traefik / Render / AWS ALB / Nginx)     │
└───────────────────────────┬────────────────────────────┘
                            │ HTTP :80 or :PORT
┌───────────────────────────▼────────────────────────────┐
│              monic-app (Docker Container)               │
│   • ASP.NET Core 10 Backend + Angular Production SPA   │
│   • Multi-stage build (node:20-alpine + dotnet:10.0)   │
│   • Health check: GET /health                          │
│   • Persistent volume: /app/data (Keys & Tokens)       │
└───────────────────────────┬────────────────────────────┘
                            │ TDS Protocol :1433
┌───────────────────────────▼────────────────────────────┐
│              monic-db (SQL Server 2022 / 2025)          │
│   • Containerized (mcr.microsoft.com/mssql/server)     │
│   • Or Cloud Managed (AWS RDS, Azure SQL)              │
│   • Persistent volume: /var/opt/mssql                  │
└────────────────────────────────────────────────────────┘
```

---

## ⚡ 1. Local Machine Deployment (Docker Desktop / Docker Engine)

Running both the application and database locally inside Docker:

### Step 1: Copy Environment File
```bash
cp .env.example .env
```
*(On Windows PowerShell: `Copy-Item .env.example .env`)*

### Step 2: Customize Settings (Optional)
The defaults in `.env` are configured for local Docker:
* `APP_PORT=8080` (accessible at `http://localhost:8080`)
* `ROOT_URL=http://localhost:8080/`
* `DB_PASSWORD=@Monic123Secure!`

### Step 3: Build & Start Containers
```bash
docker compose up -d --build
```

### Step 4: Verify
* Open browser: **http://localhost:8080**
* Health check: **http://localhost:8080/health**
* View logs:
  ```bash
  docker compose logs -f monic-app
  ```

---

## 🚀 2. Coolify Deployment

Coolify is an open-source, self-hosted PaaS that deploys directly via Git repository or Docker Compose.

### Method A: Deploy via Docker Compose (Recommended)
1. In Coolify, create a new **Service** or **Application** from Git repository.
2. Select **Docker Compose** as the build pack.
3. Set the compose file path: `docker-compose.yml`.
4. Under **Environment Variables**, provide:
   ```env
   DB_PASSWORD=YourStrongDatabasePassword123!
   APP_PORT=80
   ROOT_URL=https://monic.yourdomain.com/
   SITE_NAME=MONIC
   ASPNETCORE_ENVIRONMENT=Production
   ```
5. In Coolify UI, assign your domain: `https://monic.yourdomain.com` pointing to `monic-app:80`.
6. Click **Deploy**. Coolify automatically builds, health-checks on `/health`, and provisions SSL with Traefik/Caddy.

---

## ☁️ 3. Render Deployment

Render can deploy the application as a **Docker Web Service**:

1. In Render Dashboard, click **New +** ➔ **Web Service**.
2. Connect your Git repository.
3. Select **Docker** as the runtime.
4. Render will detect the root [`Dockerfile`](Dockerfile).
5. Under **Environment Variables**, set:
   * `ConnectionStrings__DefaultConnection`: Your connection string to SQL Server (e.g., Azure SQL, AWS RDS, or Render Private Service).
     ```text
     Server=your-db-host,1433;Database=MONIC;User Id=sa;Password=YourPassword;TrustServerCertificate=true;Encrypt=false;
     ```
   * `Settings__RootUrl`: `https://your-service-name.onrender.com/`
   * `Settings__SiteName`: `MONIC`
   * `ASPNETCORE_ENVIRONMENT`: `Production`
6. Under **Health Check Path**, enter: `/health`.
7. Click **Create Web Service**. Render provides the `PORT` environment variable automatically, which MONIC binds to automatically.

---

## 🐧 4. Standalone Linux VPS (Ubuntu / Debian / CentOS)

For deploying on a cloud VPS (DigitalOcean, Linode, Hetzner, AWS EC2):

### 1. Install Docker & Compose (if not already installed)
```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
```

### 2. Clone & Configure
```bash
git clone <your-repo-url> /opt/monic
cd /opt/monic
cp .env.example .env
nano .env
```
Set:
```env
APP_PORT=80
ROOT_URL=http://your-server-ip/   # or https://yourdomain.com/
DB_PASSWORD=YourStrongPasswordHere!
```

### 3. Launch
```bash
docker compose up -d --build
```

### 4. Optional: SSL with Nginx / Certbot / Caddy
If placing behind Nginx reverse proxy:
```nginx
server {
    server_name monic.yourdomain.com;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🟧 5. Amazon Web Services (AWS)

### Option A: AWS App Runner
1. Push your built image to **Amazon ECR** (Elastic Container Registry):
   ```bash
   aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <aws_account_id>.dkr.ecr.us-east-1.amazonaws.com
   docker build -t monic-app .
   docker tag monic-app:latest <aws_account_id>.dkr.ecr.us-east-1.amazonaws.com/monic:latest
   docker push <aws_account_id>.dkr.ecr.us-east-1.amazonaws.com/monic:latest
   ```
2. Create an **App Runner Service** pointing to the ECR image.
3. Set Port: `80`.
4. Configure Environment Variables pointing to AWS RDS for SQL Server.

### Option B: AWS ECS Fargate
1. Create an ECS Task Definition with container image from ECR.
2. Mount EFS or Docker volume for `/app/data` to persist tokens.
3. Configure Application Load Balancer (ALB) target group pointing to container port 80 with health check path `/health`.

---

## 🛠️ Performance & Build Optimizations

The fine-tuned [`Dockerfile`](Dockerfile) includes:
1. **Multi-Stage Caching**:
   - Node dependencies (`package.json`, `patches/`) and .NET packages (`Web.csproj`) are restored in separate cached layers before copying source code.
   - Code changes do **not** trigger re-downloading npm packages or NuGet packages.
2. **Headless Angular Build**:
   - Compiles inside `node:20-alpine`, keeping the final container under 300MB.
3. **Cross-Architecture**:
   - Compatible with x86_64 (`amd64`) and ARM64 (`arm64` - Apple Silicon / AWS Graviton).
4. **Resilient Health Checking**:
   - Built-in `/health` endpoint checked with `curl` for container lifecycle management.

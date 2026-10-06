# MONIC Docker Deployment Guide

## Overview
This guide covers deploying MONIC to Coolify using Docker containers.

## Architecture

```
┌─────────────────────────────────────┐
│      MONIC Application Stack        │
├─────────────────────────────────────┤
│  monic-app (ASP.NET Core 10)        │
│  - Port: 80/443                     │
│  - Health checks enabled            │
│  - Logs mounted                     │
├─────────────────────────────────────┤
│  monic-db (SQL Server 2022)         │
│  - Port: 1433                       │
│  - Data persistence enabled         │
│  - Health checks enabled            │
└─────────────────────────────────────┘
```

## Prerequisites

- Docker & Docker Compose installed
- Coolify instance running
- Minimum 2GB RAM available
- 5GB disk space for database and logs
- .NET 10 runtime support

## Quick Start

### 1. Clone and Configure
```bash
git clone https://github.com/Matandaelis/MONIC.git
cd MONIC
cp .env.example .env
```

### 2. Edit .env with Your Settings
```bash
# Critical settings
DB_PASSWORD=your_secure_password_here     # Min 8 chars, 1 uppercase, 1 number, 1 special
DOMAIN=monic.yourdomain.com              # Your public domain
SITE_NAME=Your MONIC Instance            # Display name

# Optional email settings
SEND_EMAILS=true
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_SSL=true
```

### 3. Build and Start
```bash
# Build the Docker image
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f monic-app
```

### 4. Verify Deployment
```bash
# Check container status
docker-compose ps

# Check health
curl http://localhost/health

# View detailed logs
docker-compose logs monic-app
```

## Environment Variables

### Required
- `DB_PASSWORD`: SQL Server SA password (minimum complexity required)
- `DOMAIN`: Your public domain (e.g., monic.example.com)
- `SITE_NAME`: Application display name

### Optional
- `APP_PORT`: Application port (default: 80)
- `SEND_EMAILS`: Enable email notifications (default: false)
- `EMAIL_SENDER_NAME`: Email sender name
- `SMTP_SERVER`: SMTP server address
- `SMTP_PORT`: SMTP port (default: 25)
- `SMTP_USERNAME`: SMTP username
- `SMTP_PASSWORD`: SMTP password
- `SMTP_SSL`: Use SSL (default: false)
- `ERROR_EMAIL`: Error notification email
- `USE_APP_INSIGHTS`: Application Insights (default: false)

## Coolify Integration

### Deploy via Coolify Dashboard

1. **Create Application**
   - Log in to Coolify
   - Navigate to Applications → Create New

2. **Configure Repository**
   - Source: GitHub
   - Repository: `Matandaelis/MONIC`
   - Branch: `master`

3. **Build Configuration**
   - Dockerfile Path: `./Dockerfile`
   - Docker Compose Path: `./docker-compose.yml`
   - Base Directory: `/`

4. **Environment Variables**
   - Add all variables from `.env` file
   - Mark sensitive values (passwords) as secrets

5. **Port Configuration**
   - Application Port: `80`
   - Expose: Yes

6. **Persistent Storage**
   - `/var/opt/mssql` → monic-db-data (database)
   - `/app/logs` → monic-app-logs (application logs)

7. **Health Check**
   - Path: `/health`
   - Interval: `30s`
   - Timeout: `10s`
   - Retries: `3`

## Database Management

### Connect to Database
```bash
# Using sqlcmd
sqlcmd -S localhost,1433 -U sa -P your_password

# Or use Azure Data Studio
# Server: localhost,1433
# Username: sa
# Password: (from .env)
```

### Backup Database
```bash
docker-compose exec monic-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" \
  -Q "BACKUP DATABASE [MONIC] TO DISK = '/var/opt/mssql/backup/MONIC.bak'"
```

### Restore Database
```bash
docker-compose exec monic-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${DB_PASSWORD}" \
  -Q "RESTORE DATABASE [MONIC] FROM DISK = '/var/opt/mssql/backup/MONIC.bak'"
```

## Troubleshooting

### Container Won't Start
```bash
# Check logs
docker-compose logs monic-app

# Common issues:
# 1. Port already in use
changed APP_PORT=8080  # in .env

# 2. Invalid DB password
# Must contain: 8+ chars, 1 uppercase, 1 number, 1 special character

# 3. Insufficient memory
# Increase Docker memory allocation

# Restart after fixing
docker-compose down
docker-compose up -d
```

### Database Connection Issues
```bash
# Verify DB container is healthy
docker-compose ps monic-db

# Check database logs
docker-compose logs monic-db

# Test connection from app
docker-compose exec monic-app bash
telnet monic-db 1433
```

### Health Check Failing
```bash
# Test health endpoint
curl -v http://localhost/health

# If 404 or timeout, app may not be ready
# Check logs for startup errors
docker-compose logs --tail=100 monic-app

# Restart app
docker-compose restart monic-app

# Wait 40+ seconds for startup period
```

### High Memory Usage
```bash
# Check current usage
docker stats monic-db monic-app

# Reduce database memory in docker-compose.yml
environment:
  MSSQL_MEMORY_LIMIT_MB: 1024

# Restart
docker-compose restart monic-db
```

## Production Recommendations

### Resource Limits
```yaml
resources:
  limits:
    cpus: '2'
    memory: 2G
  reservations:
    cpus: '1'
    memory: 1G
```

### Database Optimization
- Increase `MSSQL_MEMORY_LIMIT_MB` to available RAM
- Use SSD storage for database volume
- Schedule regular backups

### Security
- Change default passwords immediately
- Use strong, unique DB password
- Use SSL for SMTP (if enabled)
- Keep Docker images updated
- Use Coolify's reverse proxy for HTTPS

### Monitoring
```bash
# Real-time stats
docker stats

# View logs with timestamps
docker-compose logs -t --tail=50

# Follow specific service
docker-compose logs -f monic-app
```

## Updating MONIC

### Pull Latest Changes
```bash
# Stop containers
docker-compose down

# Update repository
git pull origin master

# Rebuild image
docker-compose build --no-cache

# Restart
docker-compose up -d

# Check logs
docker-compose logs -f monic-app
```

## Cleanup

### Remove Containers
```bash
# Stop and remove
docker-compose down

# Also remove volumes (WARNING: deletes data)
docker-compose down -v
```

### Clean Unused Resources
```bash
# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune
```

## Support

For issues:
1. Check logs: `docker-compose logs`
2. Verify environment: `docker-compose config`
3. Test connectivity: `docker-compose exec monic-app curl http://monic-db:1433`
4. Review MONIC repo: https://github.com/Matandaelis/MONIC/issues

## Important Notes

⚠️ **Critical Requirements:**
- .NET 10 runtime support required
- SQL Server 2022 compatible environment
- Minimum 2GB RAM
- Valid DB password (complexity requirements)

⚠️ **Known Limitations:**
- MONIC is under active development
- May receive breaking changes
- Azure features require additional configuration
- Email functionality is optional

## Next Steps

- [ ] Configure custom domain
- [ ] Set up SSL certificates (Coolify reverse proxy)
- [ ] Configure email notifications
- [ ] Set up automated backups
- [ ] Configure monitoring and alerts
- [ ] Plan disaster recovery

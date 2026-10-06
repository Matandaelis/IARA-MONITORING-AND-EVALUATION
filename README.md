# MONIC
### Open-source monitoring & evaluation system for impact-driven organisations

<div align="center">
  <img src="https://github.com/capesean/MONIC/assets/642609/3d1f66ad-efb9-461b-b941-a1a21de7d04a" width="900" alt="MONIC dashboard preview" />
  
  **Monitoring • Evaluation • Impact Assessment • Project Management**
  
  <a href="https://www.youtube.com/watch?v=A5eus7Yhv1k">
    <img src="https://img.shields.io/badge/Watch-Demo%20Video-red?style=for-the-badge&logo=youtube" alt="Watch demo video" />
  </a>
  <a href="https://www.monic.tech">
    <img src="https://img.shields.io/badge/Visit-monic.tech-4CAF50?style=for-the-badge" alt="Visit monic.tech" />
  </a>
  <a href="https://github.com/Matandaelis/MONIC/blob/master/LICENSE.txt">
    <img src="https://img.shields.io/badge/License-AGPL%20v3-blue?style=for-the-badge" alt="License: AGPL v3" />
  </a>
</div>

---

## 🎯 What is MONIC?

MONIC is a **comprehensive, open-source Monitoring & Evaluation (M&E) system** designed for organizations focused on impact assessment and evidence-based decision making. Whether you're an NGO, government agency, or development organization, MONIC provides a complete platform for tracking program performance, managing indicators, and evaluating organizational impact.

### Core Capabilities

✅ **Monitoring & Data Collection** - Design questionnaires, collect real-time data from field teams  
✅ **Indicator Management** - Define, track, and analyze key performance indicators  
✅ **Theories of Change** - Link your logic models with frameworks and indicators  
✅ **Dashboards & Analytics** - Visualize data with charts, maps, and custom widgets  
✅ **Project Management** - Track activities, timelines, and deliverables  
✅ **Document Management** - Store and organize project documents and knowledge  
✅ **Advanced Analytics** - Generate reports, AI-powered summaries, and insights  
✅ **Flexible Permissions** - Granular user access control for secure data entry  

---

## 🚀 Quick Start (Docker)

### Prerequisites
- Docker & Docker Compose installed
- .NET 10 runtime support
- 2GB+ RAM
- 5GB+ disk space

### Deploy in 5 Minutes

```bash
# Clone repository
git clone https://github.com/Matandaelis/MONIC.git
cd MONIC

# Configure environment
cp .env.example .env
nano .env  # Edit with your values

# Build and start
docker compose up --build -d

# Verify
docker compose ps
curl http://localhost/health

# Access the application
# Browser: http://localhost
# Follow the setup wizard to create your first admin account
```

### Verify Deployment

```bash
# Check container status
docker compose ps

# View logs
docker compose logs -f monic-app

# Health check
curl -v http://localhost/health
```

---

## 🏗️ Architecture & Technology Stack

```
╔══════════════════════════════════════════════════════════════╗
║              MONIC Application Architecture                 ║
╠══════════════════════════════════════════════════════════════╣
║  🖥️ FRONTEND (Angular 19)                                   ║
║     ├─ Interactive dashboard with real-time updates         ║
║     ├─ Charts & maps (D3, ECharts, Google Maps)             ║
║     ├─ Responsive UI (desktop, tablet, mobile)              ║
║     └─ Form builder for questionnaires                      ║
╠══════════════════════════════════════════════════════════════╣
║  ⚙️ BACKEND (ASP.NET Core 10)                              ║
║     ├─ REST API endpoints                                   ║
║     ├─ OpenIddict authentication & OAuth 2.0                ║
║     ├─ Entity Framework Core ORM                            ║
║     ├─ Business logic for M&E workflows                     ║
║     └─ PDF/Excel/CSV export capabilities                    ║
╠══════════════════════════════════════════════════════════════╣
║  💾 DATABASE (SQL Server 2022)                             ║
║     ├─ Normalized relational schema                         ║
║     ├─ Full ACID compliance                                 ║
║     ├─ Schema migrations with EF Core                       ║
║     └─ Data persistence & backup support                    ║
╚══════════════════════════════════════════════════════════════╝
```

### Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Frontend** | Angular | 19 |
| **Backend** | ASP.NET Core | 10.0 |
| **Database** | SQL Server | 2022 |
| **Authentication** | OpenIddict | 7.2.0 |
| **ORM** | Entity Framework Core | 10.0 |
| **Charting** | ECharts, D3 | Latest |
| **Maps** | Google Maps API | v3 |

---

## 📊 Key Features

### 1. **Monitoring & Data Collection**
- Design custom questionnaires with branching logic
- Mobile-friendly data collection forms
- Real-time data validation and constraints
- Support for multiple data types (text, numeric, date, geolocation, media)
- Offline data collection with sync capability

### 2. **Indicator Management**
- Define indicators with units and targets
- Create calculated indicators (formulas for custom metrics)
- Track indicator values over time
- Compare actual vs. planned performance
- Disaggregate data by dimensions (geography, demographics, time)

### 3. **Theories of Change & Logical Frameworks**
- Map program logic from inputs → activities → outputs → outcomes → impact
- Link indicators to TOC/log-frame elements
- Track assumption validation
- Visualize causal pathways

### 4. **Dashboards & Visualization**
- Custom dashboard widgets (charts, maps, tables, gauges)
- Real-time data updates
- Export charts to PowerPoint or PDF
- Geographic data visualization
- Completion rate tracking

### 5. **Project Management**
- Gantt charts for activity timelines
- Milestone tracking and reporting
- Resource allocation and budgeting
- Activity-level indicators
- Progress monitoring

### 6. **Document Management**
- Store and organize project documents
- Version control for reports and policies
- Knowledge repository
- Team collaboration spaces

### 7. **Analytics & Reporting**
- Pre-built and custom reports
- AI-powered summaries (ChatGPT integration optional)
- Data export (CSV, Excel, PDF)
- Statistical analysis and trend detection
- Performance summaries and scorecards

### 8. **User Management & Security**
- Role-based access control (RBAC)
- Granular permissions per indicator/entity
- Audit logging of data changes
- Two-factor authentication support
- Secure password policies

---

## ☁️ Deployment Options

### 1. **Local Development**
```bash
docker compose up -d
# Direct access at http://localhost
```

### 2. **Docker on Server**
```bash
# Full Docker stack with persistent volumes
docker compose -f docker-compose.yml up -d
```

### 3. **Coolify (Recommended for Production)**

<div align="center">
  <img src="https://github.com/capesean/MONIC/assets/642609/f389ee36-f5fd-4587-8d26-f9f9b3852c8b" width="900" alt="MONIC setup screen" />
</div>

Coolify is a self-hosted alternative to Heroku/Vercel. Deploy MONIC in minutes:

#### Step 1: Create Application in Coolify
```
Applications → Create New → Docker Compose
```

#### Step 2: Configure Repository
```
Source: GitHub
Repository: Matandaelis/MONIC
Branch: master
```

#### Step 3: Build Settings
```
Dockerfile: ./Dockerfile
Docker Compose: ./docker-compose.yml
Base Directory: /
```

#### Step 4: Environment Variables
Copy from [COOLIFY_PRODUCTION_CONFIG.md](COOLIFY_PRODUCTION_CONFIG.md):

```env
# Required: Database
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=true;Encrypt=false;

# Required: Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Required: Site
Settings__RootUrl=https://monic.example.com/
Settings__SiteName=MONIC

# Optional: Email
Settings__EmailSettings__SendEmails=false
Settings__EmailSettings__SenderName=MONIC Alerts
Settings__EmailSettings__SMTP=smtp.gmail.com
Settings__EmailSettings__SMTPPort=587
```

#### Step 5: Deploy
```
Click Deploy
Wait 5-10 minutes for build and startup
Monitor in Logs tab
```

#### Step 6: Verify
```bash
# Check health
curl https://your-domain/health

# Access application
# Browser: https://your-domain
```

---

## 🔐 Production Configuration

### Database Password Requirements

SQL Server requires strong passwords:
- ✅ Minimum 8 characters
- ✅ 1 uppercase letter (A-Z)
- ✅ 1 lowercase letter (a-z)
- ✅ 1 number (0-9)
- ✅ 1 special character (!@#$%^&*)

**Valid Examples:**
- `MyMonic@2024!`
- `SecurePass123$`
- `P@ssw0rd!MONIC`

### Essential Environment Variables

| Variable | Required | Example | Notes |
|----------|----------|---------|-------|
| `ConnectionStrings__DefaultConnection` | ✅ | `Server=monic-db,...` | Database connection |
| `ASPNETCORE_ENVIRONMENT` | ✅ | `Production` | Enable optimizations |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | ✅ | `true` | Reverse proxy support |
| `Settings__RootUrl` | ✅ | `https://monic.example.com/` | Public domain |
| `Settings__SiteName` | ✅ | `MONIC` | Display name |
| `SEND_EMAILS` | ❌ | `false` | Email notifications |

**See [COOLIFY_PRODUCTION_CONFIG.md](COOLIFY_PRODUCTION_CONFIG.md) for complete production configuration.**

---

## 🔍 Troubleshooting

### Health Check Failed
```bash
# Wait for startup (40+ seconds)
docker compose logs -f monic-app

# Verify endpoint
curl -v http://localhost/health
```

### Database Connection Failed
```bash
# Check database status
docker compose ps monic-db

# Verify password meets requirements
# Must have: 8+ chars, uppercase, lowercase, number, special char
```

### Application Won't Start
```bash
# View full logs
docker compose logs --tail=100 monic-app

# Common issues:
# 1. Invalid connection string
# 2. .NET 10 runtime not available
# 3. Database not ready
```

### Email Not Sending
```bash
# Check email config
SEND_EMAILS=true
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-specific-password
SMTP_SSL=true
```

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| [DOCKER_DEPLOYMENT.md](DOCKER_DEPLOYMENT.md) | Complete Docker setup and management |
| [COOLIFY_PRODUCTION_CONFIG.md](COOLIFY_PRODUCTION_CONFIG.md) | Production-ready Coolify configuration |
| [YouTube Demo](https://www.youtube.com/watch?v=A5eus7Yhv1k) | 20-minute feature walkthrough |
| [monic.tech](https://www.monic.tech) | Official website and documentation |

---

## 💾 Backup & Maintenance

### Backup Database
```bash
docker compose exec monic-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourPassword123!" \
  -Q "BACKUP DATABASE [MONIC] TO DISK = '/var/opt/mssql/backup/MONIC.bak'"
```

### Restore Database
```bash
docker compose exec monic-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourPassword123!" \
  -Q "RESTORE DATABASE [MONIC] FROM DISK = '/var/opt/mssql/backup/MONIC.bak'"
```

### Update MONIC
```bash
git pull origin master
docker compose build --no-cache
docker compose down
docker compose up -d
```

---

## 🔐 Security Best Practices

### Before Production Deployment

- [ ] Use strong, unique database password
- [ ] Enable HTTPS/SSL (Coolify reverse proxy)
- [ ] Configure firewall rules
- [ ] Set up regular backups
- [ ] Enable email notifications for alerts
- [ ] Use secrets management for sensitive values
- [ ] Keep Docker images updated
- [ ] Review and restrict user permissions
- [ ] Enable audit logging
- [ ] Test disaster recovery procedures

### Ongoing Security

- [ ] Regularly update dependencies
- [ ] Monitor access logs
- [ ] Rotate sensitive credentials
- [ ] Test backup restoration
- [ ] Review user access quarterly
- [ ] Keep system patches current

---

## 🤝 Contributing

MONIC welcomes contributions from the community. To contribute:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

---

## 🆘 Support & Issues

### Getting Help

1. **Check Docs**: Review [DOCKER_DEPLOYMENT.md](DOCKER_DEPLOYMENT.md) and [COOLIFY_PRODUCTION_CONFIG.md](COOLIFY_PRODUCTION_CONFIG.md)
2. **Watch Demo**: https://www.youtube.com/watch?v=A5eus7Yhv1k
3. **Report Issues**: https://github.com/Matandaelis/MONIC/issues
4. **Visit Website**: https://www.monic.tech

### Reporting Bugs

When reporting issues, please include:
- **Logs**: `docker compose logs monic-app`
- **Environment**: OS, Docker version, .NET version
- **Steps to Reproduce**: Clear, numbered steps
- **Expected vs Actual**: What should happen vs what happened
- **Screenshots**: If relevant to UI issues

---

## 📄 License

MONIC is licensed under the **GNU Affero General Public License v3.0** (AGPL-3.0).

```
This means:
✅ Free to use and modify
✅ Source code must be made available
✅ Network use triggers copyleft obligation
⚠️  Modifications must be shared under same license
```

See [LICENSE.txt](LICENSE.txt) for full terms.

---

## 🎯 Roadmap

- [ ] Enhanced analytics and reporting
- [ ] Mobile app version (iOS/Android)
- [ ] Multi-language support (i18n)
- [ ] Advanced data visualization
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Performance optimizations for large datasets
- [ ] Real-time collaboration features
- [ ] Integration with external data sources

---

## 👥 Acknowledgments

MONIC is developed by a community of impact-driven developers and organizations committed to evidence-based development and evaluation.

Special thanks to:
- **Global Evaluation Initiative (GEI)** for platform exposure and feedback
- All contributors and the open-source community

---

## 📞 Get in Touch

- **Website**: https://www.monic.tech
- **GitHub**: https://github.com/Matandaelis/MONIC
- **YouTube**: https://www.youtube.com/watch?v=A5eus7Yhv1k
- **Issues**: https://github.com/Matandaelis/MONIC/issues

---

<div align="center">
  <b>Made with ❤️ for impact-driven organizations</b>
  
  ⭐ If MONIC helps your organization, please consider giving us a star on GitHub!
  
  **Last Updated:** October 2026 | **Status:** Active Development | **Stability:** Beta
</div>

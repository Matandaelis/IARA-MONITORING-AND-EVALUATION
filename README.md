# 🌟 IARA: Impact & Accountability Reporting Architecture

<div align="center">

  ### 🎯 Enterprise Monitoring, Evaluation, Governance, and Donor Reporting Platform
  **Track Results • Manage LogFrames • Govern Data • Publish Accountability Reports**

  [![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg?style=for-the-badge)](LICENSE.txt)
  [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](WEB/WEB.csproj)
  [![Angular 19](https://img.shields.io/badge/Angular-19.0-DD0031?style=for-the-badge&logo=angular&logoColor=white)](WEB/ClientApp)
  [![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](docker-compose.yml)
  [![Docker Ready](https://img.shields.io/badge/Docker-Multi--Stage%20Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](docker-compose.yml)
  [![Tests](https://img.shields.io/badge/Tests-185%20Passing-success?style=for-the-badge)](WEB.Tests)

</div>

---

## Overview

IARA is a modern monitoring and evaluation platform designed for organizations that need to turn strategy into measurable results.

It brings together:

- logical framework management
- indicator tracking and calculation
- surveys and field data collection
- governance and approvals
- budget alignment
- risk management
- donor reporting
- team collaboration

In short, IARA helps programs move from scattered spreadsheets and fragmented tools to a single operational system for accountability.

---

## Why this platform exists

Most organizations do not struggle because they lack data.
They struggle because their data is fragmented, difficult to trust, and disconnected from decision-making.

Common problems IARA solves:

- indicators are tracked in multiple spreadsheets
- field data is collected in separate tools and never reconciles with program reporting
- no clear link between activities, budgets, and expected outcomes
- data quality is weak and approvals are informal
- donor reporting takes too long and is hard to defend
- teams cannot easily see which objectives are on track and which are at risk

IARA creates a unified operating model for performance management.

---

## Business value

IARA gives organizations a way to:

- connect activity to results
- validate whether implementation is delivering intended outcomes
- monitor performance by time, location, partner, and target group
- strengthen data quality through review and approval cycles
- align budgets to program objectives
- produce consistent, auditable donor-ready reports
- improve accountability across internal teams and external stakeholders

This is not only a reporting tool. It is a governance and performance management system.

---

## Core business logic

IARA is built around a simple operating model:

`Input -> Activity -> Output -> Outcome -> Impact`

Every program is represented through a structured result chain.

The platform supports:

- theory of change design
- logframe management
- indicator definition and calculation
- baseline and target tracking
- actual value collection and comparison
- variance analysis
- disaggregation by geography, gender, age, facility, and other dimensions
- approval workflows
- risk and corrective action tracking

Each result is connected to the people responsible, the timeline, the data source, and the reporting requirements.

---

## Platform capabilities

### 1. Results-Based Management

IARA helps organizations plan and manage results systematically.

Features include:

- logical framework hierarchy
- outcome and output mapping
- target scheduling by month, quarter, or year
- cumulative tracking against expected progress
- multi-level indicator rollups

### 2. Indicator and formula engine

Indicators can be manually entered or calculated automatically.

Examples include:

- total beneficiaries reached
- percentage of targets achieved
- weighted operational performance score
- composite program indicators derived from multiple data sources

The calculation engine supports formula logic that is transparent and auditable.

### 3. Survey and field data collection

The platform supports structured field data capture through:

- digital questionnaires
- survey logic and skip rules
- attachment capture
- public community feedback forms
- KoboToolbox / ODK compatibility

This allows field evidence to feed directly into program performance tracking.

### 4. Data quality and governance

The platform includes approval-based governance to improve trust in the reported numbers.

Workflow stages include:

- submit
- verify
- approve
- reject
- corrective action

This helps reduce error, weak quality control, and questionable reporting.

### 5. Budget and financial alignment

IARA links financial information to operational results.

This allows teams to track:

- planned vs actual spend
- budget allocation by activity or objective
- financial performance relative to delivery
- donor-financed commitments and execution gaps

### 6. Risk and corrective action tracking

IARA supports structured risk management by enabling:

- risk register tracking
- issue capture
- variance alerts
- corrective action plans
- accountability for follow-up

### 7. Donor and stakeholder reporting

The platform supports export-ready reporting for internal management and external funders.

Examples include:

- performance summary reports
- indicator dashboards
- donor-specific output tables
- IATI-compatible exports
- Excel and PDF reporting outputs

---

## End-to-end operating workflow

A typical project lifecycle in IARA looks like this:

1. Define the program strategy and results framework
2. Set objectives, outcomes, outputs, and indicators
3. Assign targets, baselines, and reporting periods
4. Collect data from team operations or field systems
5. Validate data quality and compare actuals to targets
6. Review and approve data through formal governance steps
7. Trigger corrective actions where required
8. Publish consolidated performance and donor reporting

This creates a continuous loop between planning, execution, control, and accountability.

---

## Technology architecture

IARA is built with a modern, service-oriented architecture:

- Backend: ASP.NET Core 10
- Frontend: Angular 19
- Database: Microsoft SQL Server 2022/2025
- Real-time collaboration: SignalR
- Identity and security: OpenIddict OAuth 2.0 / OIDC
- Containerization: Docker and Docker Compose

This combination provides a scalable foundation for both internal program management and public-facing operational reporting.

---

## Repository structure

```text
.
├── .agents/
├── docs/
├── specs/
├── WEB/
│   ├── ClientApp/
│   ├── Code/
│   ├── Controllers/
│   ├── Migrations/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
├── WEB.Tests/
├── Dockerfile
├── docker-compose.yml
├── entrypoint.sh
├── .env.example
├── coolify.env.example
├── README.md
├── LICENSE.txt
└── MONIC.sln
```

---

## Quick start with Docker

### Prerequisites

- Docker
- Docker Compose

### Step 1: Copy the environment file

```bash
cp .env.example .env
```

### Step 2: Start the stack

```bash
docker compose up -d --build
```

### Step 3: Open the app

```text
http://localhost:8080
```

This starts the application and the SQL Server database required for the platform.

---

## Coolify deployment

This repository is structured to work well with Coolify using Docker Compose.

Recommended deployment pattern:

1. Create a new Docker Compose service in Coolify
2. Connect the GitHub repo
3. Set the branch to `master`
4. Use `docker-compose.yml`
5. Add environment variables from `coolify.env.example`
6. Set public HTTPS domain and enable reverse proxy/SSL
7. Keep the database internal and protected

Minimal production variables:

```env
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;Encrypt=false;
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Settings__RootUrl=https://your-domain.example.com/
Settings__SiteName=MONIC
Settings__AccessTokenExpiryMinutes=10
Settings__RefreshTokenExpiryMinutes=10880
Settings__EmailSettings__SendEmails=false
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
```

The service exposes a health endpoint at `/health` for deployment checks.

---

## Why teams choose IARA

Organizations select IARA when they need:

- a single system for results management and reporting
- stronger accountability in program operations
- cleaner data governance and review workflows
- better visibility into progress, risks, and gaps
- donor-ready outputs without duplicating work
- a scalable digital foundation for program monitoring

It is designed for organizations that want to move from fragmented reporting to evidence-driven management.

---

## License

This project is licensed under the GNU Affero General Public License v3.0.

See [LICENSE.txt](LICENSE.txt) for the full license text.

---

<div align="center">
  <b>IARA: Impact & Accountability Reporting Architecture</b><br/>
  Engineered for precision, transparency, and accountability in global development.
</div>




























































































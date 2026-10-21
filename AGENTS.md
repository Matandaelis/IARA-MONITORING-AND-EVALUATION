# 🤖 MONIC Multi-Agent System Architecture (AGENTS.md)

This document defines the **autonomous AI agent roles, operational rules, toolchains, and collaborative workflows** designed to operate on, maintain, and interact with the **MONIC Monitoring & Evaluation (M&E) platform**.

---

## 🏛️ System Overview

MONIC is a domain-specialized, enterprise-grade Monitoring & Evaluation platform combining an **ASP.NET Core 10 Web API**, a **SQL Server database (64 tables)**, and an **Angular 19 SPA frontend**.

Agents interacting with MONIC operate under defined personas with clear boundaries, input/output schemas, and verification requirements.

```mermaid
graph TD
    User["👤 Changemaker / Program Manager"] --> LeadAgent["🧠 MONIC Lead Coordinator Agent"]
    
    subgraph DomainAgents ["Specialized Domain Agents"]
        LeadAgent --> MEAgent["📊 M&E Framework Designer Agent"]
        LeadAgent --> SurveyAgent["📝 Survey & Questionnaire Agent"]
        LeadAgent --> AuditAgent["🔍 Review & Governance Agent"]
    end

    subgraph EngineeringAgents ["Engineering & DevOps Agents"]
        LeadAgent --> DBAgent["💾 Database & Migration Agent"]
        LeadAgent --> APIAgent["⚙️ ASP.NET Core & OpenIddict Agent"]
        LeadAgent --> DevopsAgent["🐳 Cloud & Containerization Agent"]
    end

    MEAgent --> CoreDB[(MONIC SQL Server)]
    SurveyAgent --> CoreDB
    AuditAgent --> CoreDB
    DBAgent --> CoreDB
    APIAgent --> CoreDB
    DevopsAgent --> DockerHost["Cloud Host (Coolify / Render / AWS)"]

    style LeadAgent fill:#e1f5fe,stroke:#0288d1,stroke-width:2px;
    style DomainAgents fill:#f3e5f5,stroke:#8e24aa,stroke-width:1px;
    style EngineeringAgents fill:#e8f5e9,stroke:#2e7d32,stroke-width:1px;
```

---

## 👥 Agent Directory

### 1. 🧠 MONIC Lead Coordinator (`monic-lead-coordinator`)
* **Role**: Primary orchestrator and prompt dispatcher.
* **Responsibilities**:
  * Deconstructs high-level user goals into structured sub-tasks.
  * Routes requests to the appropriate domain or engineering agent.
  * Validates outputs against MONIC architectural and M&E standards before presenting results to the user.
* **Tool Permissions**: File reading, sub-agent delegation, task management.

---

### 2. 📊 M&E Framework Designer (`me-framework-designer`)
* **Role**: Expert in Results-Based Management (RBM), Theories of Change (ToC), and Indicator frameworks.
* **Responsibilities**:
  * Designs causal pathways: `Input` ➔ `Activity` ➔ `Output` ➔ `Outcome` ➔ `Impact`.
  * Configures **Logical Frameworks (LogFrames)** aligned with donor standards (USAID, EU, UN, World Bank).
  * Formulates **Calculated Indicators** with valid token stacks (Reverse Polish Notation arithmetic) and aggregation rules (`Sum`, `MostRecent`).
  * Establishes temporal roll-ups (`Month` ➔ `Quarter` ➔ `Year`) and organizational roll-ups (`EntityLinks`).
* **Key Context Files**:
  * [`WEB/Models/TheoryOfChange.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/TheoryOfChange.cs)
  * [`WEB/Models/LogFrame.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/LogFrame.cs)
  * [`WEB/Models/Indicator.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/Indicator.cs)
  * [`WEB/Code/Calculation.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Code/Calculation.cs)

---

### 3. 📝 Survey & Questionnaire Agent (`survey-builder-agent`)
* **Role**: Field data collection and survey architect.
* **Responsibilities**:
  * Generates digital questionnaires with conditional branching and skip logic (`SkipLogicOption`).
  * Configures reusable option sets (`QuestionOptionGroup`) for Likert scales, ratings, and dropdowns.
  * Prepares anonymous public survey links (`PublicCode`) for community data collection.
  * Maps questionnaire questions to indicator data targets.
* **Key Context Files**:
  * [`WEB/Models/Questionnaire.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/Questionnaire.cs)
  * [`WEB/Models/Question.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/Question.cs)
  * [`WEB/Models/SkipLogicOption.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/SkipLogicOption.cs)
  * [`WEB/Controllers/SurveysController.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Controllers/SurveysController.cs)

---

### 4. 🔍 Review & Governance Agent (`data-governance-agent`)
* **Role**: Data quality auditor and approval workflow pipeline controller.
* **Responsibilities**:
  * Monitors the 3-stage data approval lifecycle: **`Submit` ➔ `Verify` ➔ `Approve`** (or `Reject`).
  * Enforces role-based permissions (`Administrator`, `Manager`, `Oversight`, `DataEntry`).
  * Audits data integrity across `DataReview` entries and tracks revision histories.
  * Detects anomalies, outliers, and missing quarterly reporting entries.
* **Key Context Files**:
  * [`WEB/Models/DataReview.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/DataReview.cs)
  * [`WEB/Models/Roles.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/Roles.cs)
  * [`WEB/Controllers/DataReviewsController.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Controllers/DataReviewsController.cs)

---

### 5. 💾 Database & EF Core Migration Agent (`db-migration-agent`)
* **Role**: SQL Server schema steward and Entity Framework Core specialist.
* **Responsibilities**:
  * Generates and reviews EF Core migrations targeting SQL Server 2022/2025.
  * Manages schema constraints, filtered indexes (`CreateNullableUniqueIndex`), and stored procedures (`StoredProcedures.sql`).
  * Optimizes queries, indexes, and execution plans for high-volume survey datasets.
  * Maintains backup and point-in-time recovery scripts.
* **Operating Guardrails**:
  * **Zero Data Loss**: Always verify before dropping columns or tables.
  * Always run design-time factory tests via `dotnet ef` before committing migrations.
* **Key Context Files**:
  * [`WEB/Models/ApplicationDBContext.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Models/ApplicationDBContext.cs)
  * [`WEB/Migrations/`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Migrations/)
  * [`WEB/Code/SQL/StoredProcedures.sql`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Code/SQL/StoredProcedures.sql)

---

### 6. ⚙️ ASP.NET Core & OpenIddict Agent (`backend-security-agent`)
* **Role**: Backend API architect and OAuth 2.0 / OpenIddict security maintainer.
* **Responsibilities**:
  * Implements RESTful endpoints extending `BaseApiController`.
  * Configures OAuth 2.0 token issuance, refresh tokens, PKCE enforcement, and claims.
  * Manages Data Protection key persistence and local certificate generation (`CertificateHelper.cs`).
  * Enforces API exception filtering (`ApiExceptionAttribute`) and data transformation DTOs.
* **Key Context Files**:
  * [`WEB/Program.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Program.cs)
  * [`WEB/Code/OpenIddict.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Code/OpenIddict.cs)
  * [`WEB/Controllers/BaseApiController.cs`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/WEB/Controllers/BaseApiController.cs)

---

### 7. 🐳 Cloud & DevOps Agent (`devops-deployment-agent`)
* **Role**: Multi-platform containerization and cloud orchestration engineer.
* **Responsibilities**:
  * Maintains multi-stage `Dockerfile` and `docker-compose.yml`.
  * Manages zero-downtime rolling deploys on **Coolify**, **Render**, **Linux VPS**, and **AWS**.
  * Monitors health checks (`/health`), reverse proxy headers (`X-Forwarded-*`), and persistent volume storage (`/app/data`).
  * Ensures SSL certificate propagation and environment configuration safety.
* **Key Context Files**:
  * [`Dockerfile`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/Dockerfile)
  * [`docker-compose.yml`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/docker-compose.yml)
  * [`.env.example`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.env.example)
  * [`DOCKER_DEPLOYMENT.md`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/DOCKER_DEPLOYMENT.md)

---

## 📋 Standard Operating Procedures (SOPs) for Agents

### SOP 1: Modifying Database Schema
1. Update domain entity in `WEB/Models/<Entity>.cs`.
2. Update corresponding DTO in `WEB/Models/DTOs/<Entity>DTO.cs`.
3. Add migration via `dotnet ef migrations add <Name>` inside `WEB/`.
4. Inspect generated migration `Up()` and `Down()` methods.
5. If table has nullable unique fields, call `CreateNullableUniqueIndex` in `ApplicationDBContext.cs`.
6. Run `dotnet ef database update` against local test database.

### SOP 2: Adding a New Calculated Indicator Formula
1. Create source indicators with `IndicatorType.Collected`.
2. Create calculated indicator with `IndicatorType.Calculated`.
3. Formulate token sequence in Reverse Polish Notation:
   * Tokens must specify `TokenType` (`Indicator`, `Number`, `Operator`, `Parenthesis`).
   * For operators: `Add`, `Subtract`, `Multiply`, `Divide`.
4. Test formula evaluation via `Calculation.cs` test harness.

### SOP 3: Verifying Cloud Container Build
1. Check that `.dockerignore` preserves `package-lock.json` and `patches/`.
2. Verify multi-stage build: `node:20-alpine` builds client SPA, `dotnet publish` compiles C# backend.
3. Test container startup with health check verification: `curl -f http://localhost:80/health`.
4. Verify `/app/data` volume mount maintains `dataprotection-keys` and OpenIddict signing certificates across restarts.

### SOP 4: Specification-Driven Development (GitHub Spec Kit)
1. Ensure alignment with project principles in [`.specify/memory/constitution.md`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.specify/memory/constitution.md).
2. Generate feature specifications using `/speckit-specify <Feature Name>`.
3. Clarify ambiguous requirements (optional) via `/speckit-clarify`.
4. Formulate technical implementation plans using `/speckit-plan`.
5. Break down into actionable tasks using `/speckit-tasks` (validate with `/speckit-checklist` and `/speckit-analyze`).
6. Execute implementation systematically using `/speckit-implement`.
7. Assess and reconcile remaining work with `/speckit-converge`.

---

## 🛠️ Installed Agent Skills (`.agents/skills/`)

The workspace provides standardized AI agent skills under `.agents/skills/` to accelerate backend, frontend, and test development:

| Skill | Category | Primary Agents | Description |
|---|---|---|---|
| [`csharp-expert`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/csharp-expert/SKILL.md) | Backend | `backend-security-agent`, `db-migration-agent` | C# & .NET specialist routing, semantics, and `dotnet/skills` marketplace integration |
| [`rest-api-design`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/rest-api-design/SKILL.md) | Backend / API | `backend-security-agent`, `monic-lead-coordinator` | RESTful API design standards, OpenAPI 3.0, and spec validation (`scripts/validate-api.ps1`, `validate-api.sh`) |
| [`angular-developer`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/angular-developer/SKILL.md) | Frontend | Lead & UI Agents | Angular 19+ architecture, Signals, signal forms, SSR, accessibility, and CLI workflows |
| [`frontend-design`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/frontend-design/SKILL.md) | Frontend | Lead & UI Agents | Distinctive, intentional UI design, typography scale, palette curation, and copywriting |
| [`typescript-unit-testing`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/typescript-unit-testing/SKILL.md) | Testing / QA | All Agents | AAA unit testing patterns, Angular TestBed / Signals testing, strict typing, and boundary testing |
| `speckit-*` (15 skills) | Specification | `monic-lead-coordinator` | GitHub Spec Kit specification-driven development workflow |
| [`containerize-aspnetcore`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/containerize-aspnetcore/SKILL.md) | DevOps / Deployment | `devops-deployment-agent` | Containerize ASP.NET Core projects — Dockerfile and `.dockerignore` creation following Microsoft containerization best practices |
| [`dotnet-backend-patterns`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/dotnet-backend-patterns/SKILL.md) | Backend | `backend-security-agent`, `db-migration-agent` | C#/.NET backend patterns — APIs, EF Core/Dapper data access, async/await, DI, caching, xUnit testing |
| [`dotnet-webapi`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/dotnet-webapi/SKILL.md) | Backend / API | `backend-security-agent` | ASP.NET Core Web API endpoints — HTTP semantics, OpenAPI/Swagger metadata, error handling middleware, `.http` test files |
| [`ef-core`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/ef-core/SKILL.md) | Data / Engineering | `db-migration-agent` | Entity Framework Core best practices — DbContext design, entity modeling, migrations, change tracking, security |
| [`optimizing-ef-core-queries`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/optimizing-ef-core-queries/SKILL.md) | Data / Engineering | `db-migration-agent` | EF Core data access review and optimization — N+1 detection, round-trips, DbContext lifetime, paging, raw SQL |
| [`run-tests`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/run-tests/SKILL.md) | Testing / QA | All Agents | .NET test execution — correct `dotnet test` syntax, filters, coverage collection, diagnostic logs |
| [`sql-optimization`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/sql-optimization/SKILL.md) | Data / Engineering | `db-migration-agent` | SQL performance optimization — execution plans, indexing strategies, pagination, batch operations, monitoring queries |
| [`test-anti-patterns`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/test-anti-patterns/SKILL.md) | Testing / QA | All Agents | Test suite auditing — severity-ranked diagnostics for tautological assertions, swallowed exceptions, flaky tests, magic values |
| [`github-actions-templates`](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/.agents/skills/github-actions-templates/SKILL.md) | CI/CD / DevOps | `devops-deployment-agent` | GitHub Actions workflow templates — testing matrix builds, Docker image publishing, security scanning, deployment approvals |


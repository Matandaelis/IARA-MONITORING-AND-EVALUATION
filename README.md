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

IARA is an enterprise-grade Monitoring, Evaluation, and Accountability platform designed for organizations that need to manage program performance with discipline, transparency, and operational control.

It is built to support programs in the public sector, nonprofits, international development, donor-funded institutions, and mission-driven organizations that must connect strategy, implementation, evidence, budgets, and public accountability.

The platform combines:

- results-based management
- theory of change and logframe design
- indicator definition and calculation
- field surveys and digital data collection
- budget and expenditure alignment
- governance and review workflows
- risk and corrective action tracking
- donor-ready reporting and exports
- collaboration across management teams and stakeholders

IARA helps move organizations from fragmented recordkeeping to a single operational system for program delivery and accountability.

---

## Why IARA exists

Most organizations do not lack data. They lack a reliable way to connect data to decisions.

The common operational failure pattern is:

- program plans are defined in one system
- activities are tracked in spreadsheets
- field surveys live in another tool
- budgets are stored separately
- donor reporting is assembled manually at the end of a reporting cycle
- no one is sure which numbers are authoritative

IARA addresses this by creating a structured and auditable link between:

- plans and objectives
- activities and outputs
- indicators and targets
- actual implementation and measured results
- budgets and program execution
- reviews and approvals
- reported outcomes and donor commitments

The result is a platform that is both operationally useful and institutionally credible.

---

## Business value

IARA delivers value across multiple layers of an organization:

### Strategic value
- helps leadership monitor whether strategy is being implemented as intended
- keeps results, risks, and performance visible at a glance
- strengthens evidence-based planning and management decisions

### Operational value
- unifies program management, field data, and financial tracking
- reduces manual work in reporting and analysis
- improves efficiency in indicator monitoring and review cycles
- supports faster issue detection and course correction

### Governance value
- creates transparent review and approval workflows
- preserves an audit trail for data changes and decisions
- supports accountability across managers, departments, and partners
- improves compliance with internal and donor control requirements

### Reporting value
- enables consistent reporting to program leadership and donors
- reduces duplication across Excel, PDF, and ad hoc reporting workflows
- supports export-ready outputs aligned with recognized reporting frameworks

---

## Core business logic

IARA is built around a simple but powerful operational model:

`Input -> Activity -> Output -> Outcome -> Impact`

This structure allows every initiative to be traced from the resources used to the results it produces.

### Business concepts in the platform

- Program or project: the operational unit being implemented
- Theory of change: the causal logic explaining how change happens
- Logframe: the structured representation of objectives, indicators, and targets
- Indicator: the measurable signal used to track performance
- Baseline: starting point before implementation or intervention
- Target: expected value to be achieved within a given period
- Actual: real implementation value generated from field or system data
- Disaggregation: breakdown by gender, age group, location, facility, partner, etc.
- Variance: difference between actual and target
- Review: validation of data and decisions by authorized roles
- Corrective action: response to underperformance or quality problems
- Budget alignment: the relationship between spending and planned results

Every major entity in the platform is designed to support decision-making rather than raw record storage alone.

---

## The platform as an operating system for results

IARA is designed to help organizations answer four critical questions:

1. Are we doing what we planned to do?
2. Are we producing the expected outputs?
3. Are those outputs leading to measurable outcomes?
4. Are we reporting this accurately and credibly?

The system supports those questions through structured program logic, data quality controls, and management workflows.

---

## Functional architecture

### 1. Results-based management and logframe logic

IARA supports the design of results frameworks from project planning through implementation and reporting.

Capabilities include:

- objective hierarchy management
- target setting across time periods
- indicator linkage to outcomes and outputs
- cumulative and periodic measurement
- roll-up views by project, partner, district, or organization unit
- multi-level logic for management reporting

This allows program teams to understand not only what is happening, but why it matters.

### 2. Indicator calculation engine

The platform includes a formula engine for derived indicators and calculated results.

Instead of only storing direct measurements, the system can compute indicators from underlying values and rules.

Examples of derived logic include:

- total beneficiaries reached by disaggregation
- percentage target achievement
- weighted performance score
- output ratios and productivity measures
- aggregate values across multiple project units

The logic is implemented using a structured token-based formula model that allows formula transparency and traceability.

### 3. Survey and field data collection

IARA is built for real-world field operations and program evidence.

It supports:

- dynamic questionnaires
- conditional logic and skip rules
- multiple question types and validation rules
- public community feedback links
- mobile-friendly or offline-friendly data collection patterns
- integration points with KoboToolbox and ODK-style workflows
- attachments such as photos, files, or GPS-related evidence

This ensures field evidence does not live in isolated systems but feeds into core program measurement.

### 4. Data quality assurance and validation

Data quality is essential in any monitoring system. IARA includes built-in logic to evaluate whether reported values are credible.

Examples:

- check for missing or incomplete fields
- validate aggregated totals against disaggregated sums
- flag outliers or suspicious changes
- compare actual values with planned targets
- identify variance beyond acceptable thresholds

This helps teams act before poor-quality data distorts reporting or operational decisions.

### 5. Approval and governance workflow

The platform formalizes review and approval processes so data is not simply entered and accepted without controls.

Typical lifecycle:

`Submit -> Verify -> Approve or Reject`

Supported logic includes:

- authorization based on role and responsibility
- protected review actions for designated users
- rejection with evidence or rationale
- traceable history of approvals and changes
- corrective actions when a submission fails review

This is especially important in organizations where reporting quality affects donor confidence, compliance, and management decisions.

### 6. Budget and financial alignment

IARA links implementation activity with budget realities.

This enables:

- budget planning by objective or activity
- multiple-currency management
- tracking of planned vs disbursed amounts
- alignment of expenditure to indicators or outputs
- financial performance analysis against delivery status

This creates a direct connection between spend and results, which is critical for accountability and evidence-based resource allocation.

### 7. Risk and corrective action management

Program execution rarely proceeds perfectly. IARA supports structured risk management so problems do not remain invisible.

Capabilities include:

- risk register tracking
- issue logging
- variance-driven alerts
- corrective action plan assignment
- accountability tracking for follow-up
- status tracking until closure

This closes the loop between monitoring, intervention, and operational correction.

### 8. Donor reporting and compliance

IARA is designed for real-world reporting environments, including donors with strict standards.

It supports:

- indicator summary reporting
- output and outcome tracking
- project-level performance dashboards
- donor-specific template outputs
- IATI-compatible and structured export patterns
- Excel and PDF generation for operational communication

This reduces the pain of report assembly and improves consistency across reporting cycles.

---

## End-to-end program lifecycle

A typical project in IARA follows this lifecycle:

### 1. Strategic design
- define project goals and theory of change
- set outcome and output logic
- identify key indicators and reporting periods

### 2. Planning
- define baselines and targets
- assign responsibilities to users or teams
- map budget and activity priorities

### 3. Implementation tracking
- capture activities and field data
- compare progress to targets
- track disaggregated performance

### 4. Quality assurance
- validate completeness and consistency
- detect anomalies or performance risk
- review data before final approval

### 5. Governance and decision-making
- submit values for review
- verify results with assigned stakeholders
- approve or reject records
- initiate corrective actions when required

### 6. Reporting and communication
- summarize performance for management teams
- produce donor and stakeholder reports
- publish structured results for accountability

This full lifecycle makes the platform operational rather than merely analytical.

---

## Roles and responsibilities

IARA supports multi-role operational environments.

Typical roles include:

- Administrator: system configuration and oversight
- Manager: planning and operational review
- Oversight: governance and performance assurance
- Data Entry: capturing raw program and field data

These roles support controlled escalation and accountability. Data is not treated as a single admin-only record; it follows who is responsible for it and how it is validated.

---

## Security and trust model

IARA is designed for sensitive operational data and institutional trust.

Key principles include:

- role-based security and authorization
- secure identity and token-based access
- protection of sensitive configuration and secrets
- auditability of user actions and data changes
- controlled workflow approval across key operations
- persistence of data protection keys and signing material for production environments

This makes the system suitable for organizations that must demonstrate accountability and maintain internal governance discipline.

---

## Technical architecture

The platform uses a modern and scalable architecture:

### Backend
- ASP.NET Core 10 Web API
- C# and .NET 10 runtime
- OpenIddict for OAuth 2.0 / OpenID Connect
- Entity Framework Core for database modeling and migrations
- business logic and services for calculations, governance, and reporting

### Frontend
- Angular 19 single-page application
- modern component-driven UI
- dashboards, tables, forms, and workflow interfaces
- charting and visual analytics for performance monitoring

### Data layer
- Microsoft SQL Server 2022 / 2025
- relational schema with structured reporting entities
- transactional integrity for approvals, data quality, and project records

### Infrastructure
- Docker multi-stage builds
- Docker Compose orchestration
- environment-based configuration
- health checks for production site reliability
- support for Coolify, Render, VPS, and cloud deployment patterns

---

## Deployment readiness

IARA is designed for agile deployment across multiple environments including:

- local Docker environments
- cloud-hosted Docker services
- private servers and VPS
- Coolify deployments
- Render and container-based production hosting
- AWS and other containerized infrastructure

The repo includes:

- `Dockerfile`
- `docker-compose.yml`
- `entrypoint.sh`
- `.env.example`
- `coolify.env.example`
- deployment documentation for production setups

This makes it straightforward to run the platform in both development and production environments.

---

## Coolify deployment model

The platform is explicitly structured for deployment behind a modern reverse proxy and HTTPS termination layer.

Recommended production setup:

1. Create a Docker Compose app in Coolify
2. Connect the GitHub repository
3. Use `docker-compose.yml` as the compose file
4. Set environment variables from `coolify.env.example`
5. Configure the app domain with HTTPS enabled
6. Keep DB passwords and secrets in Coolify secrets or environment variables
7. Use `/health` for service checks and health verification

This is well suited to self-hosted environments where operational simplicity and reliability matter.

---

## Key differentiators

IARA stands out because it does not treat monitoring as a reporting add-on. It treats monitoring as a business operating system.

It combines:

- program logic
- data integrity
- governance controls
- financial alignment
- donor reporting
- operational tracking

This gives organizations one coherent foundation for implementation oversight and accountability.

---

## Target users

IARA is designed for:

- monitoring and evaluation teams
- program managers
- development organizations
- public sector planners
- grant management teams
- implementing partners
- donor-funded project offices
- governance and compliance teams

---

## Quick start

### Prerequisites
- Docker
- Docker Compose

### Start locally

```bash
cp .env.example .env
docker compose up -d --build
```

Then open:

```text
http://localhost:8080
```

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
├── MONIC.sln
└── AGENTS.md
```

---

## License

This project is licensed under the GNU Affero General Public License v3.0.

See [LICENSE.txt](LICENSE.txt) for the full license text.

---

<div align="center">
  <b>IARA: Impact & Accountability Reporting Architecture</b><br/>
  Engineered for precision, transparency, and accountability in global development.
</div>






























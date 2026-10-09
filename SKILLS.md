# 🛠️ MONIC Agent Skills Catalog (SKILLS.md)

This document defines the specialized **Skills** available to AI agents and developers operating on the **MONIC** Monitoring & Evaluation platform.

---

## 📑 Skills Index

| Skill Identifier | Category | Purpose |
| :--- | :--- | :--- |
| `skill-me-indicator-engine` | M&E Domain | Design indicator metrics, token formulas, and aggregation rules |
| `skill-me-theory-of-change` | M&E Domain | Build causal pathways from Inputs to Impact |
| `skill-me-logframe-matrix` | M&E Domain | Assemble standard Logical Framework matrices |
| `skill-survey-questionnaire` | Data Collection | Construct questionnaires, Likert scales, and skip logic |
| `skill-data-governance-audit` | Data Governance | Execute 3-tier Submit ➔ Verify ➔ Approve workflows |
| `skill-efcore-database-ops` | Engineering | Execute database migrations and manage filtered indexes |
| `skill-openiddict-rbac-auth` | Security | Manage OAuth 2.0 tokens, PKCE, roles, and entity permissions |
| `skill-docker-cloud-deploy` | DevOps | Package, test, and deploy containers across multi-cloud targets |

---

## 1. 📊 `skill-me-indicator-engine`

### Description
Designs, validates, and evaluates quantitative and qualitative indicators within MONIC's dynamic calculation engine.

### Capabilities
* Configures indicators of type: `Collected`, `Calculated`, `Group`.
* Sets units and data formats: `Currency`, `Number`, `Percent`, `OptionList`.
* Defines temporal roll-up frequencies: `Month`, `Quarter`, `Year`.
* Formulates mathematical expressions using **Reverse Polish Notation (RPN)** token stacks:
  * Tokens: `Indicator`, `Number`, `Operator` (`Add`, `Subtract`, `Multiply`, `Divide`), `Parenthesis`.
* Configures aggregation types:
  * `Sum`: Aggregates values across time and child entities.
  * `MostRecent`: Carries forward the latest recorded value.

### Example Token Stack (C#)
```csharp
// Example: Ratio = (BeneficiariesReached / TotalTarget) * 100
var tokens = new List<Token>
{
    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = reachedIndicatorId, SortOrder = 0 },
    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = targetIndicatorId, SortOrder = 1 },
    new Token { TokenType = TokenType.Operator, Operator = OperatorType.Divide, SortOrder = 2 },
    new Token { TokenType = TokenType.Number, Value = 100, SortOrder = 3 },
    new Token { TokenType = TokenType.Operator, Operator = OperatorType.Multiply, SortOrder = 4 }
};
```

---

## 2. 🌿 `skill-me-theory-of-change`

### Description
Constructs and validates programmatic Theories of Change (ToC) to track causal hypothesis trees.

### Capabilities
* Creates ToC Components across 5 canonical levels:
  1. `Input`: Resources, funding, personnel, equipment.
  2. `Activity`: Training sessions, field distributions, workshops.
  3. `Output`: Tangible direct results (e.g., 500 farmers trained).
  4. `Outcome`: Behavioral or institutional shifts (e.g., 60% adoption of organic fertilizer).
  5. `Impact`: Long-term societal change (e.g., 25% increase in household food security).
* Creates directed `Relationship` links connecting lower-level components to higher-level outcomes.
* Attaches indicators to components to establish quantifiable progress monitoring.

---

## 3. 📋 `skill-me-logframe-matrix`

### Description
Assembles standardized Logical Framework (LogFrame) matrices used by international donors (USAID, FCDO, EU, Global Fund).

### Capabilities
* Structures 4 standard LogFrame tiers:
  * `Goals` / Impact
  * `Outcomes`
  * `Outputs`
  * `Activities`
* Links indicators (`LogFrameRowIndicator`) and ToC components (`LogFrameRowComponent`) to each row.
* Validates completeness (verifies every row has at least one measurable indicator and source of verification).

---

## 4. 📝 `skill-survey-questionnaire`

### Description
Designs and publishes digital survey forms with real-time validation and conditional routing.

### Capabilities
* Constructs questions using types: `Text`, `OptionList`, `Multiline`, `Note`, `Document`.
* Configures reusable option list sets (`QuestionOptionGroup`):
  * Display formats: `Rating`, `Dropdown`, `RadioList`, `Checkboxes`.
  * Pre-configured Likert scales: *Frequency, Quality, Intensity, Agreement, Approval, Satisfaction, Performance*.
* Implements conditional skip logic (`SkipLogicOption`):
  * Triggers: when question `Q` is answered with option `O`.
  * Actions: `Show` or `Hide` target question.
* Generates secure `PublicCode` tokens for open community surveys without requiring user accounts.

---

## 5. 🔍 `skill-data-governance-audit`

### Description
Orchestrates the 3-tier data review and validation pipeline, ensuring clean, verifiable data entry.

### Capabilities
* Manages the lifecycle states:
  $$\text{Draft} \longrightarrow \text{Submit} \longrightarrow \text{Verify} \longrightarrow \text{Approve (or Reject)}$$
* Evaluates user authority against `PermissionType`:
  * `Submit`: Field data collector submitting quarterly figures.
  * `Verify`: M&E Officer checking supporting evidence/documents.
  * `Approve`: Project Director finalizing numbers for official reporting.
* Automatically records reviewer identity (`UserId`), timestamp (`DateUtc`), and review notes.
* Triggers recalculation of parent entity roll-ups upon approval via `Calculation.cs`.

---

## 6. 💾 `skill-efcore-database-ops`

### Description
Manages SQL Server database operations, schema updates, migrations, and performance tuning.

### Capabilities
* Executes migrations cleanly:
  ```bash
  dotnet ef migrations add <MigrationName>
  dotnet ef database update
  ```
* Enforces SQL Server filtered unique indexes for nullable fields:
  ```csharp
  db.AddNullableUniqueIndexes();
  // Executes: CREATE UNIQUE NONCLUSTERED INDEX IX_Table_Field ON Table(Field) WHERE Field IS NOT NULL;
  ```
* Runs database maintenance:
  * Expired token pruning (`OpenIddictTokens` cleanup).
  * Weekly error log rotation (`DeleteErrors`).

---

## 7. 🔐 `skill-openiddict-rbac-auth`

### Description
Configures and audits application security, token lifecycle, and role-based permissions.

### Capabilities
* Implements OpenIddict OAuth 2.0 endpoints:
  * Token endpoint: `/connect/token` (Password flow, Refresh token flow).
  * End session: `/connect/logout`.
* Enforces PKCE (Proof Key for Code Exchange) on client authentication.
* Manages the 6 core system roles:
  1. `Administrator`: Complete system configuration and user management.
  2. `Manager`: Project management, indicator creation, questionnaire publishing.
  3. `Oversight`: Data verification and approval authority.
  4. `Questionnaires`: Form design and survey management.
  5. `Folders`: Document repository administration.
  6. `DataEntry`: Field data collection and response submission.

---

## 8. 🐳 `skill-docker-cloud-deploy`

### Description
Packages, configures, and deploys MONIC containers across local environments and cloud platforms.

### Capabilities
* Builds optimized multi-stage images:
  * Stage 1: `node:20-alpine` (Angular production build).
  * Stage 2: `mcr.microsoft.com/dotnet/sdk:10.0` (C# Release compile).
  * Stage 3: `mcr.microsoft.com/dotnet/aspnet:10.0` (Minimal secure runtime).
* Validates cloud-specific configurations:
  * **Coolify**: Traefik labels, Docker Compose integration, internal networking.
  * **Render**: Dynamic `$PORT` detection, external SQL Server connection string.
  * **AWS (ECS / App Runner)**: ECR image push, `/health` endpoint load-balancer target.
  * **Local Host**: Port forwarding to `http://localhost:8080` with SQL Server 2022 container.

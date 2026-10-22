# Implementation Plan — Phase 1: Collaboration & Budget

> Scope confirmed with stakeholder: **Comments + Notifications + Document Tags + Budget & Expenditure**.
> Companion document: [`GAP_ANALYSIS.md`](GAP_ANALYSIS.md).

## 0. Conventions observed in this codebase (must-follow)

- Models: `WEB/Models/<Feature>.cs`, Guid PKs generated in ctor, `ToString()`/`Equals()`/`GetHashCode()` overrides, DataAnnotations.
- DbContext: partial files per feature — `ApplicationDBContext.<Feature>.cs` with `DbSet<>` properties + `Configure<Feature>Model(ModelBuilder)` called from `ApplicationDBContext.OnModelCreating`. Global `DeleteBehavior.Restrict` is applied post-hoc; configure `.OnDelete(DeleteBehavior.Restrict)` explicitly for new FKs.
- DTOs: `WEB/Models/DTOs/<Entity>DTO.cs` with static `ModelFactory.Create(entity)` / `ModelFactory.Hydrate(entity, dto)` partial-class extensions. DTO + entity share the `Monic.Web.Models` namespace.
- Search: `WEB/Models/SearchOptions/<Entity>SearchOptions.cs : SearchOptions`; controllers return `GetPaginatedResponse(...)` (writes `X-Pagination` header).
- Controllers: `[Route("api/[Controller]"), Authorize]`, ctor `(IDbContextFactory<ApplicationDbContext>, UserManager<User>, AppSettings) : base(...)`, `AuthorizeRoles(Roles.X)` for admin endpoints, Save = `POST api/x/{id}` (id == Guid.Empty → new), delete guard children first inside `Code.Db.CreateTransactionScope()`.
- Frontend: models in `common/models`, services extend `SearchQuery` with `buildQueryParams` + `x-pagination` parsing; NgModule app (declare in `custom.module.ts`, routes in `custom.routes.ts`); Bootstrap 5 + ng-bootstrap + Font Awesome; `appHasRole` directive guards menu items.
- Angular 19 codebase is NgModule-based — new components stay `standalone: false` (do NOT introduce standalone components here).
- Verification: `dotnet build WEB` and `ng build` must pass; migration must be inspected (SOP 1).

## 1. Backend work breakdown

| # | Task | Files |
|---|------|-------|
| B1 | Entities: `Comment`, `CommentReaction`, `Notification`, `Tag`, `DocumentTag` | `Models/Collaboration.cs` |
| B2 | Entities: `BudgetCategory`, `BudgetLine` | `Models/Budget.cs` |
| B3 | DbContext partials + Fluent config (indexes, FK restrict) | `Models/ApplicationDBContext.Collaboration.cs`, `Models/ApplicationDBContext.Budget.cs` |
| B4 | DTOs + ModelFactory | `Models/DTOs/CommentDTO.cs`, `NotificationDTO.cs`, `TagDTO.cs`, `BudgetDTO.cs`; extend `DocumentDTO` with `Tags` |
| B5 | SearchOptions | `Models/SearchOptions/{Comment,Notification,Tag,BudgetCategory,BudgetLine}SearchOptions.cs` |
| B6 | Controllers | `Controllers/{Comments,Notifications,Tags,BudgetCategories,BudgetLines}Controller.cs` |
| B7 | Document tag sync on save + tag include on search | edit `Controllers/DocumentsController.cs` |
| B8 | Review-workflow notifications (Verified/Approved/Rejected → notify submitter) | edit `Controllers/DataReviewsController.cs` |
| B9 | Budget summary endpoint (per-category planned/actual/variance/utilization) | in `BudgetLinesController` |
| B10 | EF migration `AddCommentsNotificationsTagsBudget` + inspect Up()/Down() | `Migrations/` |

### API surface (new)

| Endpoint | Method | Roles | Notes |
|----------|--------|-------|-------|
| `api/comments?subjectType=&subjectId=` | GET | any authenticated | list w/ reactions + authors |
| `api/comments/{id}` | GET/POST/DELETE | any authenticated | POST creates mention notifications |
| `api/notifications` | GET | any authenticated | current user's, newest first |
| `api/notifications/unreadcount` | GET | any authenticated | `{ count }` |
| `api/notifications/markread` | POST | any authenticated | `{ ids: [] }` (empty = all) |
| `api/notifications/{id}` | DELETE | any authenticated | own only |
| `api/tags` | GET | any authenticated | search, includes useCount |
| `api/tags/{id}` | POST/DELETE | Manager+ | DELETE blocked if in use |
| `api/budgetcategories` | GET | any authenticated | dictionary |
| `api/budgetcategories/{id}` | POST/DELETE | Manager+ | DELETE blocked if used by lines |
| `api/budgetlines?projectId=&categoryId=` | GET | any authenticated | paged |
| `api/budgetlines/{id}` | POST/DELETE | Manager+ | |
| `api/budgetlines/summary?projectId=` | GET | any authenticated | per-category aggregates |

## 2. Frontend work breakdown

| # | Task | Files |
|---|------|-------|
| F1 | Models | `common/models/{comment,notification,tag,budget}.model.ts`; extend `document.model.ts` |
| F2 | Services | `common/services/{comment,notification,tag,budget}.service.ts` |
| F3 | Reusable comments panel (list, add, @mention autocomplete, reactions) | `common/components/comments/comments.component.*` |
| F4 | Notification bell (badge, dropdown/offcanvas, mark read, navigate) + header wiring | `common/components/notifications-bell/*`, edit `header.component.html/.ts` |
| F5 | Document tags UI: badges + filter on list; tag editor on edit | edit `document.list.component.*`, `document.edit.component.*` |
| F6 | Budget pages: project picker, summary cards, lines table + modal; category admin | `budget/{budgets,budgetcategories}.component.*`, `budget/budgetline.modal.*` |
| F7 | Routes + module declarations + nav menu entries | edit `custom.routes.ts`, `custom.module.ts`, `nav-menu.component.html` |
| F8 | Comments embedded on Response record page | edit `admin/responses/response.edit.component.html/.ts` |

## 3. Build order (dependency-ordered)

1. B1→B5 (models/DTOs/search options) — no dependencies
2. B3 (context partials) → B10 (migration)
3. B6→B9 (controllers) — depends on B1-B5
4. F1→F2 (models/services) — depends on API contract
5. F3→F4 (shared components + header)
6. F5 (documents), F6 (budget pages), F8 (record page comments)
7. F7 (wiring: routes/module/nav)
8. Verification: `dotnet build`, `ng build`, migration inspection

## 4. Verification checklist (SOP / spec alignment)

- [ ] `dotnet build WEB/WEB.csproj` — 0 errors
- [ ] `ng build` — 0 errors
- [ ] Migration `Up()` creates all 7 tables + indexes; `Down()` drops them; no data loss paths
- [ ] `DeleteBehavior.Restrict` on all new FKs (matches global convention)
- [ ] Notifications never leak across users (server filters by `CurrentUser.Id`)
- [ ] Comment delete cascades reactions via explicit `ExecuteDeleteAsync` + transaction scope
- [ ] Tag delete blocked when referenced by documents; category delete blocked when referenced by budget lines
- [ ] Menu items role-guarded; budget pages reachable for Manager/Administrator

# Implementation Plan: M&E Modernization for Low-End Machines

**Branch**: `master` | **Date**: 2026-10-15 | **Spec**: [ME_MODERNIZATION_PLAN.md](file:///C:/Users/Administrator/Downloads/MONICv2/MONICv.001/ME_MODERNIZATION_PLAN.md)

## Summary

Optimize MONIC platform performance for low-end machines (devices with ≤2GB RAM, low-power multi-core CPUs, and constrained field connectivity) and update the modernization roadmap based on empirical benchmark findings, adversarial review, and bug fixes.

## Technical Context

- **Backend**: ASP.NET Core 10, Entity Framework Core 10, SQL Server 2022.
- **Frontend**: Angular 19 SPA (`@angular/build:application`, TypeScript 5.8, RxJS 7.8).
- **Test Framework**: xUnit, Microsoft.EntityFrameworkCore.InMemory, .NET 10.
- **Target Platform**: Low-end field laptops, tablets, and lightweight cloud containers (<1GB RAM).
- **Performance Goals**:
  - API response times <100ms for data saves.
  - SPA initial transfer size <350kB (reduced from 5.59MB).
  - Memory allocation per calculation <100KB (reduced from ~50MB).
  - Elimination of CommonJS optimizer bailouts.
  - Zero stale calculation data across chained multi-tiered indicator dependencies.

## Key Changes & Benchmark Findings

1. **Backend Calculation Optimization (`WEB/Code/Calculation.cs`)**:
   - Converted eager synchronous indicator catalog loading in constructor into lazy/scoped execution.
   - Built multi-level transitive dependency resolution: replaces naive single-hop search with full dependency graph traversal.
   - Implemented `OrderTopologicalLevels` (Kahn's algorithm) to execute calculated indicators strictly in topological batches ($L_0, L_1, L_2, \dots$), guaranteeing parent calculations commit before child calculations read from `Data`.
   - Added acyclic graph validation detecting circular dependencies ($A \to B \to A$) and self-references, failing fast with informative `InvalidOperationException` errors.
   - Integrated `IMemoryCache` for indicator formula catalogs with 5-minute sliding expiration, dropping repeated DB roundtrips to 0 during batch data entry.
   - Converted blocking synchronous EF Core queries on `db.Dates` in `SaveAsync` and `ChangeEntityLinkAsync` to asynchronous `await ... ToListAsync()`.
   - Fixed `RequiresSubmit` resolution to check `SourceIndicator.RequiresSubmit`.

2. **SQL Stored Procedure Fix (`WEB/Code/SQL/StoredProcedures.sql` & Migration)**:
   - Corrected line 96 in `Calculate` stored procedure from `SELECT SourceIndicatorId FROM Indicators` to `SELECT IndicatorId FROM Indicators`, resolving a fatal runtime SQL syntax error.

3. **Backend Network & Caching (`WEB/Program.cs`)**:
   - Configured Response Compression with `BrotliCompressionProvider` and `GzipCompressionProvider` set to `CompressionLevel.Fastest` (CPU-efficient on low-end servers).
   - Added compression support for `application/json`, `application/geo+json`, and `image/svg+xml`.
   - Dual-tiered static file caching: immutable 1-year caching for content-hashed assets (`-[A-Z0-9]{8}\.(js|css)$`, `/media/`), safe 24-hour caching for unhashed assets (GeoJSON, logos), and `no-cache` for HTML entry points.

4. **Frontend Bundle Optimization (`WEB/ClientApp`)**:
   - Configured `angular.json` default configuration to `production`.
   - Updated `package.json` scripts (`"build": "ng build --configuration production"`).
   - Consolidated `allowedCommonJsDependencies` to resolve warnings for `dagre`, `webcola`, `zxcvbn`, `quill`, and `quill-delta`.
   - Synchronized production assets to `WEB/wwwroot`.
   - Result: Initial transfer payload reduced from 5.59MB to 325.76kB (84%+ reduction).

5. **Test Verification (`WEB.Tests`)**:
   - Added `CalculationTests.cs` covering postfix stack conversion, precedence, parentheses, independent indicator levels, linear chains, diamond graphs, circular dependency detection, self-reference detection, and `RequiresSubmit` propagation.
   - Added `CalculationPipelineTests.cs` using `Microsoft.EntityFrameworkCore.InMemory` testing multi-level transitive resolution, diamond dependency grouping, memory cache hits, and indicator isolation.
   - Executed full test suite: **35 tests passed** with zero failures.

## Phase 2 & 3 Modernization Roadmap

- **Phase 2: Offline-First Field Surveys**: Local staging via IndexedDB, background sync service worker, delta sync protocol.
- **Phase 3: Low-End UI Virtualization**: CDK Virtual Scroll on large LogFrames and Data tables, low-power mode for charts.
- **Phase 4: CI/CD Performance Guardrails**: Automated bundle budget tests and microbenchmarks in deployment workflows.

# Presentation readiness: M&E correctness

Run these checks before demonstrating the system:

1. `dotnet test WEB.Tests/WEB.Tests.csproj`
2. Verify external submissions reject unknown entity codes and period codes.
3. Verify re-sync reports duplicates and no duplicate responses or contributions.
4. Verify malformed mapping filters are rejected rather than silently contributing.
5. Verify downstream calculated indicators recalculate after submitted response roll-up.
6. Do not rely on unverified dashboard scores as official results.

Presentation dataset recommendation:

- use only approved or explicitly labelled test data;
- show the data source and reporting period beside every metric;
- distinguish live, submitted, verified, and approved values;
- keep a one-page reconciliation of indicator totals and disaggregations;
- do not present currency totals containing missing exchange rates as complete;
- show the latest period and refresh time on dashboards.

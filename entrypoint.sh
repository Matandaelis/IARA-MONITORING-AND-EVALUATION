#!/bin/sh
set -e

# =======================================================
# MONIC application container entrypoint (POSIX sh).
# SQL Server runs as a SEPARATE service (docker-compose 'monic-db',
# AWS RDS, Azure SQL, ...). This container only runs the
# ASP.NET Core app + freshly built Angular SPA from /app/wwwroot.
# =======================================================

# Connection string: only defaulted when the caller did not provide one,
# so docker-compose / Coolify / Render environment overrides always win.
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Server=monic-db,1433;Database=MONIC;User Id=sa;Password=@Monic123Secure!;TrustServerCertificate=true;Encrypt=false;}"

# Persistent directories (OpenIddict certificate + DataProtection keys).
# The volume may be mounted with restrictive permissions; tolerate failure.
mkdir -p /app/data/dataprotection-keys /app/logs 2>/dev/null || true

cd /app

# exec: dotnet becomes PID 1 and receives SIGTERM directly (graceful shutdown)
exec dotnet Monic.Web.dll

#!/bin/bash
set -e

echo "=========================================="
echo " Starting MONIC All-in-One Container"
echo " (SQL Server 2022 + ASP.NET Core 10 + Angular)"
echo "=========================================="

SA_PASSWORD="${DB_PASSWORD:-@Monic123Secure!}"
export ACCEPT_EULA="Y"
export MSSQL_SA_PASSWORD="$SA_PASSWORD"
export MSSQL_PID="Developer"

# [1/4] Ensure persistent directories exist and have correct permissions
echo "[1/4] Preparing data directories and filesystem permissions..."
mkdir -p /var/opt/mssql/data /var/opt/mssql/log /var/opt/mssql/secrets /app/data/dataprotection-keys /app/logs
chown -R mssql:root /var/opt/mssql 2>/dev/null || true
chmod -R 775 /var/opt/mssql 2>/dev/null || true
chown -R root:root /app/data /app/logs 2>/dev/null || true
chmod -R 775 /app/data /app/logs 2>/dev/null || true

# [2/4] Start SQL Server as mssql user
echo "[2/4] Starting Microsoft SQL Server 2022 as mssql user..."
if [ -x /opt/mssql/bin/permissions_check.sh ]; then
    SQL_EXEC="/opt/mssql/bin/permissions_check.sh /opt/mssql/bin/sqlservr"
elif [ -x /opt/mssql/bin/launch_sqlservr.sh ]; then
    SQL_EXEC="/opt/mssql/bin/launch_sqlservr.sh"
else
    SQL_EXEC="/opt/mssql/bin/sqlservr"
fi

if command -v runuser >/dev/null 2>&1; then
    runuser -u mssql -- env ACCEPT_EULA="Y" MSSQL_SA_PASSWORD="$SA_PASSWORD" MSSQL_PID="Developer" $SQL_EXEC &
    SQL_PID=$!
elif id "mssql" &>/dev/null; then
    su -s /bin/bash mssql -c "ACCEPT_EULA='Y' MSSQL_SA_PASSWORD='$SA_PASSWORD' MSSQL_PID='Developer' $SQL_EXEC" &
    SQL_PID=$!
else
    $SQL_EXEC &
    SQL_PID=$!
fi

# [3/4] Wait for SQL Server to accept connections
echo "[3/4] Waiting for SQL Server to become ready..."
READY=0
for i in $(seq 1 60); do
    if ! kill -0 $SQL_PID 2>/dev/null; then
        echo "FATAL: SQL Server process (PID $SQL_PID) has died! Error log tail:"
        tail -n 40 /var/opt/mssql/log/errorlog 2>/dev/null || true
        exit 1
    fi

    if (/opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1) || \
       (/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U sa -P "$SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1) || \
       (bash -c "cat < /dev/null > /dev/tcp/127.0.0.1/1433" 2>/dev/null); then
        echo "[3/4] SQL Server is ONLINE and accepting connections!"
        READY=1
        break
    fi
    echo "      Waiting for SQL Server to start... ($i/60)"
    sleep 2
done

if [ $READY -eq 0 ]; then
    echo "WARNING: SQL Server readiness probe did not confirm in 120s, proceeding anyway..."
fi

sleep 2

# [4/4] Configure database connection and runtime settings
export ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=MONIC;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=true;Encrypt=false;"
export ASPNETCORE_URLS="http://0.0.0.0:3000;http://0.0.0.0:80"
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_FORWARDEDHEADERS_ENABLED="true"
export DOTNET_RUNNING_IN_CONTAINER="true"

echo "[4/4] Starting MONIC Web Application..."
cd /app
if [ -f "./Monic.Web" ]; then
    ./Monic.Web &
    APP_PID=$!
elif [ -f "./Monic.Web.dll" ]; then
    dotnet Monic.Web.dll &
    APP_PID=$!
else
    echo "ERROR: Neither ./Monic.Web nor ./Monic.Web.dll was found in /app!"
    exit 1
fi

echo "=========================================="
echo " MONIC is running successfully!"
echo " Web: http://localhost:3000 / http://localhost:80"
echo " Database: localhost:1433"
echo " Health: /health"
echo "=========================================="

# Graceful termination handler
shutdown() {
    echo "Shutting down MONIC services..."
    kill -TERM $APP_PID 2>/dev/null || true
    kill -TERM $SQL_PID 2>/dev/null || true
    wait $APP_PID 2>/dev/null || true
    wait $SQL_PID 2>/dev/null || true
    exit 0
}

trap shutdown SIGTERM SIGINT

# Wait for either process to exit
wait -n $APP_PID $SQL_PID
EXIT_CODE=$?
echo "A service exited with code $EXIT_CODE. Initiating container shutdown..."
shutdown

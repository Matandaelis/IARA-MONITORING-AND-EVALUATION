# MONIC Coolify Deployment - Production Configuration

This file contains the exact environment variables and settings needed for a production-safe deployment on Coolify.

## ⚠️ Before You Deploy

1. **Change ALL placeholder values** - Do not use examples as-is
2. **Use strong passwords** - See password requirements below
3. **Enable HTTPS** - Use Coolify's reverse proxy SSL termination
4. **Backup plan** - Have a backup strategy before going live
5. **Test locally first** - Run locally with Docker Compose first

## 🔐 Password Requirements

SQL Server requires strong passwords for the SA account. Must contain:
- ✅ Minimum 8 characters
- ✅ At least 1 uppercase letter (A-Z)
- ✅ At least 1 lowercase letter (a-z)
- ✅ At least 1 number (0-9)
- ✅ At least 1 special character (!@#$%^&*)

### Valid Examples
```
MyMonic@2024!
SecurePass123$
P@ssw0rd!MONIC
Coolify#Prod2024
```

### Invalid Examples
```
password         # No uppercase, number, special char
Pass123          # No special character
P@ss1            # Too short
UPPERCASE123!    # No lowercase
```

## 📋 Coolify Environment Variables

Copy and paste these into Coolify under **Application → Environment Variables**.

### REQUIRED: Database Configuration

```
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=true;Encrypt=false;
```

**⚠️ Replace `YourSecurePassword123!` with your actual strong password**

Example with real password:
```
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=MyMonic@2024!;TrustServerCertificate=true;Encrypt=false;
```

### REQUIRED: ASP.NET Core Settings

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

### REQUIRED: Application Settings

```
Settings__RootUrl=https://monic.example.com/
Settings__SiteName=MONIC
Settings__AccessTokenExpiryMinutes=10
Settings__RefreshTokenExpiryMinutes=10880
```

**⚠️ Replace `monic.example.com` with your actual domain**

Example:
```
Settings__RootUrl=https://myorg-monic.com/
Settings__SiteName=MyOrganization MONIC
```

### OPTIONAL: Email Configuration

If you want to enable email notifications:

```
Settings__EmailSettings__SendEmails=true
Settings__EmailSettings__SenderName=MONIC Alerts
Settings__EmailSettings__Sender=noreply@monic.example.com
Settings__EmailSettings__SMTP=smtp.gmail.com
Settings__EmailSettings__SMTPPort=587
Settings__EmailSettings__UserName=your-email@gmail.com
Settings__EmailSettings__Password=your-app-specific-password
Settings__EmailSettings__SSL=true
Settings__EmailSettings__EmailToErrors=admin@monic.example.com
Settings__EmailSettings__SubstitutionEmailAddress=admin@monic.example.com
```

**Notes on Email:**
- **Gmail**: Use app-specific passwords, not your account password
- **Office 365**: Use `smtp.office365.com` with port `587`
- **Custom SMTP**: Update server and port accordingly
- If not configured, email is disabled (safe default)

### OPTIONAL: Logging & Monitoring

```
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
Settings__UseApplicationInsights=false
```

### OPTIONAL: Azure Configuration

Only set if using Azure Key Vault or Azure Storage:

```
KeyVault__VaultUri=https://your-keyvault.vault.azure.net/
Settings__AzureSettings__DataProtection__BlobUri=https://yourstg.blob.core.windows.net/dataprotection/
Settings__AzureSettings__DataProtection__KeyIdentifier=https://your-keyvault.vault.azure.net/keys/dataprotectionkey/
```

## 📦 Complete Paste-Ready Configuration

### Option 1: Minimal Production (Recommended for Testing)

```
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=MyMonic@2024!;TrustServerCertificate=true;Encrypt=false;
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Settings__RootUrl=https://monic.example.com/
Settings__SiteName=MONIC
Settings__AccessTokenExpiryMinutes=10
Settings__RefreshTokenExpiryMinutes=10880
Settings__EmailSettings__SendEmails=false
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
```

**Customization Needed:**
1. Change `MyMonic@2024!` to your actual strong password
2. Change `monic.example.com` to your actual domain
3. Update `Settings__SiteName` to your organization name

### Option 2: Full Production (With Email)

```
ConnectionStrings__DefaultConnection=Server=monic-db,1433;Database=MONIC;User Id=sa;Password=MyMonic@2024!;TrustServerCertificate=true;Encrypt=false;
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Settings__RootUrl=https://monic.example.com/
Settings__SiteName=MONIC
Settings__AccessTokenExpiryMinutes=10
Settings__RefreshTokenExpiryMinutes=10880
Settings__EmailSettings__SendEmails=true
Settings__EmailSettings__SenderName=MONIC Alerts
Settings__EmailSettings__Sender=noreply@monic.example.com
Settings__EmailSettings__SMTP=smtp.gmail.com
Settings__EmailSettings__SMTPPort=587
Settings__EmailSettings__UserName=your-email@gmail.com
Settings__EmailSettings__Password=your-app-password
Settings__EmailSettings__SSL=true
Settings__EmailSettings__EmailToErrors=admin@monic.example.com
Settings__EmailSettings__SubstitutionEmailAddress=admin@monic.example.com
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
```

**Customization Needed:**
1. Database password (see above)
2. Domain name
3. Organization name
4. All email settings (SMTP server, credentials, etc.)

## 🔄 .env File for Local Testing

Before deploying to Coolify, test locally with this `.env`:

```env
DB_PASSWORD=MyMonic@2024!
DOMAIN=localhost
SITE_NAME=MONIC
APP_PORT=80
SEND_EMAILS=false
EMAIL_SENDER_NAME=MONIC
EMAIL_SENDER=noreply@monic.local
SMTP_SERVER=localhost
SMTP_PORT=25
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_SSL=false
ERROR_EMAIL=admin@monic.local
SUBSTITUTION_EMAIL=admin@monic.local
USE_APP_INSIGHTS=false
```

Test locally:
```bash
cp .env.example .env
nano .env  # Edit values above
docker compose up --build -d
curl http://localhost/health
```

## ✅ Coolify Deployment Checklist

- [ ] **Database Password**: Changed to strong password (8+ chars, mixed case, numbers, special chars)
- [ ] **Domain**: Set to your actual domain (not localhost)
- [ ] **Site Name**: Updated to your organization name
- [ ] **HTTPS**: Coolify reverse proxy enabled with SSL
- [ ] **Forwarded Headers**: `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
- [ ] **Environment**: Set to `Production`
- [ ] **Email**: Configured if needed, or disabled if not
- [ ] **Ports**: App port 80, database port internal only
- [ ] **Volumes**: Database volume mounted for persistence
- [ ] **Backup**: Plan in place before going live
- [ ] **Testing**: Tested locally with Docker Compose first
- [ ] **DNS**: Domain points to Coolify instance
- [ ] **SSL Certificate**: Valid certificate configured in Coolify

## 🚀 Deployment Steps in Coolify

1. **Create Application**
   - Type: Docker Compose
   - Repository: Matandaelis/MONIC
   - Branch: master

2. **Build Configuration**
   - Dockerfile: `./Dockerfile`
   - Compose: `./docker-compose.yml`
   - Base: `/`

3. **Add Environment Variables**
   - Copy variables from "Option 1" or "Option 2" above
   - Replace placeholders with your values
   - Use Coolify secrets for sensitive values

4. **Configure Volumes**
   - `/var/opt/mssql` → `monic-db-data`
   - `/app/logs` → `monic-app-logs`

5. **Set Up Reverse Proxy**
   - Enable HTTPS
   - Point to your domain
   - Configure SSL certificate

6. **Deploy**
   - Click Deploy
   - Wait 5-10 minutes
   - Monitor logs: **Logs** tab

7. **Verify**
   ```
   curl https://your-domain/health
   ```

## 🔍 Deployment Diagnostics

### If Deployment Fails

1. **Check Logs**
   - Coolify: Application → Logs
   - Full output should appear there

2. **Common Errors**

   **Error**: `Connection to monic-db:1433 failed`
   - Database not ready yet
   - Wait 30+ seconds
   - Verify password is correct in connection string

   **Error**: `Password does not meet complexity requirements`
   - DB_PASSWORD too weak
   - Use format: `MyMonic@2024!`
   - 8+ chars, uppercase, lowercase, number, special char

   **Error**: `Health check failing`
   - App still starting up
   - Wait 40+ seconds
   - Check `/health` endpoint is accessible

   **Error**: `.NET 10 runtime not found`
   - Host doesn't support .NET 10
   - Check Coolify Docker version
   - May need to update Docker

3. **Get Help**
   - Check logs in Coolify
   - Search issues: https://github.com/Matandaelis/MONIC/issues
   - Include logs when asking for help

## 📊 Post-Deployment Verification

After successful deployment:

1. **Access Application**
   ```
   https://your-domain
   ```
   You should see MONIC setup screen

2. **Create Admin Account**
   - Follow setup wizard
   - Create first admin user

3. **Check Health**
   ```
   curl https://your-domain/health
   ```
   Should return 200 OK

4. **Verify Database**
   - Login to admin panel
   - Check any page that queries data
   - Should work without errors

5. **Monitor Performance**
   - Coolify: Stats tab
   - Watch CPU and memory usage
   - Adjust resource limits if needed

## 🔐 Security Post-Deployment

- [ ] Change default admin password immediately
- [ ] Review user roles and permissions
- [ ] Configure firewall rules
- [ ] Set up automated backups
- [ ] Enable audit logging if available
- [ ] Review SSL certificate validity
- [ ] Test email notifications if enabled
- [ ] Plan disaster recovery

## 📞 Need Help?

1. **Documentation**: See [DOCKER_DEPLOYMENT.md](../DOCKER_DEPLOYMENT.md)
2. **Issues**: https://github.com/Matandaelis/MONIC/issues
3. **Logs**: Always include application logs in bug reports

---

**Remember**: Keep database passwords secure, enable HTTPS, and maintain backups!

        store.Open(OpenFlags.ReadOnly);

        var now = DateTime.UtcNow;

        var certificate = store.Certificates
            .Find(
                X509FindType.FindBySubjectDistinguishedName,
                subjectDistinguishedName,
                validOnly: false)
            .OfType<X509Certificate2>()
            .Where(c =>
                c.HasPrivateKey &&
                c.NotBefore.ToUniversalTime() <= now &&
                c.NotAfter.ToUniversalTime() > now)
            .OrderByDescending(c => c.NotAfter)
            .FirstOrDefault();

        if (certificate == null)
        {
            throw new Exception(
                $"Certificate not found. Subject Distinguished Name: {subjectDistinguishedName}");
        }

        return certificate;
    }

    public static X509Certificate2 GetLocalCertificate(
        AppSettings appSettings)
    {
        // Store the certificate in the persistent application data directory.
        // Coolify mounts /app/data as a persistent volume.
        var dataDirectory = Path.Combine(
            appSettings.RootPath,
            "data");

        Directory.CreateDirectory(dataDirectory);

        var certificatePath = Path.Combine(
            dataDirectory,
            "certificate.pfx");

        var password = appSettings.CertificatePassword ?? string.Empty;

        if (!File.Exists(certificatePath))
        {
            var certificate = BuildSelfSignedServerCertificate(
                appSettings.SiteName,
                password);

            var bytes = certificate.Export(
                X509ContentType.Pfx,
                password);

            File.WriteAllBytes(
                certificatePath,
                bytes);

            return certificate;
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            password,
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.Exportable);
    }

    public static X509Certificate2 BuildSelfSignedServerCertificate(
        string certificateName,
        string password)
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();

        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");

        // Also include the configured site name when it is a valid DNS name.
        if (!string.IsNullOrWhiteSpace(certificateName) &&
            !string.Equals(
                certificateName,
                "localhost",
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                sanBuilder.AddDnsName(certificateName);
            }
            catch (ArgumentException)
            {
                // Ignore invalid DNS names. localhost remains available.
            }
        }

        var distinguishedName = new X500DistinguishedName(
            $"CN={certificateName ?? "MONIC"}");

        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            distinguishedName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature,
                false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1")
                },
                false));

        request.CertificateExtensions.Add(
            sanBuilder.Build());

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(3650));

        var bytes = certificate.Export(
            X509ContentType.Pfx,
            password);

        return X509CertificateLoader.LoadPkcs12(
            bytes,
            password,
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.Exportable);
    }

    public static string GetEncryptionCertificatePath(
        AppSettings appSettings) =>
        Path.Combine(
            appSettings.RootPath,
            "data",
            "encryption-certificate.pfx");

    public static string GetSigningCertificatePath(
        AppSettings appSettings) =>
        Path.Combine(
            appSettings.RootPath,
            "data",
            "signing-certificate.pfx");

    public static void CreateEncryptionCertificate(
        AppSettings appSettings)
    {
        var dataDirectory = Path.Combine(
            appSettings.RootPath,
            "data");

        Directory.CreateDirectory(dataDirectory);

        using var algorithm = RSA.Create(2048);

        var subject = new X500DistinguishedName(
            "CN=Fabrikam Encryption Certificate");

        var request = new CertificateRequest(
            subject,
            algorithm,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(2));

        File.WriteAllBytes(
            GetEncryptionCertificatePath(appSettings),
            certificate.Export(
                X509ContentType.Pfx,
                appSettings.CertificatePassword ?? string.Empty));
    }

    public static void CreateSigningCertificate(
        AppSettings appSettings)
    {
        var dataDirectory = Path.Combine(
            appSettings.RootPath,
            "data");

        Directory.CreateDirectory(dataDirectory);

        using var algorithm = RSA.Create(2048);

        var subject = new X500DistinguishedName(
            "CN=Fabrikam Signing Certificate");

        var request = new CertificateRequest(
            subject,
            algorithm,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature,
                critical: true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(2));

        File.WriteAllBytes(
            GetSigningCertificatePath(appSettings),
            certificate.Export(
                X509ContentType.Pfx,
                appSettings.CertificatePassword ?? string.Empty));
    }
}

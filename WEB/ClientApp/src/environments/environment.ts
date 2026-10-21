export const environment = {
    production: false,
    baseUrl: `${window.location.origin}/`,
    // matches with launchSettings.json (which configures on which port the development api runs)
    baseApiUrl: `https://localhost:44510/api/`,
    baseAuthUrl: `https://localhost:44510/`,
    siteName: 'MONIC',
    // LogFrame v2 (read-only tree) feature flag. Enabled in dev; off in prod until
    // the multi-tenancy/Lago billing discussion with the client is settled.
    logFrameV2: true
};

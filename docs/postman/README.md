MONIC Postman Documentation

Backend URL: https://localhost:44510
SPA URL: https://localhost:44410

OpenAPI spec is available at https://localhost:44510/openapi/v1.json

To import into Postman:
1. Open Postman → Import → Paste Raw Text
2. Paste the contents of https://localhost:44510/openapi/v1.json
3. Create environment with variables:
   baseUrl = https://localhost:44510
   tokenUrl = https://localhost:44510/connect/token
   username
   password
   accessToken

OAuth2 Password Grant:
POST {{tokenUrl}}
Content-Type: application/x-www-form-urlencoded
grant_type=password&username={{username}}&password={{password}}&client_id=Monic.Web&client_secret=

Set accessToken from response and use Bearer token for API calls.

SSL verification should be disabled for localhost (self-signed cert).

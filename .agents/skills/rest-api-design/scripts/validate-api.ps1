<#
.SYNOPSIS
    Validate an OpenAPI/Swagger specification file against REST design best practices.
.DESCRIPTION
    Checks for OpenAPI/Swagger declaration, required info/paths sections, RESTful path naming conventions
    (no trailing slashes, no action verbs, lowercase casing), and standard HTTP status codes.
.PARAMETER SpecPath
    Path to the OpenAPI/Swagger YAML or JSON specification file.
.EXAMPLE
    .\validate-api.ps1 -SpecPath ..\templates\api-scaffold.yaml
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SpecPath
)

if (-not (Test-Path -Path $SpecPath)) {
    Write-Error "Specification file not found: $SpecPath"
    exit 1
}

$fullPath = Resolve-Path $SpecPath
Write-Host "Validating API spec: $fullPath" -ForegroundColor Cyan

$content = Get-Content -Path $fullPath -Raw
$lines = Get-Content -Path $fullPath
$errors = 0
$warnings = 0

# 1. Check for OpenAPI or Swagger declaration
if ($content -match "(?m)^(openapi|swagger):\s*['`"]?([^'`"\r\n]+)") {
    $specType = $matches[1]
    $specVer = $matches[2].Trim()
    Write-Host "  [OK] Found specification: $specType $specVer" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Missing 'openapi:' or 'swagger:' root definition" -ForegroundColor Red
    $errors++
}

# 2. Check for info section
if ($content -match "(?m)^info:\s*$") {
    Write-Host "  [OK] Found 'info:' section" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Missing 'info:' section" -ForegroundColor Red
    $errors++
}

# 3. Check for paths section
if ($content -match "(?m)^paths:\s*$") {
    Write-Host "  [OK] Found 'paths:' section" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Missing 'paths:' section" -ForegroundColor Red
    $errors++
}

# 4. Check paths conventions
$inPaths = $false
foreach ($line in $lines) {
    if ($line -match "^paths:\s*$") {
        $inPaths = $true
        continue
    }
    if ($inPaths -and $line -match "^[a-zA-Z0-9_-]+:") {
        # Exited paths section (new top-level key)
        $inPaths = $false
        continue
    }
    if ($inPaths -and $line -match "^\s{2}(/[^:\s]+):") {
        $pathItem = $matches[1]

        # Check trailing slash
        if ($pathItem.Length -gt 1 -and $pathItem.EndsWith('/')) {
            Write-Host "  [WARN] Path has trailing slash: $pathItem (REST best practice: avoid trailing slashes)" -ForegroundColor Yellow
            $warnings++
        }

        # Remove path parameters before checking segments
        $cleanPath = [regex]::Replace($pathItem, "\{[^}]+\}", "")

        # Check uppercase letters
        if ($cleanPath -cmatch "[A-Z]") {
            Write-Host "  [WARN] Path contains uppercase characters: $pathItem (REST best practice: use lowercase kebab-case or snake_case)" -ForegroundColor Yellow
            $warnings++
        }

        # Check action verbs
        if ($cleanPath -match "/(get|set|create|delete|update|remove|add|fetch|list)(/|$)") {
            Write-Host "  [WARN] Path contains action verb: $pathItem (REST best practice: use HTTP methods with noun resources)" -ForegroundColor Yellow
            $warnings++
        }
    }
}

# 5. Check response status codes
$codeMatches = [regex]::Matches($content, "(?m)^\s+['`"]?([0-9]{3})['`"]?:\s*$")
foreach ($m in $codeMatches) {
    $code = [int]$m.Groups[1].Value
    if ($code -lt 100 -or $code -gt 599) {
        Write-Host "  [WARN] Non-standard HTTP status code found: $code" -ForegroundColor Yellow
        $warnings++
    }
}

Write-Host "API validation complete: $errors error(s), $warnings warning(s)." -ForegroundColor $(if ($errors -eq 0) { "Green" } else { "Red" })

if ($errors -gt 0) {
    exit 1
}
exit 0

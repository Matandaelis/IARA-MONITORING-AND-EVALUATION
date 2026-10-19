#!/usr/bin/env bash
# validate-api.sh - Validate API specification (OpenAPI / Swagger YAML or JSON)
# Usage: ./validate-api.sh <openapi_spec>

set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: $0 <openapi_spec>"
    exit 1
fi

SPEC_FILE="$1"

if [ ! -f "$SPEC_FILE" ]; then
    echo "Error: Specification file not found: $SPEC_FILE"
    exit 1
fi

echo "Validating API spec: $SPEC_FILE"
ERRORS=0
WARNINGS=0

# 1. Check for OpenAPI or Swagger specification declaration
if ! grep -Eq "^(openapi|swagger):" "$SPEC_FILE"; then
    echo "  [ERROR] Missing 'openapi:' or 'swagger:' root definition"
    ERRORS=$((ERRORS + 1))
else
    SPEC_VERSION=$(grep -E "^(openapi|swagger):" "$SPEC_FILE" | head -n 1 | tr -d ' "\r')
    echo "  [OK] Found specification: $SPEC_VERSION"
fi

# 2. Check for info section
if ! grep -Eq "^info:" "$SPEC_FILE"; then
    echo "  [ERROR] Missing 'info:' section"
    ERRORS=$((ERRORS + 1))
else
    echo "  [OK] Found 'info:' section"
fi

# 3. Check for paths section
if ! grep -Eq "^paths:" "$SPEC_FILE"; then
    echo "  [ERROR] Missing 'paths:' section"
    ERRORS=$((ERRORS + 1))
else
    echo "  [OK] Found 'paths:' section"
fi

# 4. Check for path naming conventions (no verbs in path, no trailing slashes)
# Extract path keys (indented or top-level under paths)
PATHS=$(grep -E "^[[:space:]]+/[a-zA-Z0-9_{}/-]+:" "$SPEC_FILE" | sed -e 's/^[[:space:]]*//' -e 's/:.*//' || true)

if [ -n "$PATHS" ]; then
    while IFS= read -r PATH_ITEM; do
        [ -z "$PATH_ITEM" ] && continue
        
        # Check trailing slash
        if [[ "$PATH_ITEM" =~ .+/$ ]]; then
            echo "  [WARN] Path has trailing slash: $PATH_ITEM (REST best practice: avoid trailing slashes)"
            WARNINGS=$((WARNINGS + 1))
        fi
        
        # Check for uppercase letters in static segments
        CLEAN_PATH=$(echo "$PATH_ITEM" | sed 's/{[^}]*}//g')
        if echo "$CLEAN_PATH" | grep -q "[A-Z]"; then
            echo "  [WARN] Path contains uppercase characters: $PATH_ITEM (REST best practice: use lowercase kebab-case or snake_case)"
            WARNINGS=$((WARNINGS + 1))
        fi
        
        # Check for CRUD verbs in path segments
        if echo "$CLEAN_PATH" | grep -Eq "/(get|set|create|delete|update|remove|add|fetch|list)(/|$)"; then
            echo "  [WARN] Path contains action verb: $PATH_ITEM (REST best practice: use HTTP methods with noun resources)"
            WARNINGS=$((WARNINGS + 1))
        fi
    done <<< "$PATHS"
fi

# 5. Check for valid HTTP status codes in responses
STATUS_CODES=$(grep -E "^[[:space:]]+(\"?[0-9]{3}\"?):" "$SPEC_FILE" | sed -e 's/^[[:space:]]*//' -e 's/:.*//' -e 's/"//g' || true)
if [ -n "$STATUS_CODES" ]; then
    while IFS= read -r CODE; do
        [ -z "$CODE" ] && continue
        if [[ ! "$CODE" =~ ^[1-5][0-9]{2}$ ]]; then
            echo "  [WARN] Non-standard HTTP status code found: $CODE"
            WARNINGS=$((WARNINGS + 1))
        fi
    done <<< "$STATUS_CODES"
fi

echo "API validation complete: $ERRORS error(s), $WARNINGS warning(s)."

if [ "$ERRORS" -gt 0 ]; then
    exit 1
fi
exit 0

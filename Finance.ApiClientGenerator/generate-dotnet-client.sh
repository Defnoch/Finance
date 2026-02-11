#!/bin/bash
# Genereer een .NET API client op basis van de OpenAPI/Swagger spec
# Vereist: nswag (npm install -g nswag)
# Gebruik: ./generate-dotnet-client.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SWAGGER_JSON="$SCRIPT_DIR/swagger.json"
OUTPUT_DIR="$SCRIPT_DIR/GeneratedClients"
OUTPUT_FILE="$OUTPUT_DIR/ApiClient.cs"

mkdir -p "$OUTPUT_DIR"

nswag openapi2csclient /input:$SWAGGER_JSON /output:$OUTPUT_FILE \
  /namespace:Finance.GeneratedApiClient \
  /className:FinanceApiClient \
  /GenerateClientClasses:true \
  /GenerateDtoTypes:true \
  /UseHttpClientCreationMethod:false \
  /InjectHttpClient:false \
  /UseBaseUrl:true

echo "C# API client gegenereerd in $OUTPUT_FILE"

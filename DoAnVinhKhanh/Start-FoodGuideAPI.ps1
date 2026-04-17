$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "DAVK.API\VinhKhanhmain\FoodGuideAPI.csproj"

Write-Host "Starting FoodGuideAPI on http://localhost:5074 ..."
Write-Host "Health check: http://localhost:5074/api/health"
Write-Host "POI API:      http://localhost:5074/api/POIs"
Write-Host ""

dotnet run --project $project --launch-profile http

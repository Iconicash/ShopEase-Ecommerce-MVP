$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$env:ASPNETCORE_ENVIRONMENT = 'Development'

$toolList = dotnet tool list --global
if (-not ($toolList | Select-String -Pattern '^dotnet-ef\s')) {
    dotnet tool install --global dotnet-ef --version 10.0.0
}

dotnet restore .\ShopEase.slnx
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

$migrationDir = '.\src\backend\ShopEase.Infrastructure\Data\Migrations'
$existingMigration = Get-ChildItem $migrationDir -Filter '*_AddCommerceModules.cs' -ErrorAction SilentlyContinue
if (-not $existingMigration) {
    dotnet ef migrations add AddCommerceModules --project .\src\backend\ShopEase.Infrastructure\ShopEase.Infrastructure.csproj --startup-project .\src\backend\ShopEase.Api\ShopEase.Api.csproj --output-dir Data\Migrations
    if ($LASTEXITCODE -ne 0) { throw 'Creating the AddCommerceModules migration failed.' }
}

dotnet ef database update --project .\src\backend\ShopEase.Infrastructure\ShopEase.Infrastructure.csproj --startup-project .\src\backend\ShopEase.Api\ShopEase.Api.csproj
if ($LASTEXITCODE -ne 0) { throw 'Updating the ShopEaseDB database failed.' }

Write-Host 'ShopEase database migration completed.' -ForegroundColor Green

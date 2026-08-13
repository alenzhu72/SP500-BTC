$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SP500Widget.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
Write-Host "`n完成：bin\Release\net8.0-windows\win-x64\publish\MarketFloat.exe"

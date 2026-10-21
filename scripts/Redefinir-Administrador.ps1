$ErrorActionPreference = 'Stop'
$projetoApi = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\OficinaMecanicaFase1.Api\OficinaMecanicaFase1.Api.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET não encontrado.'
}

$ambienteAnterior = $env:OFICINA_REDEFINIR_ADMIN
$ambienteAspNetAnterior = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:OFICINA_REDEFINIR_ADMIN = '1'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet run --project $projetoApi --no-launch-profile
    if ($LASTEXITCODE -ne 0) {
        throw 'Não foi possível redefinir a senha do Administrador.'
    }
} finally {
    if ($null -eq $ambienteAnterior) { Remove-Item Env:OFICINA_REDEFINIR_ADMIN -ErrorAction SilentlyContinue }
    else { $env:OFICINA_REDEFINIR_ADMIN = $ambienteAnterior }
    if ($null -eq $ambienteAspNetAnterior) { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }
    else { $env:ASPNETCORE_ENVIRONMENT = $ambienteAspNetAnterior }
}

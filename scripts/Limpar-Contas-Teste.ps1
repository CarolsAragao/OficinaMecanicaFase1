$ErrorActionPreference = 'Stop'
$projetoApi = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\OficinaMecanicaFase1.Api\OficinaMecanicaFase1.Api.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET não encontrado.'
}

$limpezaAnterior = $env:OFICINA_LIMPAR_USUARIOS_TESTE
$ambienteAspNetAnterior = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:OFICINA_LIMPAR_USUARIOS_TESTE = '1'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet run --project $projetoApi --no-launch-profile
    if ($LASTEXITCODE -ne 0) {
        throw 'A limpeza das contas de teste falhou. Leia a mensagem de diagnóstico acima.'
    }
} finally {
    if ($null -eq $limpezaAnterior) { Remove-Item Env:OFICINA_LIMPAR_USUARIOS_TESTE -ErrorAction SilentlyContinue }
    else { $env:OFICINA_LIMPAR_USUARIOS_TESTE = $limpezaAnterior }
    if ($null -eq $ambienteAspNetAnterior) { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }
    else { $env:ASPNETCORE_ENVIRONMENT = $ambienteAspNetAnterior }
}

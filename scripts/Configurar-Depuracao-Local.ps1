$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$arquivoEnv = Join-Path $raiz '.env'
$projetoApi = Join-Path $raiz 'src\OficinaMecanicaFase1.Api\OficinaMecanicaFase1.Api.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET não encontrado.'
}

if (Test-Path -LiteralPath $arquivoEnv) {
    $linhaSenha = Get-Content -LiteralPath $arquivoEnv | Where-Object { $_ -match '^MSSQL_SA_PASSWORD=' } | Select-Object -Last 1
    if (-not $linhaSenha) {
        throw 'O arquivo .env não contém MSSQL_SA_PASSWORD.'
    }
    $senha = $linhaSenha.Substring('MSSQL_SA_PASSWORD='.Length)
} else {
    $senhaProtegida = Read-Host 'Senha para o SQL Server local' -AsSecureString
    $ponteiro = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($senhaProtegida)
    try {
        $senha = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ponteiro)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ponteiro)
    }
    if ($senha.Length -lt 8 -or $senha -match '[\r\n\$#"'';\s]') {
        throw 'Use ao menos 8 caracteres, sem espaços, $, #, aspas ou ponto e vírgula.'
    }
    [System.IO.File]::WriteAllText($arquivoEnv, "MSSQL_SA_PASSWORD=$senha$([Environment]::NewLine)", [System.Text.UTF8Encoding]::new($false))
}

if ([string]::IsNullOrWhiteSpace($senha) -or $senha -match '[\r\n;"'']') {
    throw 'A senha existente no .env não pode ficar vazia nem conter aspas, ponto e vírgula ou quebras de linha.'
}

$conexao = "Server=localhost,1433;Database=OficinaMecanica;User Id=sa;Password=$senha;Encrypt=True;TrustServerCertificate=True"
& dotnet user-secrets set 'ConnectionStrings:Oficina' $conexao --project $projetoApi
if ($LASTEXITCODE -ne 0) {
    throw 'Não foi possível salvar a conexão nos User Secrets da API.'
}

Write-Host 'Conexão local configurada nos User Secrets da API.'

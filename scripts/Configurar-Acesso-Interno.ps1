$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$arquivoEnv = Join-Path $raiz '.env'
$projetoApi = Join-Path $raiz 'src\OficinaMecanicaFase1.Api\OficinaMecanicaFase1.Api.csproj'

if (-not (Test-Path -LiteralPath $arquivoEnv)) {
    throw 'Configure primeiro a senha do SQL Server no arquivo .env.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'SDK .NET 10 não encontrado.'
}

$conteudo = [System.IO.File]::ReadAllText($arquivoEnv)
$conteudo = $conteudo.TrimEnd([char[]]"`r`n") + [Environment]::NewLine
$valores = @{}
foreach ($linha in ($conteudo -split "`r?`n")) {
    if ($linha -match '^([A-Z_]+)=(.*)$') {
        $valores[$matches[1]] = $matches[2]
    }
}

if (-not $valores.ContainsKey('MSSQL_SA_PASSWORD') -or
    [string]::IsNullOrWhiteSpace($valores['MSSQL_SA_PASSWORD'])) {
    throw 'Defina uma senha forte e própria em MSSQL_SA_PASSWORD no arquivo .env.'
}
if ($valores['MSSQL_SA_PASSWORD'] -eq 'SenhaForte123!') {
    throw 'Substitua a senha de exemplo por uma senha própria antes de prosseguir.'
}

if (-not $valores.ContainsKey('JWT_SIGNING_KEY')) {
    $bytes = New-Object byte[] 32
    $gerador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $gerador.GetBytes($bytes)
    }
    finally {
        $gerador.Dispose()
    }
    $valores['JWT_SIGNING_KEY'] = [Convert]::ToBase64String($bytes)
    $conteudo = $conteudo.TrimEnd() + [Environment]::NewLine + 'JWT_SIGNING_KEY=' + $valores['JWT_SIGNING_KEY'] + [Environment]::NewLine
}

if (-not $valores.ContainsKey('BOOTSTRAP_ADMIN_EMAIL')) {
    $valores['BOOTSTRAP_ADMIN_EMAIL'] = Read-Host 'E-mail do primeiro administrador'
    $conteudo += 'BOOTSTRAP_ADMIN_EMAIL=' + $valores['BOOTSTRAP_ADMIN_EMAIL'] + [Environment]::NewLine
}

if (-not $valores.ContainsKey('BOOTSTRAP_ADMIN_PASSWORD')) {
    $senhaProtegida = Read-Host 'Senha do primeiro administrador (mínimo 12 caracteres, letras maiúsculas e minúsculas, número e símbolo)' -AsSecureString
    $ponteiro = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($senhaProtegida)
    try {
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ponteiro)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ponteiro)
    }

    if ($valores['BOOTSTRAP_ADMIN_PASSWORD'].Length -lt 12 -or
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] -cnotmatch '[a-z]' -or
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] -cnotmatch '[A-Z]' -or
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] -notmatch '[0-9]' -or
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] -notmatch '[^a-zA-Z0-9]' -or
        $valores['BOOTSTRAP_ADMIN_PASSWORD'] -match '[\r\n\$#"'']') {
        throw 'A senha deve atender às regras do Identity e não pode conter $, #, aspas ou quebras de linha no arquivo .env.'
    }

    $conteudo += 'BOOTSTRAP_ADMIN_PASSWORD=' + $valores['BOOTSTRAP_ADMIN_PASSWORD'] + [Environment]::NewLine
}

foreach ($nome in @('JWT_SIGNING_KEY', 'BOOTSTRAP_ADMIN_EMAIL', 'BOOTSTRAP_ADMIN_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace($valores[$nome])) {
        throw "O valor de $nome no .env está vazio. Corrija esse arquivo antes de prosseguir."
    }
}

try {
    $chaveValida = [Convert]::FromBase64String($valores['JWT_SIGNING_KEY'])
}
catch {
    throw 'JWT_SIGNING_KEY precisa ser uma chave em Base64. Remova a linha inválida do .env e execute novamente.'
}

if ($chaveValida.Length -lt 32) {
    throw 'JWT_SIGNING_KEY precisa conter ao menos 32 bytes aleatórios.'
}

[System.IO.File]::WriteAllText($arquivoEnv, $conteudo, [System.Text.UTF8Encoding]::new($false))

$segredos = @{
    'Jwt:SigningKey' = $valores['JWT_SIGNING_KEY']
    'Bootstrap:AdminEmail' = $valores['BOOTSTRAP_ADMIN_EMAIL']
    'Bootstrap:AdminPassword' = $valores['BOOTSTRAP_ADMIN_PASSWORD']
}

foreach ($nome in $segredos.Keys) {
    & dotnet user-secrets set $nome $segredos[$nome] --project $projetoApi
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível gravar $nome nos User Secrets da API."
    }
}

Write-Host 'Acesso interno configurado no .env local e nos User Secrets da API.'

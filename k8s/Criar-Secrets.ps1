# Execute na raiz do repositório com PowerShell 7.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

foreach ($nome in 'MSSQL_SA_PASSWORD', 'JWT_SIGNING_KEY', 'BOOTSTRAP_ADMIN_EMAIL', 'BOOTSTRAP_ADMIN_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($nome))) {
        throw "Defina a variável $nome antes de criar os Secrets."
    }
}

# O construtor preserva caracteres especiais da senha na string de conexão.
$conexao = [System.Data.Common.DbConnectionStringBuilder]::new()
$conexao['Server'] = 'sqlserver,1433'
$conexao['Database'] = 'OficinaMecanica'
$conexao['User Id'] = 'sa'
$conexao['Password'] = $env:MSSQL_SA_PASSWORD
$conexao['Encrypt'] = 'True'
$conexao['TrustServerCertificate'] = 'True'

# Envia os valores ao kubectl pela entrada padrão, sem gravar arquivos com senhas.
function Set-OficinaSecret([string] $Nome, [hashtable] $Valores) {
    $secret = @{
        apiVersion = 'v1'
        kind = 'Secret'
        type = 'Opaque'
        metadata = @{ name = $Nome }
        data = @{}
    }
    foreach ($chave in $Valores.Keys) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Valores[$chave])
        $secret.data[$chave] = [Convert]::ToBase64String($bytes)
    }
    $secret | ConvertTo-Json -Depth 4 | kubectl --context kind-oficina -n default apply --server-side -f -
}

Set-OficinaSecret 'sqlserver-secret' @{ MSSQL_SA_PASSWORD = $env:MSSQL_SA_PASSWORD }
Set-OficinaSecret 'oficina-api-secret' @{
    ConnectionStrings__Oficina = $conexao.ConnectionString
    Jwt__SigningKey = $env:JWT_SIGNING_KEY
    Bootstrap__AdminEmail = $env:BOOTSTRAP_ADMIN_EMAIL
    Bootstrap__AdminPassword = $env:BOOTSTRAP_ADMIN_PASSWORD
}

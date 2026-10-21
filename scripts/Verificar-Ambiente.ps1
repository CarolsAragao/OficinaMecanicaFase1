$falhas = 0

function Resultado([string]$tipo, [string]$texto) {
    Write-Host "[$tipo] $texto"
    if ($tipo -eq 'FALHA') { $script:falhas++ }
}

function Existe([string]$nome) {
    return [bool](Get-Command $nome -ErrorAction SilentlyContinue)
}

Resultado 'INFO' "Diretório atual: $(Get-Location)"
if (Existe 'git') {
    Resultado 'OK' ((& git --version 2>&1 | Out-String).Trim())
} else {
    Resultado 'FALHA' 'Git não encontrado.'
}

if (Existe 'dotnet') {
    $sdks = @(& dotnet --list-sdks 2>&1)
    if ($LASTEXITCODE -eq 0 -and @($sdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) {
        Resultado 'OK' "SDK .NET 10 encontrado: $((@($sdks | Where-Object { $_ -match '^10\.' }) -join ', '))"
    } else {
        Resultado 'FALHA' 'SDK .NET 10 não foi encontrado em dotnet --list-sdks.'
    }
} else {
    Resultado 'FALHA' 'dotnet não encontrado.'
}

if (Existe 'docker') {
    $versao = (& docker --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0) { Resultado 'OK' $versao }
    else { Resultado 'FALHA' 'Docker CLI não respondeu.' }

    $compose = (& docker compose version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0) { Resultado 'OK' "Compose: $compose" }
    else { Resultado 'FALHA' 'Docker Compose não está disponível.' }

    $info = (& docker info --format '{{.OSType}}|{{.Architecture}}|{{.MemTotal}}' 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        Resultado 'FALHA' 'Docker Engine não respondeu; abra o Docker Desktop e repita a verificação.'
    } else {
        $partes = $info -split '\|'
        if ($partes.Count -ne 3) {
            Resultado 'FALHA' "Não foi possível interpretar o ambiente Docker: $info"
        } else {
            $sistema = $partes[0].Trim().ToLowerInvariant()
            $arquitetura = $partes[1].Trim().ToLowerInvariant()
            [long]$memoria = 0
            $memoriaValida = [long]::TryParse($partes[2].Trim(), [ref]$memoria)
            if ($sistema -eq 'linux') { Resultado 'OK' 'Docker Engine em modo Linux.' }
            else { Resultado 'FALHA' "Docker Engine em modo $sistema; selecione contêineres Linux." }
            if ($arquitetura -in @('x86_64', 'amd64')) {
                Resultado 'OK' "Arquitetura do Docker Engine: $arquitetura."
            } else {
                Resultado 'FALHA' "Arquitetura do Docker Engine: $arquitetura; verificar compatibilidade com SQL Server 2022." 
            }
            if ($memoriaValida -and $memoria -ge 2GB) {
                Resultado 'OK' "Memória informada pelo Docker Engine: $([math]::Round($memoria / 1GB, 1)) GB."
            } else {
                Resultado 'FALHA' 'Docker Engine informou menos de 2 GB de memória, ou valor não pôde ser lido.'
            }
        }
    }
} else {
    Resultado 'FALHA' 'Docker não encontrado.'
}

if (Test-Path '.git') {
    $origin = (& git remote get-url origin 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0) {
        Resultado 'OK' 'Repositório local possui origin configurado (URL omitida).'
        Resultado 'INFO' 'Confirme separadamente que o repositório é acessível ao avaliador pela URL informada.'
    } else {
        Resultado 'INFO' 'Repositório local ainda não possui origin.'
    }
} else {
    Resultado 'INFO' 'Esta pasta ainda não foi inicializada como repositório Git.'
}

try {
    $raiz = [System.IO.Path]::GetPathRoot((Get-Location).Path)
    $unidade = New-Object System.IO.DriveInfo($raiz)
    if ($unidade.AvailableFreeSpace -ge 2GB) {
        Resultado 'OK' "Espaço livre na unidade do projeto: $([math]::Round($unidade.AvailableFreeSpace / 1GB, 1)) GB."
    } else {
        Resultado 'FALHA' 'Menos de 2 GB livres na unidade do projeto.'
    }
    Resultado 'INFO' 'Se o Docker usa outra unidade para imagens, confira também o espaço livre nessa unidade.'
} catch {
    Resultado 'INFO' 'Não foi possível verificar o espaço livre do disco. Confira manualmente ao menos 2 GB.'
}

if ($falhas -gt 0) {
    Resultado 'INFO' "Verificação concluída com $falhas falha(s). Corrija os requisitos do ambiente e execute novamente."
    exit 1
}
Resultado 'INFO' 'Ferramentas e modo Docker verificados. O acesso ao repositório ainda exige verificação separada.'
exit 0

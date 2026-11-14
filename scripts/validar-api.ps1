param(
    [string]$BaseUrl = 'http://localhost:5078'
)
$ErrorActionPreference = 'Stop'

function Get-HeatSafeToken([string]$Username, [string]$Password) {
    $body = @{ username = $Username; password = $Password } | ConvertTo-Json
    $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/token" -ContentType 'application/json' -Body $body
    return $response.accessToken
}

function Invoke-HeatSafeJson([string]$Method, [string]$Path, [string]$Token, $Body = $null) {
    $headers = @{ Authorization = "Bearer $Token" }
    if ($null -ne $Body) {
        return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 10)
    }
    return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers
}

Write-Host 'Verificando API...' -ForegroundColor Cyan
$health = Invoke-RestMethod -Uri "$BaseUrl/health"
if ($health.status -ne 'healthy') { throw 'Health check não retornou healthy.' }
Write-Host 'Health check: OK' -ForegroundColor Green

$deviceToken = Get-HeatSafeToken 'dispositivo' 'Device@123'
$now = [DateTimeOffset]::Now.ToString('O')
$reading = @{
    dispositivoId = 'HS-001'
    temperatura = 35.2
    umidade = 60
    indiceCalor = 42.1
    nivel = 'PERIGO'
    movimento = $false
    dataHora = $now
}
$readingResult = Invoke-HeatSafeJson 'Post' '/leituras' $deviceToken $reading
Write-Host "Leitura registrada: id=$($readingResult.id), índice servidor=$($readingResult.indiceCalorCalculado) °C, nível=$($readingResult.nivel)" -ForegroundColor Green

$event = @{ dispositivoId = 'HS-001'; tipo = 'ALARME_SEM_RESPOSTA'; dataHora = [DateTimeOffset]::Now.ToString('O') }
$null = Invoke-HeatSafeJson 'Post' '/eventos' $deviceToken $event
Write-Host 'Evento ALARME_SEM_RESPOSTA registrado.' -ForegroundColor Green

$familyToken = Get-HeatSafeToken 'familiar' 'Familia@123'
$alerts = Invoke-HeatSafeJson 'Get' '/alertas/ativos' $familyToken
if (-not $alerts -or $alerts.Count -eq 0) { throw 'Nenhum alerta ativo retornado após o evento de demonstração.' }
$alert = $alerts | Select-Object -First 1
Write-Host "Alerta localizado: id=$($alert.id), status=$($alert.status)" -ForegroundColor Green

if ($alert.status -ne 'CONFIRMADO' -and $alert.status -ne 'RESOLVIDO') {
    $confirmation = @{ familiarId = 2; observacao = 'Confirmação executada pelo roteiro local de validação.' }
    Invoke-HeatSafeJson 'Post' "/alertas/$($alert.id)/confirmar" $familyToken $confirmation | Out-Null
    Write-Host 'Confirmação familiar: OK' -ForegroundColor Green
}

$adminToken = Get-HeatSafeToken 'admin' 'Admin@123'
$visits = Invoke-HeatSafeJson 'Get' '/visitas/prioritarias' $adminToken
if (-not $visits -or $visits.Count -eq 0) { throw 'Nenhuma visita prioritária retornada após o escalonamento.' }
Write-Host "Visitas prioritárias: $($visits.Count). Maior prioridade: $($visits[0].pontuacao)." -ForegroundColor Green

Write-Host ''
Write-Host 'Validação HTTP concluída. A notificação externa é demonstrativa e aparece apenas nos logs da API.' -ForegroundColor Cyan

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5078'
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')

function Write-Step([string]$Message) {
    Write-Host "`n== $Message ==" -ForegroundColor Cyan
}
function Assert-Status([string]$Name, [bool]$Condition, [string]$Details = '') {
    if (-not $Condition) { throw "FALHOU: $Name. $Details" }
    Write-Host "OK: $Name" -ForegroundColor Green
}
function Get-DemoToken([string]$Username, [string]$Password) {
    $payload = @{ username = $Username; password = $Password } | ConvertTo-Json
    $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/token" -ContentType 'application/json' -Body $payload -TimeoutSec 20
    if ([string]::IsNullOrWhiteSpace($response.accessToken)) { throw "Token vazio para $Username." }
    return $response.accessToken
}
function Invoke-JsonApi([string]$Method, [string]$Path, [string]$Token, $Body = $null, [hashtable]$ExtraHeaders = @{}) {
    $headers = @{ Authorization = "Bearer $Token" }
    foreach ($key in $ExtraHeaders.Keys) { $headers[$key] = $ExtraHeaders[$key] }
    $args = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; TimeoutSec = 30 }
    if ($null -ne $Body) {
        $args.ContentType = 'application/json'
        $args.Body = $Body | ConvertTo-Json -Depth 10
    }
    return Invoke-RestMethod @args
}

Write-Host 'HEATSAFE - TESTE DE INTEGRACAO LOCAL' -ForegroundColor Yellow
Write-Host "Servidor: $BaseUrl"

Write-Step '1. Verificar disponibilidade'
$health = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health" -TimeoutSec 10
Assert-Status 'GET /health retorna healthy' ($health.status -eq 'healthy')

Write-Step '2. Autenticar perfis demonstrativos (Development)'
$adminToken = Get-DemoToken 'admin' 'Admin@123'
$familiarToken = Get-DemoToken 'familiar' 'Familia@123'
$agentToken = Get-DemoToken 'agente' 'Agente@123'
$deviceToken = Get-DemoToken 'dispositivo' 'Device@123'
Assert-Status 'Tokens emitidos para administrador, familiar, agente e dispositivo' $true

Write-Step '3. Consultar cadastros como administrador'
$headersAdmin = @{ Authorization = "Bearer $adminToken" }
$idosos = Invoke-RestMethod -Method Get -Uri "$BaseUrl/idosos" -Headers $headersAdmin -TimeoutSec 20
$residences = Invoke-RestMethod -Method Get -Uri "$BaseUrl/residencias" -Headers $headersAdmin -TimeoutSec 20
Assert-Status 'GET /idosos retorna o cadastro demonstrativo' (@($idosos).Count -ge 1)
Assert-Status 'GET /residencias retorna residência 1' (@($residences | Where-Object { $_.id -eq 1 }).Count -eq 1)

Write-Step '4. Enviar leitura fictícia de calor extremo pelo dispositivo HS-001'
$readingBody = @{
    dispositivoId = 'HS-001'
    temperatura = 35.2
    umidade = 80
    indiceCalor = 42.1
    nivel = 'PERIGO'
    movimento = $false
    dataHora = (Get-Date).ToUniversalTime().ToString('o')
}
$reading = Invoke-JsonApi 'Post' '/leituras' $deviceToken $readingBody
Assert-Status 'POST /leituras recalculou o índice e classificou PERIGO_EXTREMO' ($reading.nivel -eq 'PERIGO_EXTREMO') "Nível retornado: $($reading.nivel); índice calculado: $($reading.indiceCalorCalculado)"

Write-Step '5. Consultar status da residência e alertas como familiar'
$headersFamily = @{ Authorization = "Bearer $familiarToken" }
$status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/residencias/1/status" -Headers $headersFamily -TimeoutSec 20
$alerts = Invoke-RestMethod -Method Get -Uri "$BaseUrl/alertas/ativos" -Headers $headersFamily -TimeoutSec 20
$extremeAlert = @($alerts | Where-Object { $_.nivel -eq 'PERIGO_EXTREMO' } | Select-Object -First 1)
Assert-Status 'GET /residencias/1/status autorizado para familiar vinculado' ($null -ne $status)
Assert-Status 'PERIGO_EXTREMO criou alerta ativo ou escalonado' ($extremeAlert.Count -eq 1)
$alertId = [int]$extremeAlert[0].id

Write-Step '6. Verificar visita prioritária como agente da UBS'
$headersAgent = @{ Authorization = "Bearer $agentToken" }
$visits = Invoke-RestMethod -Method Get -Uri "$BaseUrl/visitas/prioritarias" -Headers $headersAgent -TimeoutSec 20
$visit = @($visits | Where-Object { $_.nivel -eq 'PERIGO_EXTREMO' } | Select-Object -First 1)
Assert-Status 'PERIGO_EXTREMO encaminhou uma visita prioritária' ($visit.Count -eq 1)
$visitId = [int]$visit[0].id

Write-Step '7. Confirmar alerta pela família'
$confirmBody = @{ familiarId = 2; observacao = 'Contato realizado durante teste local com dados fictícios.' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$BaseUrl/alertas/$alertId/confirmar" -Headers $headersFamily -ContentType 'application/json' -Body $confirmBody -TimeoutSec 20 | Out-Null
Write-Host 'OK: POST /alertas/{id}/confirmar retornou sucesso' -ForegroundColor Green

Write-Step '8. Atualizar o andamento da visita pela UBS'
$visitBody = @{ status = 'EM_ANDAMENTO' } | ConvertTo-Json
Invoke-RestMethod -Method Patch -Uri "$BaseUrl/visitas/$visitId/status" -Headers $headersAgent -ContentType 'application/json' -Body $visitBody -TimeoutSec 20 | Out-Null
Write-Host 'OK: PATCH /visitas/{id}/status retornou sucesso' -ForegroundColor Green

Write-Step '9. Testar eventos e idempotência'
$key = "hs-test-agua-$([guid]::NewGuid().ToString('N'))"
$eventBody = @{ dispositivoId = 'HS-001'; tipo = 'AGUA'; dataHora = (Get-Date).ToUniversalTime().ToString('o') }
$first = Invoke-JsonApi 'Post' '/eventos' $deviceToken $eventBody @{ 'Idempotency-Key' = $key }
$second = Invoke-JsonApi 'Post' '/eventos' $deviceToken $eventBody @{ 'Idempotency-Key' = $key }
Assert-Status 'Primeiro evento foi registrado' ($first.registrado -eq $true)
Assert-Status 'Reenvio idempotente foi ignorado' ($second.registrado -eq $false)

Write-Host "`nTODOS OS TESTES DE INTEGRACAO PASSARAM." -ForegroundColor Green
Write-Host "Leitura: $($reading.id) | Índice calculado: $($reading.indiceCalorCalculado) °C | Alerta: $alertId | Visita: $visitId"
Write-Host 'Os registros utilizados são fictícios e destinam-se somente à validação local.'

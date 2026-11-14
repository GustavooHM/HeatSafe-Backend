# HeatSafe — Contratos de Integração entre Componentes

Este documento descreve os contratos que o backend C# disponibiliza para os demais componentes da Global Solution. A base local de demonstração é `http://localhost:5078`.

> Use somente dados fictícios no ambiente acadêmico. O endpoint de token e as credenciais abaixo existem exclusivamente em `Development`; não são um mecanismo de identidade para produção.

## 1. Autenticação de desenvolvimento

`POST /auth/token`

Request:

```json
{
  "username": "dispositivo",
  "password": "Device@123"
}
```

Response (campos principais):

```json
{
  "accessToken": "<JWT>",
  "tokenType": "Bearer",
  "expiresAtUtc": "<data-hora-UTC>",
  "role": "Dispositivo"
}
```

Perfis locais pré-configurados:

| Usuário | Senha | Papel | Identidade | Finalidade |
|---|---|---|---|---|
| `admin` | `Admin@123` | Administrador | 99 | Cadastros e administração |
| `familiar` | `Familia@123` | Familiar | 2 | Consultar status e confirmar alertas vinculados |
| `agente` | `Agente@123` | AgenteSaude | 3 | Consultar e atualizar visitas |
| `dispositivo` | `Device@123` | Dispositivo | HS-001 | Enviar dados do dispositivo HS-001 |

Enviar o token no cabeçalho `Authorization: Bearer <JWT>`.

O token de dispositivo contém a claim `device_code`. O valor de `dispositivoId` no corpo deve corresponder a essa claim; um token do HS-001 não pode enviar dados como outro dispositivo.

## 2. ESP32 → API C#

### 2.1. Registrar leitura — `POST /leituras`

Papel: `Dispositivo`.

Request compatível com o contrato da equipe:

```json
{
  "dispositivoId": "HS-001",
  "temperatura": 35.2,
  "umidade": 60,
  "indiceCalor": 42.1,
  "nivel": "PERIGO",
  "movimento": false,
  "dataHora": "2026-11-12T14:00:00"
}
```

Campos:

| Campo | Tipo | Regra |
|---|---|---|
| `dispositivoId` | string | Código cadastrado e vinculado ao token |
| `temperatura` | number | Faixa funcional aceita pelo endpoint: -20 a 60 °C |
| `umidade` | number | 0 a 100% |
| `indiceCalor` | number/null | Índice informado pelo ESP32; armazenado para comparação |
| `nivel` | string/null | Classificação informada pelo ESP32; não é autoridade do servidor |
| `movimento` | boolean | Atualiza o último movimento quando `true` |
| `dataHora` | string | ISO 8601; sem offset é interpretado como hora de São Paulo |

O servidor recalcula o índice usando temperatura e umidade. O valor calculado pelo servidor define o nível oficial. A resposta contém `id`, `indiceCalorCalculado`, `indiceCalorInformado`, `nivel` e `divergencia`.

Exemplo de resposta ilustrativa:

```json
{
  "id": 12,
  "indiceCalorCalculado": 43.2,
  "indiceCalorInformado": 42.1,
  "nivel": "PERIGO",
  "divergencia": null
}
```

Os números de resposta acima são apenas ilustrativos; o resultado efetivo depende da temperatura e umidade enviadas.

### 2.2. Registrar evento — `POST /eventos`

Papel: `Dispositivo`.

Request:

```json
{
  "dispositivoId": "HS-001",
  "tipo": "AGUA",
  "dataHora": "2026-11-12T14:00:00"
}
```

Tipos aceitos:

- `AGUA`: registra hidratação e atualiza a última hidratação.
- `MOVIMENTO`: atualiza o horário da última movimentação.
- `ALARME_SEM_RESPOSTA`: registra a ocorrência, notifica a família e encaminha imediatamente a ocorrência à lista da UBS.

Para reenvios após falha de rede, envie um identificador estável no cabeçalho `Idempotency-Key`. A API ignora uma repetição com a mesma chave para o mesmo dispositivo.

Response:

```json
{
  "registrado": true,
  "mensagem": "Evento registrado."
}
```

Em uma repetição idempotente, `registrado` será `false`.

## 3. Aplicativo → API C#

### 3.1. Status da residência — `GET /residencias/{id}/status`

Papéis: Familiar, Idoso, AgenteSaude ou Administrador, com verificação de vínculo para Familiar e Idoso.

Resposta:

```json
{
  "indiceCalor": 42.1,
  "nivel": "PERIGO",
  "ultimoMovimento": "2026-11-12T12:00:00Z",
  "ultimaAgua": "2026-11-12T11:00:00Z",
  "alertaAtivo": true,
  "modoVigilancia": false,
  "vigilanciaAteUtc": null
}
```

### 3.2. Alertas ativos — `GET /alertas/ativos`

Papel: Familiar. Retorna somente os alertas associados aos idosos vinculados à identidade do token.

### 3.3. Confirmar alerta — `POST /alertas/{id}/confirmar`

Papel: Familiar.

Request:

```json
{
  "familiarId": 2,
  "observacao": "Contato realizado durante a demonstração."
}
```

O `familiarId` precisa corresponder à identidade do token e o familiar precisa estar vinculado ao idoso do alerta. Resposta de sucesso: `204 No Content`.

### 3.4. Visitas prioritárias — `GET /visitas/prioritarias`

Papéis: AgenteSaude ou Administrador. O agente visualiza visitas atribuídas a ele ou ainda sem agente atribuído. A ordenação é decrescente pela pontuação.

Resposta ilustrativa:

```json
[
  {
    "id": 4,
    "idoso": "Dona Maria (demonstração)",
    "endereco": "Residência demonstrativa 001 (endereço fictício)",
    "nivel": "PERIGO",
    "pontuacao": 92.5,
    "motivo": "Índice de calor elevado e alerta não confirmado.",
    "criadaEmUtc": "2026-11-12T14:20:00Z",
    "status": "PENDENTE"
  }
]
```

### 3.5. Atualizar visita — `PATCH /visitas/{id}/status`

Papel: AgenteSaude ou Administrador.

Request:

```json
{
  "status": "EM_ANDAMENTO"
}
```

Valores do status: `PENDENTE`, `EM_ANDAMENTO`, `CONCLUIDA`, `CANCELADA`.

## 4. Serviço Python/FastAPI → API C#

A comunicação é iniciada pelo backend C#; o serviço Python não precisa chamar a API C# para a consulta diária.

A API C# consulta:

`GET {Forecast:BaseUrl}/previsao/amanha?estacao=A701`

Resposta esperada do serviço Python:

```json
{
  "data": "2026-11-13",
  "perigoso": true,
  "probabilidade": 0.87
}
```

Configuração no `appsettings.json`:

```json
{
  "Forecast": {
    "BaseUrl": "http://localhost:8000/",
    "Station": "A701",
    "TimeoutSeconds": 5
  }
}
```

A API registra a previsão válida e ativa o modo vigilância para o período previsto. Em falha externa, não cria previsão fictícia e não altera uma vigilância anterior com base em ausência de resposta.

## 5. Classificação de risco usada pela API

| Índice de calor calculado no servidor | Nível |
|---|---|
| abaixo de 27 °C | `NORMAL` |
| 27 °C até abaixo de 32 °C | `ATENCAO` |
| 32 °C até abaixo de 41 °C | `CAUTELA_EXTREMA` |
| 41 °C até 54 °C | `PERIGO` |
| acima de 54 °C | `PERIGO_EXTREMO` |

A API segue a tabela funcional HeatSafe, mantendo exatamente 54 °C em `PERIGO`.

## 6. Datas e fusos

Todos os horários persistidos são normalizados para UTC. Por compatibilidade com o contrato original, um `dataHora` ISO 8601 sem `Z` ou offset é interpretado como horário local de São Paulo. Novos clientes devem preferir enviar offset explícito, por exemplo `2026-11-12T14:00:00-03:00` ou UTC com `Z`.

## 7. Códigos HTTP mais relevantes

| Status | Significado |
|---|---|
| 200 | Consulta ou operação concluída |
| 201 | Recurso criado, como uma leitura |
| 204 | Atualização/confirmacão sem corpo |
| 400 | Dados inválidos |
| 401 | Token ausente, inválido ou expirado |
| 403 | Papel ou vínculo não autorizado |
| 404 | Recurso/dispositivo não encontrado |
| 409 | Conflito de estado ou cadastro duplicado |
| 500 | Erro interno não previsto |

## 8. Endpoints administrativos

Todos exigem papel `Administrador`:

- `GET/POST /idosos`; `PUT/DELETE /idosos/{id}`.
- `GET/POST /familiares`; `PUT/DELETE /familiares/{id}`.
- `GET/POST /agentes-saude`; `PUT/DELETE /agentes-saude/{id}`.
- `GET/POST /residencias`; `PUT/DELETE /residencias/{id}`.
- `GET/POST /dispositivos`; `PUT/DELETE /dispositivos/{id}`.

Os endpoints DELETE realizam desativação lógica para preservar o histórico.

## 9. Endpoints de suporte

- `GET /health`: verifica disponibilidade da API, sem autenticação.
- `GET /swagger`: documentação interativa no ambiente Development.

## 10. Limites do protótipo

- A API não envia SMS, WhatsApp ou push real; usa adaptador de notificação demonstrativo via log.
- A previsão efetiva depende da disponibilidade do serviço Python.
- O ESP32 e o aplicativo são implementações de outras disciplinas.
- A pontuação de prioridade é uma heurística acadêmica, não substitui avaliação de profissionais de saúde.
- A autenticação de usuário demonstrativa só é habilitada em Development. Antes de produção, é necessária uma solução real de identidade, rotação de segredos, autenticação forte do dispositivo, TLS e avaliação LGPD.

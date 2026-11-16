# HeatSafe — Backend C#/.NET

| | |
|---|---|
| **Instituição** | FIAP · Engenharia de Software · 3ESPG |
| **Disciplina** | C# Software Development |
| **Atividade** | Global Solution 2026.2 — CitizenTech |

## Integrantes

| Nome | RM |
|---|---|
| Felipe Braunstein e Silva | RM554483 |
| Felipe do Nascimento Fernandes | RM554598 |
| Henrique Ignacio Bartalo | RM555274 |
| Gustavo Henrique Martins | RM556956 |

## Ficha da CitizenTech

| Campo | Resposta |
|---|---|
| **Nome** | HeatSafe |
| **Cidade e bairro** | São Paulo, Itaim Paulista (zona leste): casas térreas com telha de fibrocimento |
| **Cidadão beneficiado** | Idosos que moram ou passam o dia sozinhos em casas quentes; familiares e agentes comunitários de saúde da UBS |
| **Problema, em uma frase** | Nos dias de calor extremo, idosos que passam o dia sozinhos em casas quentes podem passar mal sem perceber o risco, e ninguém fica sabendo a tempo. |
| **Como funciona** | Um ESP32 dentro da casa mede o calor e o movimento; **esta API** recalcula o índice de calor, classifica o risco, avisa a família e, sem confirmação em 20 minutos, coloca a casa na lista de visitas da UBS. Na véspera de um dia perigoso, consulta o serviço de previsão e liga o modo vigilância. |
| **Indicador de impacto** | Tempo entre o alerta e a confirmação da família · % de alertas confirmados em até 30 min · visitas priorizadas pela UBS |
| **ODS** | 3 (Saúde) · 10 (Redução das desigualdades) · 11 (Cidades sustentáveis) · 13 (Ação climática) |

## Descrição

Backend da CitizenTech HeatSafe, projeto acadêmico da Global Solution FIAP 2026.2.

O sistema recebe leituras ambientais de um ESP32, recalcula o índice de calor no servidor, classifica risco, cria alertas, permite confirmação por familiares, encaminha ocorrências não atendidas à lista de visitas da UBS e consulta a previsão climática fornecida pelo serviço de clima do grupo (HeatSafe Clima API, Java/Spring Boot).

> **Aviso acadêmico:** este protótipo não é dispositivo médico, não realiza diagnóstico e não substitui avaliação profissional. A pontuação de visita é uma heurística demonstrativa, não uma decisão clínica.

## Requisitos

- .NET SDK 10 (qualquer versão 10.0.x; o `global.json` aceita a partir da 10.0.100).
- Acesso à internet na primeira restauração dos pacotes NuGet.
- Windows, Linux ou macOS.
- SQLite incluído pelo pacote do EF Core; não é necessário instalar um servidor de banco.

## Estrutura

```text
HeatSafe/
├── HeatSafe.sln
├── HeatSafe.Api/
│   ├── BackgroundServices/
│   ├── Configuration/
│   ├── Controllers/
│   ├── Data/
│   │   └── Migrations/
│   ├── Domain/
│   ├── DTOs/
│   ├── ExternalServices/
│   ├── Security/
│   ├── Services/
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── HeatSafe.Tests/
├── HeatSafe.Api/Repositories/
├── scripts/
├── TESTAR-HEATSAFE.bat
├── INICIAR-HEATSAFE.bat
├── LEIA-ME-PRIMEIRO.txt
├── README.md
├── prompts.md
└── .gitignore
```

## Executar no Windows

1. Extraia o ZIP em uma pasta própria, por exemplo `C:\Projetos\HeatSafe-Backend`.
2. Abra a pasta no VS Code.
3. Dê dois cliques em `TESTAR-HEATSAFE.bat`. Ele executa restore, build e testes e para se algum comando falhar.
4. Depois de obter sucesso, dê dois cliques em `INICIAR-HEATSAFE.bat`.
5. Abra `http://localhost:5078/swagger`.

Os arquivos `.bat` procuram primeiro o SDK instalado em `%USERPROFILE%\.dotnet\dotnet.exe` e depois tentam o comando `dotnet` do PATH. Também existem versões PowerShell em `scripts/`.

### Executar no macOS ou Linux

```bash
dotnet test HeatSafe.sln                                    # restore, build e testes
dotnet run --project HeatSafe.Api --launch-profile HeatSafe.Api
```

O perfil de desenvolvimento usa `http://localhost:5078`. Abra `http://localhost:5078/swagger`.

A API aplica a migration inicial e insere dados de demonstração no primeiro início. O arquivo local `heatsafe.db` será criado dentro da pasta de execução do projeto e é ignorado pelo Git.

## Autenticação de demonstração

O endpoint `POST /auth/token` só existe no ambiente `Development`. Use os seguintes usuários fictícios:

| Usuário | Senha | Papel | Uso |
|---|---|---|---|
| `admin` | `Admin@123` | Administrador | Cadastros e administração |
| `familiar` | `Familia@123` | Familiar | Consultar/confirmar alertas da pessoa vinculada |
| `agente` | `Agente@123` | AgenteSaude | Consultar e atualizar visitas |
| `idoso` | `Idoso@123` | Idoso | Consultar o status da própria residência |
| `dispositivo` | `Device@123` | Dispositivo | Enviar leituras e eventos do `HS-001` |

As contas ficam em `HeatSafe.Api/appsettings.Development.json`, carregado só no ambiente Development. Essas credenciais são exclusivamente locais e didáticas. Não devem ser usadas em produção. Em produção, configure identidade, segredos, rotação de chaves, TLS e autenticação de dispositivo adequados.

### Obter token

No Swagger, execute `POST /auth/token`:

```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

Copie `accessToken`, clique em **Authorize** e informe o token. No campo Bearer, cole somente o token (sem escrever `Bearer ` manualmente).

## Dados demonstrativos

O seed cria as personas fictícias da proposta com nomes identificados como demonstração, uma residência com endereço fictício, o dispositivo `HS-001`, uma leitura inicial e um vínculo familiar. Nenhum endereço, CPF, telefone ou dado real de saúde é utilizado.

## Endpoints principais

| Método | Rota | Papel | Descrição |
|---|---|---|---|
| POST | `/auth/token` | Development | Emite token demonstrativo |
| POST | `/leituras` | Dispositivo | Recebe leitura do ESP32 e recalcula o índice |
| POST | `/eventos` | Dispositivo | Recebe AGUA, MOVIMENTO ou ALARME_SEM_RESPOSTA |
| GET | `/residencias/{id}/status` | Familiar/Idoso/Agente/Admin | Consulta status autorizado |
| GET | `/alertas/ativos` | Familiar | Lista alertas de idosos vinculados |
| POST | `/alertas/{id}/confirmar` | Familiar | Confirma alerta próprio |
| GET | `/visitas/prioritarias` | Agente/Admin | Lista visitas ordenadas por prioridade |
| PATCH | `/visitas/{id}/status` | Agente/Admin | Atualiza andamento de visita |
| GET/POST/PUT/DELETE | `/idosos` | Administrador | CRUD/desativação de idosos |
| GET/POST/PUT/DELETE | `/familiares` | Administrador | CRUD/desativação de familiares e vínculos |
| GET/POST/PUT/DELETE | `/agentes-saude` | Administrador | CRUD/desativação de agentes |
| GET/POST/PUT/DELETE | `/residencias` | Administrador | CRUD/desativação de residências |
| GET/POST/PUT/DELETE | `/dispositivos` | Administrador | CRUD/desativação de dispositivos |
| GET | `/health` | Público | Verificação de disponibilidade |

## Contratos de integração

### Autenticação do dispositivo

As rotas `/leituras` e `/eventos` aceitam **o token JWT de dispositivo** (`POST /auth/token` com `dispositivo` / `Device@123`) **ou o cabeçalho `X-Api-Key`**, que é como o firmware do ESP32 envia. Em Development, a chave do `HS-001` é `heatsafe-demo-2026` (`DeviceAuth:ApiKeys` em `appsettings.Development.json`). A chave identifica o dispositivo: um `HS-001` não consegue enviar dados em nome de outro (403).

### POST /leituras

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

O backend mantém os valores enviados para auditoria, mas utiliza o valor recalculado no servidor para classificar risco. A divergência é retornada no campo `divergencia`.

### POST /eventos

```json
{
  "dispositivoId": "HS-001",
  "tipo": "AGUA",
  "dataHora": "2026-11-12T14:00:00"
}
```

Tipos válidos: `AGUA`, `MOVIMENTO`, `ALARME_SEM_RESPOSTA` e `LEMBRETE_AGUA` (o aparelho lembrou o idoso de beber água; só é registrado, não conta como água bebida). É possível enviar `Idempotency-Key` no header para evitar duplicidade em reenvios.

### POST /alertas/{id}/confirmar

```json
{
  "familiarId": 2,
  "observacao": "Liguei e confirmei o contato."
}
```

A identidade do token e o vínculo familiar são verificados no servidor.

## Regras funcionais de risco

| Índice de calor | Classificação |
|---|---|
| abaixo de 27 °C | NORMAL |
| 27 °C até abaixo de 32 °C | ATENCAO |
| 32 °C até abaixo de 41 °C | CAUTELA_EXTREMA |
| 41 °C até abaixo de 54 °C | PERIGO |
| 54 °C ou mais | PERIGO_EXTREMO |

Cautela extrema: alerta familiar quando há 3 horas ou mais sem movimento **ou** hidratação. Perigo: alerta familiar com 2 horas ou mais sem movimento e encaminhamento à UBS se não houver confirmação em 20 minutos. Perigo extremo: alerta imediato à família e UBS.

Cada limite pertence ao nível de cima (27,0 °C já é ATENCAO e 54,0 °C já é PERIGO_EXTREMO), a mesma tabela usada no ESP32, no app, no serviço de clima e nos casos de teste do Azure.

## Pontuação de prioridade

A pontuação inicial é uma heurística ponderada configurável por `PriorityWeights`:

- Calor: 40%.
- Inatividade: 25%.
- Hidratação: 15%.
- Vulnerabilidade/idade: 10%.
- Alerta familiar não confirmado: 10%.

O resultado varia de 0 a 100 e deve apoiar a organização de visitas, sem substituir avaliação profissional.

## Integração com o serviço de previsão (SOA)

O cliente tipado consulta `GET /previsao/amanha?estacao=A701` no endereço configurado em `Forecast:BaseUrl` (padrão `http://localhost:8080/`), onde roda a **HeatSafe Clima API** (repositório [GS-HeatSafe-SOA](https://github.com/3ESPG/GS-HeatSafe-SOA), Java/Spring Boot com dados reais do INMET). A resposta usa os campos `data`, `perigoso` e `probabilidade`. Falhas externas são registradas sem inventar previsões.

## Background services

- Monitoramento de risco e escalonamento: intervalo configurável, padrão 60 segundos.
- Consulta da previsão: após 19h no fuso de São Paulo. Se o serviço estiver indisponível, nenhuma previsão é presumida.

## Documentos de integração e entrega

Consulte também a pasta `docs/`:

- `CONTRATOS-INTEGRACAO.md`: contratos HTTP para ESP32, app e serviço de previsão.
- `MATRIZ-REQUISITOS-FIAP.md`: relação dos critérios acadêmicos com arquivos e evidências.
- `PLANO-INTEGRACAO-EQUIPE.md`: responsabilidades e ordem de integração das disciplinas.
- `ROTEIRO-DEMONSTRACAO.md`: sequência sugerida para demonstrar a API.

## Testes

O projeto de testes utiliza xUnit 2.9.3 com o adaptador `xunit.runner.visualstudio` 3.1.5 e `Microsoft.NET.Test.Sdk` 17.14.1. Essa combinação mantém a execução no VSTest tradicional e evita a dependência do Microsoft.Testing.Platform que provocou incompatibilidade com o `dotnet test` nesta configuração do .NET 10.


```powershell
dotnet test .\HeatSafe.Tests\HeatSafe.Tests.csproj
```

Os 27 testes cobrem as fronteiras de classificação (incluindo 27, 32, 41 e 54 °C), as entradas da calculadora, a ordenação relativa da pontuação e a validação da chave de API do dispositivo.

## Segurança e limitações

- JWT demonstrativo disponível somente em `Development`.
- Notificações atuais são registradas no log pelo adaptador demonstrativo; não há SMS, WhatsApp ou push real configurado.
- O ESP32 e o aplicativo são componentes externos; este repositório entrega somente o backend.
- O serviço de previsão (HeatSafe Clima API) precisa estar ativo para produzir previsões reais.
- Antes de produção, implementar gestão real de identidade, segredos, autenticação forte dos dispositivos, política de retenção, auditoria, TLS, observabilidade e revisão LGPD.

## ODS relacionados

- ODS 3 — Saúde e bem-estar.
- ODS 10 — Redução das desigualdades.
- ODS 11 — Cidades e comunidades sustentáveis.
- ODS 13 — Ação contra a mudança global do clima.

## Validação técnica

Validado em 16/11/2026 no macOS com .NET SDK 10.0.103:

- `dotnet build`: **0 avisos, 0 erros**.
- `dotnet test`: **27 testes aprovados** (fronteiras de risco, calculadora, pontuação de prioridade e chave de dispositivo).
- Teste de ponta a ponta com a API rodando: tokens dos 5 papéis, 401/403 de autorização, validações 400, leitura e eventos pelo token e pelo `X-Api-Key` do ESP32, alerta de perigo extremo escalonado para a UBS, confirmação pela família, atualização da visita pelo agente e consulta da previsão na HeatSafe Clima API (Java, porta 8080).

As saídas estão em [`docs/evidencias`](docs/evidencias).

## Corrigir modelo EF Core com migration pendente

Se a inicialização apresentar `PendingModelChangesWarning`, faça backup de `HeatSafe.Api/heatsafe.db` (caso exista) e execute `ATUALIZAR-MIGRACAO-HEATSAFE.bat`. O script cria uma migration real e atualiza o snapshot; não desative o aviso. Consulte `docs/CORRECAO-PENDING-MODEL-CHANGES.md` para revisar a migration antes de aplicá-la.

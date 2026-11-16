# HeatSafe — Matriz de Requisitos e Evidências da Disciplina C#

Esta matriz relaciona os critérios de avaliação informados no briefing com os componentes do backend. Ela indica onde procurar evidências e como demonstrar cada requisito. Não substitui a conferência do professor nem representa nota garantida.

## 1. Critérios da disciplina

| Critério | Peso informado | Evidência no projeto | Como demonstrar |
|---|---:|---|---|
| Programação Orientada a Objetos | 3,0 | `Domain/Entities.cs`, `Domain/Enums.cs`, `Domain/Interfaces.cs` | Mostrar `Pessoa` abstrata, `Idoso`, `Familiar`, `AgenteSaude`, relacionamentos e métodos de domínio/serviço |
| Web API | 2,5 | `Controllers/`, `DTOs/Contracts.cs`, `Program.cs` | Abrir Swagger e demonstrar leitura, evento, status, confirmação e visita |
| EF Core + LINQ | 1,5 | `Data/HeatSafeDbContext.cs`, `Data/Migrations/`, `Repositories/` | Mostrar DbContext, TPH, relacionamentos, migrations, filtros e projeções LINQ |
| Async/Await | 1,0 | Controllers, Services, Repositories, BackgroundServices | Apontar operações assíncronas, `CancellationToken`, `ToListAsync` e `SaveChangesAsync` |
| Qualidade do código | 2,0 | Separação em camadas, DI, ProblemDetails, validações, testes e README | Explicar responsabilidade de cada camada, injeção de dependência, logs e testes |

## 2. Funcionalidades HeatSafe

| Requisito do produto | Implementação principal | Evidência sugerida |
|---|---|---|
| Receber temperatura e umidade | `IntegrationController` + `DeviceIntegrationService` | Request `POST /leituras` e resposta com índice recalculado |
| Não confiar no índice do ESP32 | `CalculadoraIndiceCalor` + `AvaliadorRisco` | Enviar índice/nivel divergentes e mostrar a classificação do servidor |
| Detectar água e movimento | `POST /eventos` | Enviar `AGUA` e `MOVIMENTO`; consultar `/residencias/{id}/status` |
| Criar alertas | `RiskMonitoringService` | Enviar leitura de risco e verificar `/alertas/ativos` |
| Confirmar alerta | `AlertsController` | Confirmar com familiar vinculado; mostrar resposta 204 |
| Escalonar para UBS | `RiskMonitoringService` + `RiskMonitoringBackgroundService` | Demonstrar alerta extremo imediato; para perigo, usar cenário de tempo controlado/teste específico |
| Priorizar visitas | `PriorityScoringService` + `VisitsController` | Mostrar lista ordenada em `/visitas/prioritarias` |
| Consultar previsão diária | `PrevisaoClimaticaClient` + `ForecastBackgroundService` | Subir a HeatSafe Clima API (SOA) e observar consulta/log |
| Proteger dados por perfil | `[Authorize]` nos controllers e verificações de vínculo | Demonstrar familiar não autorizado recebendo 403 |

## 3. Testes automatizados presentes

O projeto inclui testes unitários iniciais para:

- Fronteiras da classificação NORMAL, ATENCAO, CAUTELA_EXTREMA, PERIGO e PERIGO_EXTREMO.
- Rejeição de valores não finitos pelo classificador.
- Cálculo do índice de calor em cenário quente/úmido.
- Rejeição de temperatura e umidade inválidas.
- Comparação entre pontuação de prioridade de cenário menos grave e mais grave.
- Validação de pesos da pontuação.

O roteiro `scripts/testar-integracao-api.ps1` verifica os fluxos HTTP principais quando a API já estiver iniciada.

## 4. Comandos de validação local

Na raiz `HeatSafe-Backend`:

```powershell
dotnet restore .\HeatSafe.sln
dotnet build .\HeatSafe.sln --no-restore
dotnet test --project .\HeatSafe.Tests\HeatSafe.Tests.csproj --no-build --configuration Debug
```

Em seguida, iniciar a API:

```powershell
.\INICIAR-HEATSAFE.bat
```

E executar a validação HTTP em outro terminal:

```powershell
.\scripts\testar-integracao-api.ps1
```

## 5. Evidências a guardar para a entrega

Guardar capturas de tela ou logs reais de:

1. `dotnet --list-sdks` mostrando SDK .NET 10.
2. `dotnet restore` concluído.
3. `dotnet build` com `Build succeeded` e zero erros.
4. `dotnet test` com resumo de testes aprovados.
5. Swagger aberto em `http://localhost:5078/swagger`.
6. `GET /health` retornando `healthy`.
7. `POST /leituras` retornando índice calculado pelo servidor.
8. `POST /eventos` com hidratação e movimento.
9. `GET /alertas/ativos` e confirmação familiar.
10. `GET /visitas/prioritarias` com lista ordenada.
11. Teste negativo de acesso sem vínculo (403).

Preencher os resultados somente depois de executar. Não inventar prints, tempos, percentuais ou quantidade de testes.

## 6. Limitações conhecidas que devem ser explicadas

- Este repositório implementa o backend C#; não inclui o firmware ESP32, o app Expo nem o modelo Python.
- O adaptador de notificações é demonstrativo e não entrega mensagens reais.
- A autenticação pré-configurada é exclusiva de Development.
- O ambiente que gerou a versão do pacote não possuía SDK .NET nem acesso ao NuGet; build/test precisam ser verificados no computador da equipe.
- A pontuação de visita é heurística e não constitui diagnóstico nem decisão clínica.

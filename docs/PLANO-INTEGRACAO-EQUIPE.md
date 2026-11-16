# HeatSafe — Plano de Integração entre as Disciplinas

O HeatSafe é uma única solução dividida em entregas disciplinares. Este plano orienta a integração sem duplicar responsabilidades nem alterar os contratos que a equipe já estabeleceu.

## 1. Responsabilidades por componente

| Componente | Responsabilidade | Integração com o backend |
|---|---|---|
| ESP32 / Physical Computing | Medir ambiente, movimento e botão de água; acionar alertas locais offline | Envia `POST /leituras` e `POST /eventos` |
| API C# / Software Development | Persistir, recalcular calor, classificar risco, gerar alertas e visitas | Núcleo desta pasta |
| SOA (Java/Spring Boot) + ML | Prever se o dia seguinte será perigoso com dados INMET | Backend consulta `GET /previsao/amanha?estacao=A701` |
| Expo/React Native | Telas dos modos idoso, família e agente | Consome status, alertas, confirmação e visitas |
| Linux / Operating Systems | Hospedar a API e o serviço de previsão, systemd, Nginx, monitoramento | Publica a API e configura URL de previsão |
| Cybersecurity | Auditar PII, taint analysis, segredo e vulnerabilidades | Avalia logs, DTOs, autenticação e fluxos de dados |
| Testing / QA | Requisitos, casos positivos/negativos e evidências | Executa testes unitários, HTTP e valida permissões |

## 2. Fluxo de integração recomendado

1. O ESP32 envia uma leitura de temperatura e umidade.
2. A API valida o dispositivo e recalcula o índice de calor no servidor.
3. A API grava a leitura e atualiza o horário de movimento quando houver movimento.
4. Eventos de `AGUA` e `MOVIMENTO` atualizam os respectivos registros.
5. O serviço de monitoramento avalia risco, inatividade e hidratação.
6. Quando necessário, o backend abre um alerta e registra a notificação demonstrativa.
7. O familiar autorizado consulta e confirma o alerta pelo aplicativo.
8. Se o critério de perigo permanecer sem confirmação no prazo, ou se houver perigo extremo, o caso gera uma visita para a fila da UBS.
9. O agente consulta a lista ordenada por pontuação e atualiza o andamento da visita.
10. Diariamente, o backend consulta o serviço de previsão para o dia seguinte e aplica o modo vigilância à data retornada.

## 3. Regras que não devem ser duplicadas

- A classificação oficial de risco da API deriva do índice recalculado pelo servidor; o nível enviado pelo ESP32 é apenas comparativo.
- O serviço de previsão prevê o dia seguinte usando o conjunto INMET; a API C# não treina nem reproduz o modelo de ML.
- O aplicativo apresenta e envia ações; as regras de domínio permanecem no backend.
- O alarme local do ESP32 deve funcionar sem depender da internet ou da API.
- O backend não deve transformar ausência de resposta do serviço de previsão em previsão perigosa ou segura.

## 4. Ambientes e endereços

### Desenvolvimento local

- API C#: `http://localhost:5078`
- Swagger: `http://localhost:5078/swagger`
- Serviço de previsão (valor padrão configurável): `http://localhost:8080`

Em dispositivos físicos ou máquinas diferentes, `localhost` aponta para o próprio dispositivo, não para o computador que hospeda a API. Nesses casos, usar o IP/hostname acessível da máquina que executa o backend e configurar CORS/TLS/autenticação de forma adequada antes de expor a rede.

### Ambiente de demonstração

- Cadastros e identidades fictícios.
- Não usar endereços nem contatos reais de idosos.
- Não publicar a chave JWT de desenvolvimento.
- Não expor o Swagger ou endpoints de demonstração diretamente à internet.

## 5. Dependências externas que ainda precisam existir

A integração real só estará completa quando os responsáveis entregarem e testarem:

- Firmware ESP32 com os formatos JSON e autenticação acordados.
- Serviço de previsão (HeatSafe Clima API) respondendo o contrato da previsão.
- Aplicativo Expo apontando para a URL da API e usando os perfis/autorização adequados.
- Ambiente Linux e domínio/URL definidos para a demonstração integrada.
- Evidências da análise SentinelaSAST e casos de teste no Azure Boards.

Até lá, a API pode ser demonstrada isoladamente com Swagger, seed e Postman.

# HeatSafe — Guia de teste da API

Este material complementa o backend e permite validar o fluxo principal com Postman ou PowerShell. Todos os dados de exemplo são fictícios.

## 1. Antes de começar

1. Extraia o ZIP do backend.
2. Abra a pasta `HeatSafe-Backend` no VS Code.
3. Execute `TESTAR-HEATSAFE.bat` para restaurar pacotes, compilar e rodar os testes xUnit.
4. Execute `INICIAR-HEATSAFE.bat` e mantenha a janela aberta.
5. Confirme que a API responde em `http://localhost:5078/health`.
6. A documentação Swagger fica em `http://localhost:5078/swagger`.

O endpoint `/auth/token` e as credenciais abaixo são exclusivos do ambiente `Development`. Não utilize essas credenciais ou a chave JWT demonstrativa em produção.

## 2. Dados demonstrativos iniciais

O seed local cria registros fictícios:

| Perfil | Usuário | Senha | ID de domínio | Uso |
|---|---|---|---:|---|
| Administrador | `admin` | `Admin@123` | 99 | Consultar e gerenciar cadastros |
| Familiar | `familiar` | `Familia@123` | 2 | Consultar status e confirmar alertas |
| Agente de saúde | `agente` | `Agente@123` | 3 | Consultar e atualizar visitas |
| Dispositivo | `dispositivo` | `Device@123` | HS-001 | Enviar leituras e eventos |

IDs fictícios seed:
- Idoso: `1`.
- Familiar: `2`.
- Agente: `3`.
- Residência: `1`.
- Dispositivo: `HS-001`.

## 3. Teste automático com PowerShell

Com a API em execução, abra outro terminal PowerShell na pasta raiz do projeto e execute:

```powershell
.\scripts\testar-integracao-api.ps1
```

Para outra URL local:

```powershell
.\scripts\testar-integracao-api.ps1 -BaseUrl "http://localhost:5078"
```

O roteiro verifica saúde da API, autenticação dos quatro perfis, consulta de cadastros, leitura do dispositivo, recálculo do índice pelo servidor, alerta imediato de perigo extremo, lista da UBS, confirmação familiar, atualização de visita e idempotência do evento de hidratação.

## 4. Teste com Postman

Importe os dois arquivos desta pasta:

- `postman/HeatSafe-API.postman_collection.json`
- `postman/HeatSafe-Local.postman_environment.json`

Selecione o ambiente **HeatSafe - Local Development**. Deixe a API rodando e execute a coleção na ordem, de cima para baixo. Os requests de autenticação armazenam os tokens na própria coleção.

A coleção testa a mesma jornada do roteiro PowerShell. A leitura de demonstração usa temperatura de 35,2 °C e umidade de 80%, enquanto informa ao servidor um índice e nível deliberadamente diferentes. O objetivo é comprovar que a API calcula o índice e define a classificação oficial no servidor.

## 5. Repetir o cenário de perigo extremo

A API é idempotente e mantém histórico. Depois de concluir uma execução, o alerta criado pode permanecer confirmado e a mesma ocorrência não deve ser duplicada.

Para repetir o cenário do zero no banco demonstrativo:

1. Pare a API com `Ctrl+C`.
2. Faça backup do arquivo `HeatSafe.Api/heatsafe.db`, caso queira preservar os dados do teste.
3. Remova `HeatSafe.Api/heatsafe.db` e, se existirem, `heatsafe.db-shm` e `heatsafe.db-wal`.
4. Inicie novamente `INICIAR-HEATSAFE.bat`. As migrations e o seed serão aplicados outra vez.

Não faça isso em ambientes com dados reais ou que precisem ser preservados.

## 6. Endpoints principais

| Método | Rota | Perfil |
|---|---|---|
| GET | `/health` | Público |
| POST | `/auth/token` | Development |
| GET | `/idosos` | Administrador |
| GET | `/familiares` | Administrador |
| GET | `/agentes-saude` | Administrador |
| GET | `/residencias` | Administrador |
| GET | `/dispositivos` | Administrador |
| POST | `/leituras` | Dispositivo |
| POST | `/eventos` | Dispositivo |
| GET | `/residencias/{id}/status` | Familiar, idoso, agente ou administrador, com regras de vínculo |
| GET | `/alertas/ativos` | Familiar vinculado |
| POST | `/alertas/{id}/confirmar` | Familiar vinculado |
| GET | `/visitas/prioritarias` | Agente de saúde ou administrador |
| PATCH | `/visitas/{id}/status` | Agente responsável ou administrador |

## 7. Limites importantes da demonstração

- Notificações são registradas pelo adaptador demonstrativo; não são SMS, WhatsApp ou push reais.
- A previsão climática depende do serviço externo Python/FastAPI. Se ele estiver indisponível, a API não inventa uma previsão.
- Autenticação de demonstração existe somente em Development. Antes de produção, substitua-a por provedor de identidade, gestão segura de segredos e autenticação forte dos dispositivos.
- A pontuação da UBS é uma heurística de priorização, não um diagnóstico médico.
- A validação de build e testes deve ser executada no computador da equipe com o SDK .NET 10 e acesso ao NuGet.

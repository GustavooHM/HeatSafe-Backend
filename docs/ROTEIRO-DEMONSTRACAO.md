# HeatSafe — Roteiro de Demonstração Técnica (Backend)

Roteiro sugerido para mostrar o backend C# em uma demonstração acadêmica. Use apenas registros fictícios.

## Preparação

1. Abrir a pasta `HeatSafe-Backend` no VS Code.
2. Executar `TESTAR-HEATSAFE.bat` e guardar o resultado real.
3. Executar `INICIAR-HEATSAFE.bat`.
4. Abrir `http://localhost:5078/swagger`.
5. Abrir outra janela do PowerShell na raiz do repositório para executar o roteiro HTTP.

## Sequência recomendada

### Parte 1 — Saúde e documentação

- Mostrar Swagger e os grupos de endpoints.
- Executar `GET /health`.
- Explicar que o SQLite é criado/aplicado por migration no primeiro início.

### Parte 2 — Perfis e autorização

- Executar `POST /auth/token` com o usuário `dispositivo`.
- Mostrar que o dispositivo possui claim associada ao código `HS-001`.
- Tentar consultar um endpoint administrativo com token de dispositivo e explicar que deve ser negado.
- Autenticar como familiar e explicar a validação do vínculo com o idoso.

### Parte 3 — Leitura e recálculo

- Enviar `POST /leituras` com temperatura e umidade.
- Mostrar que a API calcula o índice novamente no servidor.
- Comparar `indiceCalorInformado` e `indiceCalorCalculado` na resposta.
- Explicar a tabela funcional de classificação: 27, 32, 41 e 54 °C.

### Parte 4 — Alertas e UBS

- Mostrar um cenário fictício de perigo extremo para demonstrar o alerta imediato e a visita.
- Consultar `GET /alertas/ativos` como familiar.
- Confirmar com `POST /alertas/{id}/confirmar`.
- Consultar `GET /visitas/prioritarias` como agente.
- Atualizar status da visita com `PATCH /visitas/{id}/status`.

### Parte 5 — Eventos e idempotência

- Enviar `POST /eventos` com `AGUA`.
- Repetir com o mesmo `Idempotency-Key`.
- Mostrar que a API retorna `registrado: false` na repetição.

## Fechamento técnico

Explicar como os demais componentes se conectam:

- ESP32 envia leituras e eventos.
- Python fornece previsão diária.
- App Expo consome status, alertas e visitas.
- Linux hospeda os serviços.
- Segurança/QA fazem auditoria e validação.

## Cuidado com a demonstração

O cenário de perigo extremo é uma simulação de software e não deve ser interpretado como teste em uma residência real. O backend não substitui procedimentos de emergência nem avaliação profissional. Notificações externas reais não estão configuradas nesta versão.

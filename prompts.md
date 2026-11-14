# Registro de uso de IA — HeatSafe Backend

Este arquivo registra o apoio de IA utilizado durante o desenvolvimento acadêmico. A equipe deve revisar, compreender e validar todo o código antes da entrega.

## Objetivo

Apoiar a implementação do backend C#/.NET da CitizenTech HeatSafe, preservando a proposta, regras de negócio e contratos definidos pela equipe.

## Prompt-base

Atuar como engenheiro de software sênior em C#/.NET e desenvolver o backend HeatSafe com ASP.NET Core Web API, Entity Framework Core, SQLite, LINQ, Dependency Injection, Async/Await, BackgroundService, Swagger, testes xUnit e integração HTTP com o serviço de previsão Python.

## Diretrizes utilizadas

- Preservar os contratos `POST /leituras`, `POST /eventos`, `GET /residencias/{id}/status`, `POST /alertas/{id}/confirmar` e `GET /visitas/prioritarias`.
- Recalcular o índice de calor no servidor, sem confiar na classificação enviada pelo dispositivo.
- Aplicar os limites funcionais do HeatSafe: 27, 32, 41 e 54 °C.
- Não importar dados INMET para o backend: o serviço C# consome a previsão oferecida pelo componente Python.
- Utilizar apenas dados fictícios no seed de demonstração.
- Proteger dados de idosos, familiares e horários de presença.
- Criar testes para limites de risco, cálculo e pontuação.

## Responsabilidade da equipe

A equipe deve validar cálculos, regras, permissões, comportamento da API, integração real com os demais módulos e requisitos do professor. O conteúdo gerado com auxílio de IA não substitui a revisão técnica e a compreensão dos integrantes.

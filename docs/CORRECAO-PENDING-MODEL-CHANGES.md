# Corrigir PendingModelChangesWarning

## Diagnóstico

O `restore` e o `build` passaram, mas a inicialização falhou em `Database.MigrateAsync()` porque o modelo atual de `HeatSafeDbContext` não coincide com o último `HeatSafeDbContextModelSnapshot`.

Os endpoints `/health` e `/swagger` recusam conexão porque o processo da API encerra durante a inicialização. Não é um problema do navegador nem da porta.

## Procedimento

1. Feche qualquer instância da API.
2. Se existir `HeatSafe.Api/heatsafe.db`, faça uma cópia de segurança.
3. Execute `ATUALIZAR-MIGRACAO-HEATSAFE.bat` na raiz do projeto.
4. O script instala `dotnet-ef` localmente em `.tools` e gera a migration `AlinharModeloAtual`, utilizando `HeatSafeDbContextFactory` para não iniciar a API nem os BackgroundServices.
5. Confira se foi criado um novo par de arquivos em `HeatSafe.Api/Data/Migrations` e se `HeatSafeDbContextModelSnapshot.cs` foi atualizado.
6. Execute `dotnet build HeatSafe.sln`.
7. Confirme que não restam alterações pendentes:

```powershell
.\.tools\dotnet-ef.exe migrations has-pending-model-changes --project .\HeatSafe.Api\HeatSafe.Api.csproj --startup-project .\HeatSafe.Api\HeatSafe.Api.csproj
```

8. Revise a migration. Se houver operações destrutivas (`DropTable` ou `DropColumn`) ou dados importantes no banco, pare e revise antes de aplicar.
9. Execute `INICIAR-HEATSAFE.bat`. O `MigrateAsync()` aplicará a nova migration automaticamente.
10. Abra `http://localhost:5078/health` e `http://localhost:5078/swagger`.

## Importante

- Não suprima `RelationalEventId.PendingModelChangesWarning` para esconder o problema.
- Não apague o banco existente como primeira tentativa.
- A conexão `heatsafe-design-time.db` da factory é usada para construir o modelo em design-time; a geração de migration não deve atualizar o banco operacional.

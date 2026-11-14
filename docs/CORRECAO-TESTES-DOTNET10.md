# Correção da execução de testes no .NET 10

## Diagnóstico

O projeto anterior referenciava `xunit.v3` 4.0.3. Essa versão incorpora suporte ao Microsoft Testing Platform (MTP) v2. O `global.json` da entrega, porém, não selecionava o runner MTP, e o script chamava o `dotnet test` pelo fluxo tradicional VSTest. Por isso o build terminava, mas a etapa de testes era recusada com a mensagem:

`Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 and later.`

## Correção aplicada

Para manter o fluxo VSTest convencional — amplamente compatível com `dotnet test` e com o adaptador do VS Code — a versão atual migra os testes para xUnit 2.9.3 e mantém:

- `Microsoft.NET.Test.Sdk` 17.14.1;
- `xunit` 2.9.3;
- `xunit.runner.visualstudio` 3.1.5.

O adaptador Visual Studio 3.1.5 oferece suporte a testes xUnit v1, v2 e v3, em .NET 8 ou superior. O projeto não referencia mais `xunit.v3`, portanto não puxa o MTP v2 por meio desse pacote.

## Como validar no Windows

Na raiz da solução, execute:

```powershell
dotnet restore .\HeatSafe.sln
dotnet build .\HeatSafe.sln --no-restore
dotnet test .\HeatSafe.Tests\HeatSafe.Tests.csproj --no-build --configuration Debug --logger "console;verbosity=normal"
```

Ou execute:

```powershell
.\TESTAR-HEATSAFE.bat
```

A validação dos testes só estará aprovada se `dotnet test` terminar com código de saída zero e apresentar resumo com `Failed: 0`. O build bem-sucedido, sozinho, não comprova que os testes foram executados.

## Fontes oficiais

- xUnit — pacotes e adaptador VSTest: https://xunit.net/docs/nuget-packages-v2
- NuGet — `xunit.runner.visualstudio` 3.1.5, compatibilidade com xUnit v1/v2/v3: https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5
- Microsoft Learn — execução de testes com .NET 10: https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test


## Correção 3.4 — faixa de temperatura consistente

O teste de integração identificou que a calculadora aceitava temperaturas até -50 °C, enquanto o DTO de entrada e o serviço de integração já restringiam a faixa operacional a -20 °C até 60 °C. A calculadora foi alinhada a esse mesmo contrato, evitando que chamadas diretas ao domínio aceitassem valores que a API rejeitaria. Foram acrescentados testes para os dois limites válidos (-20 °C e 60 °C), mantendo -21 °C como entrada inválida.

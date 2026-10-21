---
inclusion: fileMatch
fileMatchPattern: ["**/*.cs"]
---
# Padrões de código (C#)

## SOLID aplicado

- **S:** um handler por caso de uso. Classe com mais de ~200 linhas ou mais de 5 dependências no construtor deve ser dividida.
- **O:** estender com decorators e strategies, sem alterar classes estáveis.
- **L:** implementações nunca lançam `NotImplementedException` nem alteram o contrato da interface.
- **I:** interfaces pequenas, definidas pelo consumidor (`IProductReader` e `IProductWriter`, não `IProductService`).
- **D:** depender de abstrações de `Application`; injeção por construtor; sem service locator e sem `static` com estado.

## Convenções

- `Nullable` e `TreatWarningsAsErrors` habilitados; `.editorconfig` é a fonte de verdade de estilo.
- `async/await` de ponta a ponta, sufixo `Async`, `CancellationToken` em toda operação de I/O.
- `record` para DTOs, commands, queries, events e value objects.
- Sem `DateTime.Now/UtcNow`: usar `TimeProvider`.
- IDs de agregado: `Guid` gerado na aplicação (`Guid.CreateVersion7()`).
- Dinheiro: `decimal` + moeda explícita em value object `Money`.
- Exceptions apenas para situações excepcionais. Falhas de negócio retornam `Result`.
- Comentários explicam o *porquê*, nunca o *quê*.
- Decorators via Scrutor: registrar os handlers (`Scan`) **antes** de decorar; usar `TryDecorate` (não `Decorate`) enquanto puder não haver handlers; no `Scan`, usar `publicOnly: false` para encontrar handlers `internal`. Manter um teste que verifica a cadeia de decorators de um handler real.

## Padrão de caso de uso

```csharp
public sealed record CreateProductCommand(string Name, decimal Price);

public sealed class CreateProductHandler(IProductRepository products, IUnitOfWork uow)
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken ct)
    {
        var product = Product.Create(command.Name, command.Price);
        if (product.IsFailure) return Result.Failure<Guid>(product.Error);

        await products.AddAsync(product.Value, ct);
        await uow.CommitAsync(ct);
        return Result.Success(product.Value.Id);
    }
}
```

Validação, logging e métricas são **decorators** registrados no composition root (Scrutor), não código dentro do handler.

## Proibido

- Lógica de negócio em endpoints, repositories ou configurações do EF.
- `DbContext` dentro de handlers.
- Compartilhar entidades de domínio entre serviços.
- Capturar `Exception` genérica sem relançar ou registrar com contexto.

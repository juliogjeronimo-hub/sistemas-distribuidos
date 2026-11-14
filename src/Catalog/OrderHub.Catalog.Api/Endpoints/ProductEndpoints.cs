using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.UseCases.Commands;
using OrderHub.Catalog.Application.UseCases.DTOs;
using OrderHub.Catalog.Application.UseCases.Queries;
using OrderHub.Catalog.Api.Auth;
using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Api.Endpoints;

internal static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/products")
            .WithName("Products")
            .WithOpenApi()
            .RequireAuthorization(CatalogScopes.Read);

        group.MapGet("/", ListProducts)
            .WithName("ListProducts")
            .WithSummary("List all products (paginated)")
            .Produces<PaginatedResponse<ProductResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/{id}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .RequireAuthorization(CatalogScopes.Write)
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .RequireAuthorization(CatalogScopes.Write)
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/deactivate", DeactivateProduct)
            .WithName("DeactivateProduct")
            .WithSummary("Deactivate a product")
            .RequireAuthorization(CatalogScopes.Write)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListProducts(
        HttpContext context,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 20;
        else if (pageSize > 50)
            pageSize = 50;

        var handler = context.RequestServices.GetRequiredService<IQueryHandler<ListProductsQuery, PaginatedResponse<ProductResponse>>>();
        var query = new ListProductsQuery(page, pageSize, includeInactive);
        var result = await handler.HandleAsync(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : MapResultToHttpResponse(result.Error, context);
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        HttpContext context,
        CancellationToken ct = default)
    {
        var handler = context.RequestServices.GetRequiredService<IQueryHandler<GetProductByIdQuery, ProductResponse>>();
        var query = new GetProductByIdQuery(id);
        var result = await handler.HandleAsync(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : MapResultToHttpResponse(result.Error, context);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductCommand command,
        HttpContext context,
        CancellationToken ct = default)
    {
        var handler = context.RequestServices.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>();
        var result = await handler.HandleAsync(command, ct);

        if (result.IsFailure)
            return MapResultToHttpResponse(result.Error, context);

        var productId = result.Value;
        return Results.Created($"/v1/products/{productId}", null);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductCommand command,
        HttpContext context,
        CancellationToken ct = default)
    {
        var commandWithId = command with { Id = id };
        var handler = context.RequestServices.GetRequiredService<ICommandHandler<UpdateProductCommand>>();
        var result = await handler.HandleAsync(commandWithId, ct);

        if (result.IsFailure)
            return MapResultToHttpResponse(result.Error, context);

        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateProduct(
        Guid id,
        HttpContext context,
        CancellationToken ct = default)
    {
        var handler = context.RequestServices.GetRequiredService<ICommandHandler<DeactivateProductCommand>>();
        var command = new DeactivateProductCommand(id);
        var result = await handler.HandleAsync(command, ct);

        if (result.IsFailure)
            return MapResultToHttpResponse(result.Error, context);

        return Results.NoContent();
    }

    private static IResult MapResultToHttpResponse(Error error, HttpContext context)
    {
        var traceId = context.TraceIdentifier;

        return error.Code switch
        {
            var code when code.StartsWith("Validation.") => Results.BadRequest(
                CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error",
                    detail: error.Message,
                    traceId: traceId,
                    errorFieldCode: error.Code.Replace("Validation.", ""))),

            var code when code.EndsWith(".NotFound") => Results.NotFound(
                CreateProblemDetails(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: error.Message,
                    traceId: traceId)),

            var code when code == "Validation.Sku" && error.Message.Contains("already exists") =>
                Results.Conflict(
                    CreateProblemDetails(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Conflict",
                        detail: error.Message,
                        traceId: traceId,
                        errorFieldCode: "Sku")),

            _ => Results.BadRequest(
                CreateProblemDetails(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: error.Message,
                    traceId: traceId))
        };
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string traceId,
        string? errorFieldCode = null)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://api.orderhub.example/errors/{GetErrorType(statusCode)}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = null
        };

        problemDetails.Extensions["traceId"] = traceId;

        if (errorFieldCode is not null)
        {
            var errors = new Dictionary<string, string[]>
            {
                { errorFieldCode, [detail] }
            };
            problemDetails.Extensions["errors"] = errors;
        }

        return problemDetails;
    }

    private static string GetErrorType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "validation-error",
        StatusCodes.Status404NotFound => "not-found",
        StatusCodes.Status409Conflict => "conflict",
        _ => "error"
    };
}


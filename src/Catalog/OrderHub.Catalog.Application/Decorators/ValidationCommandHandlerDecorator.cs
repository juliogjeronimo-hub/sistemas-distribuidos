using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Decorators;

// When no validator is registered the command passes through unchanged, so a
// validator can be added later without touching the handler.
internal sealed class ValidationCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IEnumerable<IValidator<TCommand>> validators,
    ILogger<ValidationCommandHandlerDecorator<TCommand>> logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var validatorList = validators.ToList();

        if (validatorList.Count == 0)
            return await inner.HandleAsync(command, ct);

        var context = new ValidationContext<TCommand>(command);

        var failures = (await Task.WhenAll(
                validatorList.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var first = failures[0];
            var error = Error.Validation(first.PropertyName, first.ErrorMessage);

            HandlerLog.ValidationFailed(
                logger, typeof(TCommand).Name, error.Code, error.Message);

            return Result.Failure(error);
        }

        return await inner.HandleAsync(command, ct);
    }
}

internal sealed class ValidationCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators,
    ILogger<ValidationCommandHandlerDecorator<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var validatorList = validators.ToList();

        if (validatorList.Count == 0)
            return await inner.HandleAsync(command, ct);

        var context = new ValidationContext<TCommand>(command);

        var failures = (await Task.WhenAll(
                validatorList.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var first = failures[0];
            var error = Error.Validation(first.PropertyName, first.ErrorMessage);

            HandlerLog.ValidationFailed(
                logger, typeof(TCommand).Name, error.Code, error.Message);

            return Result.Fail<TResponse>(error);
        }

        return await inner.HandleAsync(command, ct);
    }
}

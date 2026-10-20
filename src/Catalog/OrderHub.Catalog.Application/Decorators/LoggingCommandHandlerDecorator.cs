using Microsoft.Extensions.Logging;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Application.Decorators;

// ── Source-generated log messages (CA1848-compliant, zero allocation) ─────────

internal static partial class HandlerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Executing command {CommandName}")]
    public static partial void CommandStarted(ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Command {CommandName} succeeded")]
    public static partial void CommandSucceeded(ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Command {CommandName} failed with error {ErrorCode}: {ErrorMessage}")]
    public static partial void CommandFailed(
        ILogger logger, string commandName, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Validation failed for {CommandName}: {ErrorCode} — {ErrorMessage}")]
    public static partial void ValidationFailed(
        ILogger logger, string commandName, string errorCode, string errorMessage);
}

// ── Logging decorators ────────────────────────────────────────────────────────

/// <summary>
/// Decorator that logs the start and outcome of every void command execution.
/// Registered via Scrutor; no handler contains logging code.
/// </summary>
/// <typeparam name="TCommand">The command being handled.</typeparam>
internal sealed class LoggingCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<LoggingCommandHandlerDecorator<TCommand>> logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var commandName = typeof(TCommand).Name;

        HandlerLog.CommandStarted(logger, commandName);

        var result = await inner.HandleAsync(command, ct);

        if (result.IsSuccess)
            HandlerLog.CommandSucceeded(logger, commandName);
        else
            HandlerLog.CommandFailed(logger, commandName, result.Error.Code, result.Error.Message);

        return result;
    }
}

/// <summary>
/// Decorator that logs the start and outcome of every typed command execution.
/// </summary>
/// <typeparam name="TCommand">The command being handled.</typeparam>
/// <typeparam name="TResponse">The success value type.</typeparam>
internal sealed class LoggingCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingCommandHandlerDecorator<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        var commandName = typeof(TCommand).Name;

        HandlerLog.CommandStarted(logger, commandName);

        var result = await inner.HandleAsync(command, ct);

        if (result.IsSuccess)
            HandlerLog.CommandSucceeded(logger, commandName);
        else
            HandlerLog.CommandFailed(logger, commandName, result.Error.Code, result.Error.Message);

        return result;
    }
}

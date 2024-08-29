namespace Demo.WebApi;

internal static partial class LoggerExtensions
{
    // Using high performance logging pattern, see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    // with extension methods.

    // Using theory of event codes, see: https://github.com/sgryphon/essential-logging/blob/main/docs/Event-Ids.md
    // Event ID uses format ABxx or AByyy, with the xx or yyy being a sequential number:

    // Event ID Type:
    // 1bxx Information - preliminary (before, e.g. starting)
    // 2bxx Information - completion (after, e.g. started)
    // 4bxx Warning
    // 5bxx Error
    // 9bxx Critical
    // 1byyy Debug - preliminary
    // 2byyy Debug - completion
    // yyy (i.e. <1000, possibly 0) Debug - intermediate / general

    // Event ID Category:
    // a0xx / a0yyy common services
    // a1xx / a1yyy ...
    // a9xx Unknown

    [LoggerMessage(
        LogLevel.Information,
        EventId = 1100,
        EventName = nameof(DiceRollRequested),
        Message = "Dice roll of {Dice} requested"
    )]
    public static partial void DiceRollRequested(this ILogger logger, string dice);

    [LoggerMessage(
        LogLevel.Debug,
        EventId = 21000,
        EventName = nameof(DiceRollResult),
        Message = "Dice roll result {Dice}: {DiceResult}"
    )]
    public static partial void DiceRollResult(
        this ILogger logger,
        string dice,
        string diceResult
    );
}

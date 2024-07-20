namespace Telegram.Bot.States;

public static class Constants
{
    public const char CommandPrefix = '/';
    public const char CommandDataSeparatorChar = ' ';
    public static readonly char[] CommandSeparatorChars = [ CommandDataSeparatorChar, '@' ];

    public const string DefaultStateName = "default";
    public const string StateStepKey = "STATE_STEP";
    public const string AllStepsCompletedLabel = "<<ALL COMPLETED>>";

    public const string GlobalCommandsServiceKey = "global_actoins:commands";
    public const string GlobalCallbackServiceKey = "global_actions:callbacks";

    public static string CommandKeySelector(StateContext ctx) => ctx.Update.Command;

    public const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";
}

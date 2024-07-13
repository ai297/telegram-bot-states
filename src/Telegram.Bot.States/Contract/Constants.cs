namespace Telegram.Bot.States;

public static class Constants
{
    public const char CommandPrefix = '/';
    public const char CommandDataSeparatorChar = ' ';
    public static readonly char[] CommandSeparatorChars = [ CommandDataSeparatorChar, '@' ];

    public const string DefaultStateName = "default";
    public const string StateStepKey = "STATE_STEP";
    public const string StateChangedKey = "STATE_CHANGED";
    public const string AllStepsCompletedLabel = "<<ALL COMPLETED>>";
}

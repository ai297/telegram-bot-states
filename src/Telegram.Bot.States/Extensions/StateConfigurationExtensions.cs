namespace Telegram.Bot.States;

internal static class StateConfigurationExtensions
{
    public static string AsStateKey(this string stateName) => $"STATE_{stateName}";
}

namespace Telegram.Bot.States;

public interface IActionFactoriesCollection
{
    IStateActionFactory? GetApplicableFactoryIfExists(ChatUpdate update, ChatState state);
    internal IActionFactoriesCollection Merge(IActionFactoriesCollection? actionFactories);
}

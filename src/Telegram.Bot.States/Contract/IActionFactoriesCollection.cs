namespace Telegram.Bot.States;

public interface IActionFactoriesCollection
{
    IStateActionFactory? GetApplicableFactoryIfExists(StateContext context);
    internal IActionFactoriesCollection Merge(IActionFactoriesCollection? actionFactories);
}

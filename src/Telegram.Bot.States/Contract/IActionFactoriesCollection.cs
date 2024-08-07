namespace Telegram.Bot.States;

public interface IActionFactoriesCollection<in TCtx> where TCtx : StateContext
{
    IStateActionFactory<TCtx>? GetApplicableFactoryIfExists(StateContext context);
    internal IActionFactoriesCollection<TCtx> Merge(IActionFactoriesCollection<StateContext>? actionFactories);
}

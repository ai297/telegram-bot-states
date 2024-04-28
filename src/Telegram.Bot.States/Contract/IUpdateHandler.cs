using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public interface IUpdateHandler
{
    Task Handle(Update update);
}

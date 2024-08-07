using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telegram.Bot.States;

public class CommandDescriptions(IList<CommandDescription> list)
    : ReadOnlyCollection<CommandDescription>(list), ICommandDescriptions
{
}

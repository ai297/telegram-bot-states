using System;

namespace Telegram.Bot.States;

public delegate T StateServiceFactory<out T>(IServiceProvider serviceProvider, string stateName);

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Telegram.Bot.States;

internal static class DelegateHelper
{
    private const string Invoke = "Invoke";

    public static Func<TProvider, TDelegate> CreateDelegateFactory<TProvider, TDelegate>(
        Delegate @delegate,
        Expression<Func<TProvider, Type, object>> getService)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(@delegate);
        ArgumentNullException.ThrowIfNull(getService);

        var targetType = typeof(TDelegate);
        var targetMethodInfo = targetType.GetMethod(Invoke, BindingFlags.Instance | BindingFlags.Public);

        if (targetMethodInfo == null) throw new MissingMethodException(
            $"Can't create factory for delegate '{TypeHelper.GetShortName<TDelegate>()}', " +
            $"because the '{Invoke}' method not found in this delegate type.");

        if (@delegate.Method.ReturnType != targetMethodInfo.ReturnType) throw new ArgumentException(
            $"Can't create factory for delegate '{TypeHelper.GetShortName<TDelegate>()}', " +
            $"because its return type '{TypeHelper.GetShortName(targetMethodInfo.ReturnType)}' " +
            $"is different with provided delegate - '{TypeHelper.GetShortName(@delegate.Method.ReturnType)}'.");

        var delegateParameters = @delegate.Method.GetParameters();
        var targetParameters = targetMethodInfo.GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();

        var providerParameter = Expression.Parameter(typeof(TProvider));
        var resolvedParameters = new Expression[delegateParameters.Length];

        var targetParameterIndex = 0;
        for (var i = 0; i < delegateParameters.Length; i++)
        {
            // arguments of @delegate, which have different type with target delegate parameters -
            // should be resolved by service provider
            if (targetParameters.Length <= targetParameterIndex
                || delegateParameters[i].ParameterType != targetParameters[targetParameterIndex].Type)
            {
                resolvedParameters[i] = Expression.Convert(
                    Expression.Invoke(getService, providerParameter,
                        Expression.Constant(delegateParameters[i].ParameterType)),
                    delegateParameters[i].ParameterType);

                continue;
            }

            resolvedParameters[i] = targetParameters[targetParameterIndex];
            targetParameterIndex++;
        }

        var methodTarget = @delegate.Target != null ? Expression.Constant(@delegate.Target) : null;
        var factoryExpression = Expression.Lambda<Func<TProvider, TDelegate>>(
            Expression.Lambda<TDelegate>(
                Expression.Call(methodTarget, @delegate.Method, resolvedParameters),
                targetParameters),
            providerParameter);

        return factoryExpression.Compile();
    }
}
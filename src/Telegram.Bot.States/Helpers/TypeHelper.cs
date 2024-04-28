using System;
using System.Linq;
using System.Reflection;

namespace Telegram.Bot.States;

internal static class TypeHelper
{
    public static string GetShortName<T>() => GetShortName(typeof(T));

    public static string GetShortName(Type type)
    {
        var genericArgs = type.GetGenericArguments();
        // var prefix = Convert.ToBase64String(BitConverter.GetBytes(type.GetHashCode()));

        if (genericArgs.Length == 0)
            return type.Name.Split(['`'], 2)[0];

        return $"{type.Name.Split(['`'], 2)[0]}<{string.Join(',', genericArgs.Select(t => t.Name))}>";
    }

    public static ConstructorInfo GetConstructor(Type type, params Type[] parameterTypes)
    {
        if (!type.IsClass || type.IsAbstract)
            throw new InvalidOperationException($"Can't get constructor of type '{type.Name}'");

        var ctor = type.GetConstructors()
            .FirstOrDefault(c => c.IsPublic && IsEqualArrays(
                c.GetParameters(),
                parameterTypes,
                (pi, t) => pi.ParameterType == t));

        if (ctor == null) throw new InvalidOperationException(
            $"Can't find matching constructor of type '{type.Name}'.");

        return ctor;
    }

    private static bool IsEqualArrays<T1, T2>(T1[] arr1, T2[] arr2, Func<T1, T2, bool> compare)
    {
        if (arr1 == null && arr2 == null) return true;
        if (arr1 == null || arr2 == null) return false;
        if (arr1.Length != arr2.Length) return false;

        for (var i = 0; i < arr1.Length; i++)
        {
            if (!compare(arr1[i], arr2[i])) return false;
        }

        return true;
    }
}
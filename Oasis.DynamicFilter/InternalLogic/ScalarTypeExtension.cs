namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Linq;

public static class ScalarTypeExtension
{
    private const string NullableTypeName = "System.Nullable`1[[";
    private static readonly Type[] NonPrimitiveScalarTypes = new[]
    {
        typeof(string), typeof(decimal), typeof(decimal?), typeof(DateTime), typeof(DateTime?),
    };

    public static bool IsScalarType(this Type type)
    {
        return (type.IsValueType && (type.IsPrimitive || type.IsEnum || type.IsNullablePrimitiveOrEnum())) || NonPrimitiveScalarTypes.Contains(type);
    }

    public static bool IsNullablePrimitiveOrEnum(this Type type)
    {
        return type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1 && (type.GenericTypeArguments[0].IsPrimitive || type.GenericTypeArguments[0].IsEnum);
    }

    public static bool Matches(this Type type1, Type type2)
    {
        if (type1 == type2)
        {
            return true;
        }

        if (type1.FullName!.StartsWith(NullableTypeName) && type1.GenericTypeArguments.Length == 1)
        {
            type1 = type1.GenericTypeArguments[0];
        }

        if (type2.FullName!.StartsWith(NullableTypeName) && type2.GenericTypeArguments.Length == 1)
        {
            type2 = type2.GenericTypeArguments[0];
        }

        return type1 == type2;
    }
}

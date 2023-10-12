namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class FilterTypeBuilder
{
    public const string FilterMethodName = "Filter";
    private readonly ModuleBuilder _moduleBuilder;

    public FilterTypeBuilder(ModuleBuilder moduleBuilder)
    {
        _moduleBuilder = moduleBuilder;
    }

    public FilterMethodBuilder<TEntity, TFilter> BuildFilterMethodBuilder<TEntity, TFilter>()
        where TEntity : class
        where TFilter : class
    {
        var entityType = typeof(TEntity);
        var filterType = typeof(TFilter);
        var typeBuilder = _moduleBuilder.DefineType(GetDynamicTypeName(entityType, filterType), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);
        var methodBuilder = typeBuilder.DefineMethod(FilterMethodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(filterType);
        methodBuilder.SetReturnType(typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool))));
        var generator = methodBuilder.GetILGenerator();
        return new FilterMethodBuilder<TEntity, TFilter>(typeBuilder, generator);
    }

    private static string GetDynamicTypeName(Type entityType, Type filterType) => $"Filter_${GetTypeName(entityType)}_${GetTypeName(filterType)}";

    private static string GetTypeName(Type type) => $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
}

internal sealed class FilterMethodBuilder<TEntity, TFilter>
    where TEntity : class
    where TFilter : class
{
    private const string GetValueOrDefaultMethodName = "GetValueOrDefault";
    private const string HasValuePropertyName = "HasValue";
    private readonly TypeBuilder _typeBuilder;
    private readonly ILGenerator _generator;

    public FilterMethodBuilder(TypeBuilder typeBuilder, ILGenerator generator)
    {
        _typeBuilder = typeBuilder;
        _generator = generator;
    }

    internal Type Build(
        ISet<string>? configuredEntityProperties,
        ISet<string>? configuredFilterProperties,
        Dictionary<string, Dictionary<string, CompareData<TFilter>>>? compareDictionary,
        Dictionary<string, Dictionary<string, ContainData<TFilter>>>? containDictionary,
        Dictionary<string, Dictionary<string, InData<TFilter>>>? inDictionary,
        Dictionary<string, Dictionary<string, Dictionary<string, FilterRangeData<TFilter>>>>? filterRangeDictionary,
        Dictionary<string, Dictionary<string, Dictionary<string, EntityRangeData<TFilter>>>>? entityRangeDictionary)
    {
        var (compare, contain, isIn) = ExtractFilterProperties(configuredEntityProperties, configuredFilterProperties);
        // TODO: continue here
        return _typeBuilder.CreateType()!;
    }

    internal (Dictionary<string, CompareData<TFilter>>, Dictionary<string, ContainData<TFilter>>, Dictionary<string, InData<TFilter>>) ExtractFilterProperties(ISet<string>? configuredEntityProperties, ISet<string>? configuredFilterProperties)
    {
        var filterProperties = typeof(TFilter).GetProperties(Utilities.PublicInstance)
            .Where(p => p.PropertyType.IsFilterableType() && p.GetMethod != default && (configuredFilterProperties == null || !configuredFilterProperties.Contains(p.Name)));
        var entityProperties = typeof(TEntity).GetProperties(Utilities.PublicInstance)
            .Where(p => p.PropertyType.IsFilterableType() && p.GetMethod != default && (configuredEntityProperties == null || !configuredEntityProperties.Contains(p.Name)))
            .ToDictionary(p => p.Name, p => p);

        var compareDictionary = new Dictionary<string, CompareData<TFilter>>();
        var containDictionary = new Dictionary<string, ContainData<TFilter>>();
        var inDictionary = new Dictionary<string, InData<TFilter>>();
        foreach (var filterProperty in filterProperties)
        {
            var filterPropertyType = filterProperty.PropertyType;
            var filterPropertyName = filterProperty.Name;
            if (entityProperties.TryGetValue(filterPropertyName, out var entityProperty))
            {
                var entityPropertyType = entityProperty.PropertyType;
                var comparison = TypeUtilities.GetComparisonConversion(entityPropertyType, filterPropertyType, FilterByPropertyType.Equality);
                if (comparison != null)
                {
                    var c = comparison.Value;
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    compareDictionary.Add(filterPropertyName, new CompareData<TFilter>(entityProperty, c.Item1, c.Item2, FilterByPropertyType.Equality, filterProperty, c.Item3, c.Item4, null, ignoreIf));
                    continue;
                }

                var contains = TypeUtilities.GetContainConversion(entityPropertyType, filterPropertyType);
                if (contains != null)
                {
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    containDictionary.Add(filterPropertyName, new ContainData<TFilter>(entityProperty, FilterByPropertyType.Contains, filterProperty, contains, null, ignoreIf));
                    continue;
                }

                var isIn = TypeUtilities.GetContainConversion(filterPropertyType, entityPropertyType);
                if (isIn != null)
                {
                    var ignoreIf = filterPropertyType.IsClass || filterPropertyType.IsNullable(out _) ? TypeUtilities.BuildFilterPropertyIsDefaultFunction<TFilter>(filterProperty) : null;
                    inDictionary.Add(filterPropertyName, new InData<TFilter>(entityProperty, isIn, FilterByPropertyType.In, filterProperty, null, ignoreIf));
                    continue;
                }
            }
        }

        return (compareDictionary, containDictionary, inDictionary);
    }
}

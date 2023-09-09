namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

internal sealed class FilterBuilder : IFilterBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly IFilterGlobalConfiguration _filterGlobalConfiguration;
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _expressionBuilderCache = new ();

    public FilterBuilder(IFilterGlobalConfiguration filterGlobalConfiguration)
    {
        var name = new AssemblyName($"{GenerateRandomTypeName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new (module.DefineType("Mapper", TypeAttributes.Public));
        _filterGlobalConfiguration = filterGlobalConfiguration;
    }

    public IFilter Build()
    {
        var type = _dynamicMethodBuilder.Build();
        return new Filter(_expressionBuilderCache, type);
    }

    public IFilterConfiguration<TFilter, TEntity> Configure<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public void Register<TFilter, TEntity>()
        where TFilter : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    private static string GenerateRandomTypeName(int length)
    {
        const string AvailableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int AvailableCharsCount = 52;
        var bytes = new byte[length];
        RandomNumberGenerator.Create().GetBytes(bytes);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }
}

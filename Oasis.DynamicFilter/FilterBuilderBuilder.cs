namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.InternalLogic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using System;

public sealed class FilterBuilderBuilder : IFilterBuilderBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private FilterBuilderConfiguration? _configuration;

    public FilterBuilderBuilder()
    {
        var name = new AssemblyName($"{GenerateRandomTypeName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new (module.DefineType("Mapper", TypeAttributes.Public));
    }

    internal Dictionary<Type, Delegate> EqualityManager { get; private set; } = new Dictionary<Type, Delegate>();

    public IFilterBuilderConfigurationBuilder Configure()
    {
        return _configuration ??= new FilterBuilderConfiguration(this, _dynamicMethodBuilder);
    }

    public IFilterBuilder Make()
    {
        var builder = new FilterBuilder(_dynamicMethodBuilder, _configuration, EqualityManager);
        EqualityManager = new Dictionary<Type, Delegate>();
        return builder;
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

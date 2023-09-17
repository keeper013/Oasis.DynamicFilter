namespace Oasis.DynamicFilter;

using Oasis.DynamicFilter.InternalLogic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;

public sealed class FilterBuilderFactory : IFilterBuilderFactory
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private FilterBuilderConfiguration? _configuration;

    public FilterBuilderFactory()
    {
        var name = new AssemblyName($"{GenerateRandomTypeName(16)}.Oasis.DynamicFilter.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new (module.DefineType("Mapper", TypeAttributes.Public));
    }

    internal EqualityManager EqualityManager { get; } = new ();

    public IFilterBuilderConfigurationBuilder Configure()
    {
        return _configuration ??= new FilterBuilderConfiguration(this, _dynamicMethodBuilder);
    }

    public IFilterBuilder Make()
    {
        return new FilterBuilder(_dynamicMethodBuilder, _configuration, EqualityManager);
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

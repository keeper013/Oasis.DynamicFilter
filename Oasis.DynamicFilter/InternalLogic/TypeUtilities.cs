namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

internal record struct ComparisonConversion(Type? leftConvertTo, Type? rightConvertTo);
internal record struct ContainConversion(Type containerItemType, Type? itemConvertTo, bool isCollection, bool nullValueNotCovered);

internal static class TypeUtilities
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, ComparisonConversion>> _convertForComparisonDirectionDictionary = new Dictionary<Type, IReadOnlyDictionary<Type, ComparisonConversion>>
    {
        {
            typeof(sbyte), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(short), new (typeof(short), null) },
                { typeof(int), new (typeof(int), null) },
                { typeof(long), new (typeof(long), null) },
                { typeof(byte), new (typeof(short), typeof(short)) },
                { typeof(ushort), new (typeof(int), typeof(int)) },
                { typeof(uint), new (typeof(long), typeof(long)) },
                { typeof(float), new (typeof(float), null) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(sbyte?), null) },
                { typeof(short?), new (typeof(short?), null) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(short?), typeof(short?)) },
                { typeof(ushort?), new (typeof(int?), typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(sbyte?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(sbyte?)) },
                { typeof(short), new (typeof(short?), typeof(short?)) },
                { typeof(int), new (typeof(int?), typeof(int?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (typeof(short?), typeof(short?)) },
                { typeof(ushort), new (typeof(int?), typeof(int?)) },
                { typeof(uint), new (typeof(long?), typeof(long?)) },
                { typeof(float), new (typeof(float?), typeof(float?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), new (typeof(short?), null) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(short?), typeof(short?)) },
                { typeof(ushort?), new (typeof(int?), typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(byte), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(short), typeof(short)) },
                { typeof(short), new (typeof(short), null) },
                { typeof(int), new (typeof(int), null) },
                { typeof(long), new (typeof(long), null) },
                { typeof(ushort), new (typeof(ushort), null) },
                { typeof(uint), new (typeof(uint), null) },
                { typeof(ulong), new (typeof(ulong), null) },
                { typeof(float), new (typeof(float), null) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(short?), typeof(short?)) },
                { typeof(short?), new (typeof(short?), null) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(byte?), null) },
                { typeof(ushort?), new (typeof(ushort?), null) },
                { typeof(uint?), new (typeof(uint?), null) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(byte?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(short?), typeof(short?)) },
                { typeof(short), new (typeof(short?), typeof(short?)) },
                { typeof(int), new (typeof(int?), typeof(int?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (null, typeof(byte?)) },
                { typeof(ushort), new (typeof(ushort?), typeof(ushort?)) },
                { typeof(uint), new (typeof(uint?), typeof(uint?)) },
                { typeof(ulong), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), new (typeof(float?), typeof(float?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (typeof(short?), typeof(short?)) },
                { typeof(short?), new (typeof(short?), null) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(ushort?), new (typeof(ushort?), null) },
                { typeof(uint?), new (typeof(uint?), null) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(short), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(short)) },
                { typeof(int), new (typeof(int), null) },
                { typeof(long), new (typeof(long), null) },
                { typeof(byte), new (null, typeof(short)) },
                { typeof(ushort), new (typeof(int), typeof(int)) },
                { typeof(uint), new (typeof(long), typeof(long)) },
                { typeof(float), new (typeof(float), null) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(short?), typeof(short?)) },
                { typeof(short?), new (null, typeof(short?)) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(short?), typeof(short?)) },
                { typeof(ushort?), new (typeof(int?), typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(short?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(short?)) },
                { typeof(short), new (null, typeof(short?)) },
                { typeof(int), new (typeof(int?), typeof(int?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (null, typeof(short?)) },
                { typeof(ushort), new (typeof(int?), typeof(int?)) },
                { typeof(uint), new (typeof(long?), typeof(long?)) },
                { typeof(float), new (typeof(float?), typeof(float?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (null, typeof(short?)) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(short?), typeof(short?)) },
                { typeof(ushort?), new (typeof(int?), typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(ushort), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(int), typeof(int)) },
                { typeof(short), new (typeof(int), typeof(int)) },
                { typeof(int), new (typeof(int), null) },
                { typeof(long), new (typeof(long), null) },
                { typeof(byte), new (null, typeof(ushort)) },
                { typeof(uint), new (typeof(uint), null) },
                { typeof(ulong), new (typeof(ulong), null) },
                { typeof(float), new (typeof(float), null) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(int?), typeof(int?)) },
                { typeof(short?), new (typeof(int?), typeof(int?)) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(ushort?), typeof(ushort?)) },
                { typeof(ushort?), new (typeof(ushort?), null) },
                { typeof(uint?), new (typeof(uint?), null) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(ushort?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(int?), typeof(int?)) },
                { typeof(short), new (typeof(int?), typeof(int?)) },
                { typeof(int), new (typeof(int?), typeof(int?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (null, typeof(ushort?)) },
                { typeof(ushort), new (null, typeof(ushort?)) },
                { typeof(uint), new (typeof(uint?), typeof(uint?)) },
                { typeof(ulong), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), new (typeof(float?), typeof(float?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (typeof(int?), typeof(int?)) },
                { typeof(short?), new (typeof(int?), typeof(int?)) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (null, typeof(ushort?)) },
                { typeof(uint?), new (typeof(uint?), null) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(int), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(int)) },
                { typeof(short), new (null, typeof(int)) },
                { typeof(long), new (typeof(long), null) },
                { typeof(byte), new (null, typeof(int)) },
                { typeof(ushort), new (null, typeof(int)) },
                { typeof(uint), new (typeof(long), typeof(long)) },
                { typeof(float), new (typeof(double), typeof(double)) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(int?), typeof(int?)) },
                { typeof(short?), new (typeof(int?), typeof(int?)) },
                { typeof(int?), new (typeof(int?), null) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(int?), typeof(int?)) },
                { typeof(ushort?), new (typeof(int?), typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(int?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(int?)) },
                { typeof(short), new (null, typeof(int?)) },
                { typeof(int), new (null, typeof(int?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (null, typeof(int?)) },
                { typeof(ushort), new (null, typeof(int?)) },
                { typeof(uint), new (typeof(long?), typeof(long?)) },
                { typeof(float), new (typeof(double?), typeof(double?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (null, typeof(int?)) },
                { typeof(short?), new (null, typeof(int?)) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (null, typeof(int?)) },
                { typeof(ushort?), new (null, typeof(int?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(uint), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(long), typeof(long)) },
                { typeof(short), new (typeof(long), typeof(long)) },
                { typeof(int), new (typeof(long), typeof(long)) },
                { typeof(long), new (typeof(long), null) },
                { typeof(byte), new (null, typeof(uint)) },
                { typeof(ushort), new (null, typeof(uint)) },
                { typeof(ulong), new (typeof(ulong), null) },
                { typeof(float), new (typeof(double), typeof(double)) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(long?), typeof(long?)) },
                { typeof(short?), new (typeof(long?), typeof(long?)) },
                { typeof(int?), new (typeof(long?), typeof(long?)) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(uint?), typeof(uint?)) },
                { typeof(ushort?), new (typeof(uint?), typeof(uint?)) },
                { typeof(uint?), new (typeof(uint?), null) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(double?), typeof(double)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(uint?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (typeof(long?), typeof(long?)) },
                { typeof(short), new (typeof(long?), typeof(long?)) },
                { typeof(int), new (typeof(long?), typeof(long?)) },
                { typeof(long), new (typeof(long?), typeof(long?)) },
                { typeof(byte), new (null, typeof(uint?)) },
                { typeof(ushort), new (null, typeof(uint?)) },
                { typeof(uint), new (null, typeof(uint?)) },
                { typeof(ulong), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), new (typeof(double?), typeof(double?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (typeof(long?), typeof(long?)) },
                { typeof(short?), new (typeof(long?), typeof(long?)) },
                { typeof(int?), new (typeof(long?), typeof(long?)) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (null, typeof(uint?)) },
                { typeof(ushort?), new (null, typeof(uint?)) },
                { typeof(ulong?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(long), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(long)) },
                { typeof(short), new (null, typeof(long)) },
                { typeof(int), new (null, typeof(long)) },
                { typeof(byte), new (null, typeof(long)) },
                { typeof(ushort), new (null, typeof(long)) },
                { typeof(uint), new (null, typeof(long)) },
                { typeof(float), new (typeof(double), typeof(double)) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(sbyte?), new (typeof(long?), typeof(long?)) },
                { typeof(short?), new (typeof(long?), typeof(long?)) },
                { typeof(int?), new (typeof(long?), typeof(long?)) },
                { typeof(long?), new (typeof(long?), null) },
                { typeof(byte?), new (typeof(long?), typeof(long?)) },
                { typeof(ushort?), new (typeof(long?), typeof(long?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(long?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(long?)) },
                { typeof(short), new (null, typeof(long?)) },
                { typeof(int), new (null, typeof(long?)) },
                { typeof(long), new (null, typeof(long?)) },
                { typeof(byte), new (null, typeof(long?)) },
                { typeof(ushort), new (null, typeof(long?)) },
                { typeof(uint), new (null, typeof(long?)) },
                { typeof(float), new (typeof(double?), typeof(double?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), new (typeof(long?), typeof(long?)) },
                { typeof(short?), new (typeof(long?), typeof(long?)) },
                { typeof(int?), new (typeof(long?), typeof(long?)) },
                { typeof(byte?), new (typeof(long?), typeof(long?)) },
                { typeof(ushort?), new (typeof(long?), typeof(long?)) },
                { typeof(uint?), new (typeof(long?), typeof(long?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(ulong), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(byte), new (null, typeof(ulong)) },
                { typeof(ushort), new (null, typeof(ulong)) },
                { typeof(uint), new (null, typeof(ulong)) },
                { typeof(float), new (typeof(double), typeof(double)) },
                { typeof(double), new (typeof(double), null) },
                { typeof(decimal), new (typeof(decimal), null) },
                { typeof(byte?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(ushort?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(uint?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(ulong?), new (typeof(ulong?), null) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(ulong?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(byte), new (null, typeof(ulong?)) },
                { typeof(ushort), new (null, typeof(ulong?)) },
                { typeof(uint), new (null, typeof(ulong?)) },
                { typeof(ulong), new (null, typeof(ulong?)) },
                { typeof(float), new (typeof(double?), typeof(double?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(decimal), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(ushort?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(uint?), new (typeof(ulong?), typeof(ulong?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(float), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(float)) },
                { typeof(short), new (null, typeof(float)) },
                { typeof(int), new (typeof(double), typeof(double)) },
                { typeof(long), new (typeof(double), typeof(double)) },
                { typeof(byte), new (null, typeof(float)) },
                { typeof(ushort), new (null, typeof(float)) },
                { typeof(uint), new (typeof(double), typeof(double)) },
                { typeof(ulong), new (typeof(double), typeof(double)) },
                { typeof(double), new (typeof(double), null) },
                { typeof(sbyte?), new (typeof(float?), typeof(float?)) },
                { typeof(short?), new (typeof(float?), typeof(float?)) },
                { typeof(int?), new (typeof(double?), typeof(double?)) },
                { typeof(long?), new (typeof(double?), typeof(double?)) },
                { typeof(byte?), new (typeof(double?), typeof(double?)) },
                { typeof(ushort?), new (typeof(double?), typeof(double?)) },
                { typeof(uint?), new (typeof(double?), typeof(double?)) },
                { typeof(ulong?), new (typeof(double?), typeof(double?)) },
                { typeof(float?), new (typeof(float?), null) },
                { typeof(double?), new (typeof(double?), null) },
            }
        },
        {
            typeof(float?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(float?)) },
                { typeof(short), new (null, typeof(float?)) },
                { typeof(int), new (typeof(double?), typeof(double?)) },
                { typeof(long), new (typeof(double?), typeof(double?)) },
                { typeof(byte), new (null, typeof(float?)) },
                { typeof(ushort), new (null, typeof(float?)) },
                { typeof(uint), new (typeof(double?), typeof(double?)) },
                { typeof(ulong), new (typeof(double?), typeof(double?)) },
                { typeof(float), new (null, typeof(float?)) },
                { typeof(double), new (typeof(double?), typeof(double?)) },
                { typeof(sbyte?), new (typeof(float?), typeof(float?)) },
                { typeof(short?), new (typeof(float?), typeof(float?)) },
                { typeof(int?), new (typeof(double?), typeof(double?)) },
                { typeof(long?), new (typeof(double?), typeof(double?)) },
                { typeof(byte?), new (typeof(double?), typeof(double?)) },
                { typeof(ushort?), new (typeof(double?), typeof(double?)) },
                { typeof(uint?), new (typeof(double?), typeof(double?)) },
                { typeof(ulong?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
            }
        },
        {
            typeof(double), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(double)) },
                { typeof(short), new (null, typeof(double)) },
                { typeof(int), new (null, typeof(double)) },
                { typeof(long), new (null, typeof(double)) },
                { typeof(byte), new (null, typeof(double)) },
                { typeof(ushort), new (null, typeof(double)) },
                { typeof(uint), new (null, typeof(double)) },
                { typeof(ulong), new (null, typeof(double)) },
                { typeof(float), new (null, typeof(double)) },
                { typeof(sbyte?), new (typeof(double?), typeof(double?)) },
                { typeof(short?), new (typeof(double?), typeof(double?)) },
                { typeof(int?), new (typeof(double?), typeof(double?)) },
                { typeof(long?), new (typeof(double?), typeof(double?)) },
                { typeof(byte?), new (typeof(double?), typeof(double?)) },
                { typeof(ushort?), new (typeof(double?), typeof(double?)) },
                { typeof(uint?), new (typeof(double?), typeof(double?)) },
                { typeof(ulong?), new (typeof(double?), typeof(double?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
                { typeof(double?), new (typeof(double?), null) },
            }
        },
        {
            typeof(double?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(double?)) },
                { typeof(short), new (null, typeof(double?)) },
                { typeof(int), new (null, typeof(double?)) },
                { typeof(long), new (null, typeof(double?)) },
                { typeof(byte), new (null, typeof(double?)) },
                { typeof(ushort), new (null, typeof(double?)) },
                { typeof(uint), new (null, typeof(double?)) },
                { typeof(ulong), new (null, typeof(double?)) },
                { typeof(float), new (null, typeof(double?)) },
                { typeof(double), new (null, typeof(double?)) },
                { typeof(sbyte?), new (typeof(double?), typeof(double?)) },
                { typeof(short?), new (typeof(double?), typeof(double?)) },
                { typeof(int?), new (typeof(double?), typeof(double?)) },
                { typeof(long?), new (typeof(double?), typeof(double?)) },
                { typeof(byte?), new (typeof(double?), typeof(double?)) },
                { typeof(ushort?), new (typeof(double?), typeof(double?)) },
                { typeof(uint?), new (typeof(double?), typeof(double?)) },
                { typeof(ulong?), new (typeof(double?), typeof(double?)) },
                { typeof(float?), new (typeof(double?), typeof(double?)) },
            }
        },
        {
            typeof(decimal), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(decimal)) },
                { typeof(short), new (null, typeof(decimal)) },
                { typeof(int), new (null, typeof(decimal)) },
                { typeof(long), new (null, typeof(decimal)) },
                { typeof(byte), new (null, typeof(decimal)) },
                { typeof(ushort), new (null, typeof(decimal)) },
                { typeof(uint), new (null, typeof(decimal)) },
                { typeof(ulong), new (null, typeof(decimal)) },
                { typeof(sbyte?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(int?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(long?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(ushort?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(uint?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(ulong?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(decimal?), new (typeof(decimal?), null) },
            }
        },
        {
            typeof(decimal?), new Dictionary<Type, ComparisonConversion>
            {
                { typeof(sbyte), new (null, typeof(decimal?)) },
                { typeof(short), new (null, typeof(decimal?)) },
                { typeof(int), new (null, typeof(decimal?)) },
                { typeof(long), new (null, typeof(decimal?)) },
                { typeof(byte), new (null, typeof(decimal?)) },
                { typeof(ushort), new (null, typeof(decimal?)) },
                { typeof(uint), new (null, typeof(decimal?)) },
                { typeof(ulong), new (null, typeof(decimal?)) },
                { typeof(decimal), new (null, typeof(decimal?)) },
                { typeof(sbyte?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(int?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(long?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(ushort?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(uint?), new (typeof(decimal?), typeof(decimal?)) },
                { typeof(ulong?), new (typeof(decimal?), typeof(decimal?)) },
            }
        },
    };

    private static readonly IReadOnlyDictionary<Type, ISet<Type>> _convertForContainDictionary = new Dictionary<Type, ISet<Type>>
    {
        { typeof(short), new HashSet<Type> { typeof(sbyte), typeof(byte) } },
        { typeof(int), new HashSet<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort) } },
        { typeof(long), new HashSet<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint) } },
        { typeof(ushort), new HashSet<Type> { typeof(byte) } },
        { typeof(uint), new HashSet<Type> { typeof(byte), typeof(ushort) } },
        { typeof(ulong), new HashSet<Type> { typeof(byte), typeof(ushort), typeof(uint) } },
        { typeof(float), new HashSet<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) } },
        { typeof(double), new HashSet<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float) } },
        { typeof(decimal), new HashSet<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) } },
    };

    private static readonly Type StringType = typeof(string);

    internal static ComparisonConversion? GetComparisonConversion(Type left, Type right, FilterByPropertyType type)
    {
        var leftIsNullable = left.IsNullable(out var leftArgumentType);
        if (left == right)
        {
            return left.IsPrimitive || left.IsEnum || left.HasOperator(type) ||
                (leftIsNullable && (leftArgumentType!.IsPrimitive || leftArgumentType.IsEnum || leftArgumentType.HasOperator(type))) ||
                (left.IsClass && left.HasOperator(type))
                ? new (null, null)
                : null;
        }
        else if (_convertForComparisonDirectionDictionary.TryGetValue(left, out var inner) && inner.TryGetValue(right, out var conversion))
        {
            return conversion;
        }
        else
        {
            // not equal and not nullable primitive, gotta be nullable enum or struct if can be compared
            var leftUnderlyingType = leftIsNullable ? leftArgumentType : left;
            var rightUnderlyingType = right.IsNullable(out var rightArgumentType) ? rightArgumentType : right;
            if (leftUnderlyingType == rightUnderlyingType && (leftUnderlyingType.IsEnum || leftUnderlyingType.HasOperator(type)))
            {
                return leftIsNullable ? new (left, left) : new (right, right);
            }

            return null;
        }
    }

    internal static ContainConversion? GetContainConversion(Type containerType, Type itemType)
    {
        var data = containerType.GetContainerElementType();
        if (data != null)
        {
            var containerItemType = data.Value.elementType;
            if (containerItemType == itemType && (itemType == StringType || (itemType.IsValueType && (itemType.IsPrimitive || itemType.IsEnum || itemType.HasOperator(FilterByPropertyType.Equality)))))
            {
                return new (containerItemType, null, data.Value.isCollection, false);
            }

            var containerItemTypeIsNullable = containerItemType.IsNullable(out var containerItemArgumentType);
            var itemTypeIsNullable = itemType.IsNullable(out var itemArgumentType);
            var containerItemUnderlyingType = containerItemTypeIsNullable ? containerItemArgumentType : containerItemType;
            var itemUnderlyingType = itemTypeIsNullable ? itemArgumentType : itemType;
            if (_convertForContainDictionary.Contains(containerItemUnderlyingType!, itemUnderlyingType!))
            {
                return new (containerItemType, containerItemType, data.Value.isCollection, itemTypeIsNullable && !containerItemTypeIsNullable);
            }
        }

        return null;
    }

    internal static Func<TFilter, bool> BuildFilterPropertyIsDefaultFunction<TFilter>(PropertyInfo filterProperty)
        where TFilter : class
    {
        var parameter = Expression.Parameter(typeof(TFilter), "t");
        return Expression.Lambda<Func<TFilter, bool>>(Expression.Equal(Expression.Default(filterProperty.PropertyType), Expression.Property(parameter, filterProperty)), parameter).Compile();
    }
}

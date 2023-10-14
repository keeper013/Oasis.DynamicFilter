namespace Oasis.DynamicFilter.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

internal static class TypeUtilities
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, (Type?, Type?)>> _convertForComparisonDirectionDictionary = new Dictionary<Type, IReadOnlyDictionary<Type, (Type?, Type?)>>
    {
        {
            typeof(sbyte), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(short), (typeof(short), null) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (typeof(short), typeof(short)) },
                { typeof(ushort), (typeof(int), typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(sbyte?), null) },
                { typeof(short?), (typeof(short?), null) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(short?), typeof(short?)) },
                { typeof(ushort?), (typeof(int?), typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(sbyte?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(sbyte?)) },
                { typeof(short), (typeof(short?), typeof(short?)) },
                { typeof(int), (typeof(int?), typeof(int?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (typeof(short?), typeof(short?)) },
                { typeof(ushort), (typeof(int?), typeof(int?)) },
                { typeof(uint), (typeof(long?), typeof(long?)) },
                { typeof(float), (typeof(float?), typeof(float?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), (typeof(short?), null) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(short?), typeof(short?)) },
                { typeof(ushort?), (typeof(int?), typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(byte), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(short), typeof(short)) },
                { typeof(short), (typeof(short), null) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(ushort), (typeof(ushort), null) },
                { typeof(uint), (typeof(uint), null) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(short?), typeof(short?)) },
                { typeof(short?), (typeof(short?), null) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(byte?), null) },
                { typeof(ushort?), (typeof(ushort?), null) },
                { typeof(uint?), (typeof(uint?), null) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(byte?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(short?), typeof(short?)) },
                { typeof(short), (typeof(short?), typeof(short?)) },
                { typeof(int), (typeof(int?), typeof(int?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (null, typeof(byte?)) },
                { typeof(ushort), (typeof(ushort?), typeof(ushort?)) },
                { typeof(uint), (typeof(uint?), typeof(uint?)) },
                { typeof(ulong), (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), (typeof(float?), typeof(float?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (typeof(short?), typeof(short?)) },
                { typeof(short?), (typeof(short?), null) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(ushort?), (typeof(ushort?), null) },
                { typeof(uint?), (typeof(uint?), null) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(short), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(short)) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(short)) },
                { typeof(ushort), (typeof(int), typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(short?), typeof(short?)) },
                { typeof(short?), (null, typeof(short?)) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(short?), typeof(short?)) },
                { typeof(ushort?), (typeof(int?), typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(short?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(short?)) },
                { typeof(short), (null, typeof(short?)) },
                { typeof(int), (typeof(int?), typeof(int?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (null, typeof(short?)) },
                { typeof(ushort), (typeof(int?), typeof(int?)) },
                { typeof(uint), (typeof(long?), typeof(long?)) },
                { typeof(float), (typeof(float?), typeof(float?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (null, typeof(short?)) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(short?), typeof(short?)) },
                { typeof(ushort?), (typeof(int?), typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(ushort), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(int), typeof(int)) },
                { typeof(short), (typeof(int), typeof(int)) },
                { typeof(int), (typeof(int), null) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(ushort)) },
                { typeof(uint), (typeof(uint), null) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(float), null) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(int?), typeof(int?)) },
                { typeof(short?), (typeof(int?), typeof(int?)) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(ushort?), typeof(ushort?)) },
                { typeof(ushort?), (typeof(ushort?), null) },
                { typeof(uint?), (typeof(uint?), null) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(ushort?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(int?), typeof(int?)) },
                { typeof(short), (typeof(int?), typeof(int?)) },
                { typeof(int), (typeof(int?), typeof(int?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (null, typeof(ushort?)) },
                { typeof(ushort), (null, typeof(ushort?)) },
                { typeof(uint), (typeof(uint?), typeof(uint?)) },
                { typeof(ulong), (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), (typeof(float?), typeof(float?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (typeof(int?), typeof(int?)) },
                { typeof(short?), (typeof(int?), typeof(int?)) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (null, typeof(ushort?)) },
                { typeof(uint?), (typeof(uint?), null) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(int), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(int)) },
                { typeof(short), (null, typeof(int)) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(int)) },
                { typeof(ushort), (null, typeof(int)) },
                { typeof(uint), (typeof(long), typeof(long)) },
                { typeof(float), (typeof(double), typeof(double)) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(int?), typeof(int?)) },
                { typeof(short?), (typeof(int?), typeof(int?)) },
                { typeof(int?), (typeof(int?), null) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(int?), typeof(int?)) },
                { typeof(ushort?), (typeof(int?), typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(int?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(int?)) },
                { typeof(short), (null, typeof(int?)) },
                { typeof(int), (null, typeof(int?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (null, typeof(int?)) },
                { typeof(ushort), (null, typeof(int?)) },
                { typeof(uint), (typeof(long?), typeof(long?)) },
                { typeof(float), (typeof(double?), typeof(double?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (null, typeof(int?)) },
                { typeof(short?), (null, typeof(int?)) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (null, typeof(int?)) },
                { typeof(ushort?), (null, typeof(int?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(uint), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(long), typeof(long)) },
                { typeof(short), (typeof(long), typeof(long)) },
                { typeof(int), (typeof(long), typeof(long)) },
                { typeof(long), (typeof(long), null) },
                { typeof(byte), (null, typeof(uint)) },
                { typeof(ushort), (null, typeof(uint)) },
                { typeof(ulong), (typeof(ulong), null) },
                { typeof(float), (typeof(double), typeof(double)) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(long?), typeof(long?)) },
                { typeof(short?), (typeof(long?), typeof(long?)) },
                { typeof(int?), (typeof(long?), typeof(long?)) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(uint?), typeof(uint?)) },
                { typeof(ushort?), (typeof(uint?), typeof(uint?)) },
                { typeof(uint?), (typeof(uint?), null) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(double?), typeof(double)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(uint?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (typeof(long?), typeof(long?)) },
                { typeof(short), (typeof(long?), typeof(long?)) },
                { typeof(int), (typeof(long?), typeof(long?)) },
                { typeof(long), (typeof(long?), typeof(long?)) },
                { typeof(byte), (null, typeof(uint?)) },
                { typeof(ushort), (null, typeof(uint?)) },
                { typeof(uint), (null, typeof(uint?)) },
                { typeof(ulong), (typeof(ulong?), typeof(ulong?)) },
                { typeof(float), (typeof(double?), typeof(double?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (typeof(long?), typeof(long?)) },
                { typeof(short?), (typeof(long?), typeof(long?)) },
                { typeof(int?), (typeof(long?), typeof(long?)) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (null, typeof(uint?)) },
                { typeof(ushort?), (null, typeof(uint?)) },
                { typeof(ulong?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(long), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(long)) },
                { typeof(short), (null, typeof(long)) },
                { typeof(int), (null, typeof(long)) },
                { typeof(byte), (null, typeof(long)) },
                { typeof(ushort), (null, typeof(long)) },
                { typeof(uint), (null, typeof(long)) },
                { typeof(float), (typeof(double), typeof(double)) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(sbyte?), (typeof(long?), typeof(long?)) },
                { typeof(short?), (typeof(long?), typeof(long?)) },
                { typeof(int?), (typeof(long?), typeof(long?)) },
                { typeof(long?), (typeof(long?), null) },
                { typeof(byte?), (typeof(long?), typeof(long?)) },
                { typeof(ushort?), (typeof(long?), typeof(long?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(long?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(long?)) },
                { typeof(short), (null, typeof(long?)) },
                { typeof(int), (null, typeof(long?)) },
                { typeof(long), (null, typeof(long?)) },
                { typeof(byte), (null, typeof(long?)) },
                { typeof(ushort), (null, typeof(long?)) },
                { typeof(uint), (null, typeof(long?)) },
                { typeof(float), (typeof(double?), typeof(double?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(sbyte?), (typeof(long?), typeof(long?)) },
                { typeof(short?), (typeof(long?), typeof(long?)) },
                { typeof(int?), (typeof(long?), typeof(long?)) },
                { typeof(byte?), (typeof(long?), typeof(long?)) },
                { typeof(ushort?), (typeof(long?), typeof(long?)) },
                { typeof(uint?), (typeof(long?), typeof(long?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(ulong), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(byte), (null, typeof(ulong)) },
                { typeof(ushort), (null, typeof(ulong)) },
                { typeof(uint), (null, typeof(ulong)) },
                { typeof(float), (typeof(double), typeof(double)) },
                { typeof(double), (typeof(double), null) },
                { typeof(decimal), (typeof(decimal), null) },
                { typeof(byte?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(ushort?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(uint?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(ulong?), (typeof(ulong?), null) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(ulong?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(byte), (null, typeof(ulong?)) },
                { typeof(ushort), (null, typeof(ulong?)) },
                { typeof(uint), (null, typeof(ulong?)) },
                { typeof(ulong), (null, typeof(ulong?)) },
                { typeof(float), (typeof(double?), typeof(double?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(decimal), (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(ushort?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(uint?), (typeof(ulong?), typeof(ulong?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(float), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(float)) },
                { typeof(short), (null, typeof(float)) },
                { typeof(int), (typeof(double), typeof(double)) },
                { typeof(long), (typeof(double), typeof(double)) },
                { typeof(byte), (null, typeof(float)) },
                { typeof(ushort), (null, typeof(float)) },
                { typeof(uint), (typeof(double), typeof(double)) },
                { typeof(ulong), (typeof(double), typeof(double)) },
                { typeof(double), (typeof(double), null) },
                { typeof(sbyte?), (typeof(float?), typeof(float?)) },
                { typeof(short?), (typeof(float?), typeof(float?)) },
                { typeof(int?), (typeof(double?), typeof(double?)) },
                { typeof(long?), (typeof(double?), typeof(double?)) },
                { typeof(byte?), (typeof(double?), typeof(double?)) },
                { typeof(ushort?), (typeof(double?), typeof(double?)) },
                { typeof(uint?), (typeof(double?), typeof(double?)) },
                { typeof(ulong?), (typeof(double?), typeof(double?)) },
                { typeof(float?), (typeof(float?), null) },
                { typeof(double?), (typeof(double?), null) },
            }
        },
        {
            typeof(float?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(float?)) },
                { typeof(short), (null, typeof(float?)) },
                { typeof(int), (typeof(double?), typeof(double?)) },
                { typeof(long), (typeof(double?), typeof(double?)) },
                { typeof(byte), (null, typeof(float?)) },
                { typeof(ushort), (null, typeof(float?)) },
                { typeof(uint), (typeof(double?), typeof(double?)) },
                { typeof(ulong), (typeof(double?), typeof(double?)) },
                { typeof(float), (null, typeof(float?)) },
                { typeof(double), (typeof(double?), typeof(double?)) },
                { typeof(sbyte?), (typeof(float?), typeof(float?)) },
                { typeof(short?), (typeof(float?), typeof(float?)) },
                { typeof(int?), (typeof(double?), typeof(double?)) },
                { typeof(long?), (typeof(double?), typeof(double?)) },
                { typeof(byte?), (typeof(double?), typeof(double?)) },
                { typeof(ushort?), (typeof(double?), typeof(double?)) },
                { typeof(uint?), (typeof(double?), typeof(double?)) },
                { typeof(ulong?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
            }
        },
        {
            typeof(double), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(double)) },
                { typeof(short), (null, typeof(double)) },
                { typeof(int), (null, typeof(double)) },
                { typeof(long), (null, typeof(double)) },
                { typeof(byte), (null, typeof(double)) },
                { typeof(ushort), (null, typeof(double)) },
                { typeof(uint), (null, typeof(double)) },
                { typeof(ulong), (null, typeof(double)) },
                { typeof(float), (null, typeof(double)) },
                { typeof(sbyte?), (typeof(double?), typeof(double?)) },
                { typeof(short?), (typeof(double?), typeof(double?)) },
                { typeof(int?), (typeof(double?), typeof(double?)) },
                { typeof(long?), (typeof(double?), typeof(double?)) },
                { typeof(byte?), (typeof(double?), typeof(double?)) },
                { typeof(ushort?), (typeof(double?), typeof(double?)) },
                { typeof(uint?), (typeof(double?), typeof(double?)) },
                { typeof(ulong?), (typeof(double?), typeof(double?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
                { typeof(double?), (typeof(double?), null) },
            }
        },
        {
            typeof(double?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(double?)) },
                { typeof(short), (null, typeof(double?)) },
                { typeof(int), (null, typeof(double?)) },
                { typeof(long), (null, typeof(double?)) },
                { typeof(byte), (null, typeof(double?)) },
                { typeof(ushort), (null, typeof(double?)) },
                { typeof(uint), (null, typeof(double?)) },
                { typeof(ulong), (null, typeof(double?)) },
                { typeof(float), (null, typeof(double?)) },
                { typeof(double), (null, typeof(double?)) },
                { typeof(sbyte?), (typeof(double?), typeof(double?)) },
                { typeof(short?), (typeof(double?), typeof(double?)) },
                { typeof(int?), (typeof(double?), typeof(double?)) },
                { typeof(long?), (typeof(double?), typeof(double?)) },
                { typeof(byte?), (typeof(double?), typeof(double?)) },
                { typeof(ushort?), (typeof(double?), typeof(double?)) },
                { typeof(uint?), (typeof(double?), typeof(double?)) },
                { typeof(ulong?), (typeof(double?), typeof(double?)) },
                { typeof(float?), (typeof(double?), typeof(double?)) },
            }
        },
        {
            typeof(decimal), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(decimal)) },
                { typeof(short), (null, typeof(decimal)) },
                { typeof(int), (null, typeof(decimal)) },
                { typeof(long), (null, typeof(decimal)) },
                { typeof(byte), (null, typeof(decimal)) },
                { typeof(ushort), (null, typeof(decimal)) },
                { typeof(uint), (null, typeof(decimal)) },
                { typeof(ulong), (null, typeof(decimal)) },
                { typeof(sbyte?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(int?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(long?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(ushort?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(uint?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(ulong?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(decimal?), (typeof(decimal?), null) },
            }
        },
        {
            typeof(decimal?), new Dictionary<Type, (Type?, Type?)>
            {
                { typeof(sbyte), (null, typeof(decimal?)) },
                { typeof(short), (null, typeof(decimal?)) },
                { typeof(int), (null, typeof(decimal?)) },
                { typeof(long), (null, typeof(decimal?)) },
                { typeof(byte), (null, typeof(decimal?)) },
                { typeof(ushort), (null, typeof(decimal?)) },
                { typeof(uint), (null, typeof(decimal?)) },
                { typeof(ulong), (null, typeof(decimal?)) },
                { typeof(decimal), (null, typeof(decimal?)) },
                { typeof(sbyte?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(short?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(int?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(long?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(byte?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(ushort?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(uint?), (typeof(decimal?), typeof(decimal?)) },
                { typeof(ulong?), (typeof(decimal?), typeof(decimal?)) },
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

    internal static (Type?, Type?)? GetComparisonConversion(Type left, Type right, FilterByPropertyType type)
    {
        var leftIsNullable = left.IsNullable(out var leftArgumentType);
        return left == right
            ? left.IsPrimitive || left.IsEnum || left.HasOperator(type)
                || (leftIsNullable && (leftArgumentType!.IsPrimitive || leftArgumentType.IsEnum || leftArgumentType.HasOperator(type)))
                ? (null, null)
                : null
            : _convertForComparisonDirectionDictionary.TryGetValue(left, out var inner) && inner.TryGetValue(right, out var conversion)
                ? (conversion.Item1, conversion.Item2)
                : null;
    }

    internal static (Type?, bool)? GetContainConversion(Type containerType, Type itemType)
    {
        var data = containerType.GetContainerElementType();
        if (data != null)
        {
            var containerItemType = data.Value.Item1;
            if (containerItemType == itemType && (itemType == StringType || (itemType.IsValueType && (itemType.IsPrimitive || itemType.IsEnum || itemType.HasOperator(FilterByPropertyType.Equality)))))
            {
                return (null, data.Value.Item2);
            }

            var containerItemTypeIsNullable = containerItemType.IsNullable(out var containerItemArgumentType);
            var itemTypeIsNullable = itemType.IsNullable(out var itemArgumentType);
            var containerItemUnderlyingType = containerItemTypeIsNullable ? containerItemArgumentType : containerItemType;
            var itemUnderlyingType = itemTypeIsNullable ? itemArgumentType : itemType;
            if ((containerItemTypeIsNullable || !itemTypeIsNullable) && _convertForContainDictionary.Contains(containerItemUnderlyingType!, itemUnderlyingType!))
            {
                return (containerItemType, data.Value.Item2);
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

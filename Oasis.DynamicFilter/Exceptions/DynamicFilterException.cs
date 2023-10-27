namespace Oasis.DynamicFilter.Exceptions;

using System;

public abstract class DynamicFilterException : Exception
{
    protected DynamicFilterException(string message)
        : base(message)
    {
    }
}

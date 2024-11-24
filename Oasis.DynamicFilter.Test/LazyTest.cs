using Oasis.DynamicFilter.Exceptions;

namespace Oasis.DynamicFilter.Test;

public sealed class LazyTest
{
    public sealed class LazyEntity<T>
    {
        public LazyEntity(T v)
        {
            Value = v;
        }

        public T Value { get; init; }
    }

    public sealed class LazyFilter<T>
    {
        public LazyFilter(T v)
        {
            Value = v;
        }

        public T Value { get; init; }
    }

    [Fact]
    public void NotLazyNotRegistered_ShouldFail()
    {
        var filter = new FilterBuilder().Build();
        Assert.Throws<FilterNotRegisteredException>(() => filter.GetExpression<LazyEntity<int>, LazyFilter<int>>(new LazyFilter<int>(1)));
    }

    [Fact]
    public void AutoRegisterNotRegistered_ShouldSucceed()
    {
        var filter = new FilterBuilder().Build(true);
        var func = filter.GetFunc<LazyEntity<int>, LazyFilter<int>>(new LazyFilter<int>(1));
        Assert.True(func(new LazyEntity<int>(1)));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(true, null)]
    [InlineData(false, true)]
    public void LazyConfiguration_ShouldSucceed(bool defaultLazy, bool? isLazy)
    {
        var filter = new FilterBuilder(defaultLazy)
            .Configure<LazyEntity<int>, LazyFilter<int>>(isLazy)
            .ExcludeProperties(e => e.Value)
            .Filter(f => e => e.Value > f.Value)
            .Finish()
        .Build();
        var func = filter.GetFunc<LazyEntity<int>, LazyFilter<int>>(new LazyFilter<int>(2));
        Assert.False(func(new LazyEntity<int>(1)));
        Assert.True(func(new LazyEntity<int>(3)));
    }
}

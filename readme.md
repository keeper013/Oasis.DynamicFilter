# Oasis.DynamicFilter
[![latest version](https://img.shields.io/nuget/v/Oasis.DynamicFilter)](https://www.nuget.org/packages/Oasis.DynamicFilter)
[![downloads](https://img.shields.io/nuget/dt/Oasis.DynamicFilter)](https://www.nuget.org/packages/Oasis.DynamicFilter)
## Introduction
**Oasis.DynamicFilter** (referred to as **the library** in the following content) is a library that helps users (referred to as "developers" in the following document, as users of this libraries are developers) to automatically generate a linq expression/function to filter some class instances according to a filter class. Simply summarizing the feature, it helps to filter instances of one class with another.

During implementation of a web application, searching for data according to certain criteria is a common requirement, that developers provide a searching panel to let business users provide some input values, then the input value gets sent to the server side, and the input values will be used to filter data in the database, at last the filter data will be sent back to web application and response of the search request. For simple searching use cases, the search logic is always simple and similar, yet for every search developers has to repeat the tedious and brainless coding to fill it in. An example of such case is demonstrated as this example. In a library system, each book has a name and published year. To search for the book by either name or published year in the database, the code may be like the following:
```C#
// assume Book is already declared in the entity framework database context, and SearchForBookDto is the class that contains the business user input
public class Book
{
    public string Name { get; set; } = null!;
    public int PublishedYear { get; set; }
}
public class SearchForBookDto
{
    public string Name { get; set; } = null!;
    public int? PublishedYear { get; set; }
}
```
```C#
// searching logis is like this
IQueryable queryable = databaseContext.Set<Book>();
if (searchForBookDto.Name != null)
{
    queryable = queryable.Where(book => book.Name == searchForBookDto.Name);
}

if (searchForBookDto.PublishedYear != null)
{
    queryable = queryable.Where(book => book.PublishedYear == searchForBookDto.PublishedYear);
}

await queryable.ToListAsync();
```
It's easy to see in the use case that for each searching property, the logic is the same: if the property value is not null, apply it to the queryable. This is quite tedious if there are a lot of such search fields to fill in for, or there are a lot of such searching features to implement. The library helps to implement the same searching logic and simply the code to be as following:
```C#
var expressionMaker = new FilterBuilder().Register<Book, SearchForBookDto>().Build();
await databaseContext.Set<Book>().Where(expressionMaker.GetExpression<Book, SearchForBookDto>(searchForBookDto)).ToListAsync();
```
## Basic Feature
The library uses ILGenerator class provided by C# to dynamically generate the "If not null then apply the search" logic code in IL to save developers the effort. *FilterBuilder.Register<TEntity, TFilter>* method automatically tries to scan over all public instance properties of the TEntity and TFilter classes, find the property pairs with the same names, detect types of both entity and filter properties, and decide which operator it should use for the filtering. The default filtering operators applied between properties types of certain types are listed below.
Entity Property Type | Filter Property Type | Default Operation
--- | --- | ---
C# primitive numeric | C# primitive numeric | Equal
string | string | Equal/Contains
Decimal | Decimal | Equal
DateTime | DateTime | Equal
other value types (custom enum or struct if equal operator is defined) | same value type | Equal
ICollection<T> where T is string or value type | same string or value type | Contains
T[] where T is string or value type | same string or value type | Contains
some string or value type | ICollection<T> where T is the same string or value type | In
some string or value type | T[] where T is the same string or value type | In

Note that if the property types are not suitable for any operator, the property pair will be silently ignored by the library when filtering.
When both etity and property types are string, the default operator is by default *Equal*, but configurable. This will be elaborated in the later sections.
Note that for C# primitive numeric values comparison, the types don't need to be exactly the same, like comparison between an *int* and a *long* is allowed, also that between a *sbyte* and *ushort*. As long as C# allows the comparison, the library supportes it. But for custom defined enums structs and classes, comparison is only allowed to happen between instances of the same type, for structs and classes, the corresponding compairson operator must exist as well.
In the above table, "In" logic represents simply the reverse of contains, that if the string or numeric value is in the collection or array. Developers can call register for multiple times to register filtering relationship between multiple class pairs, when finished call *FilterBuilder.Build* method to finish it get the *IFilter* instance that generates expressions.
To get a function to filter IEnumerable instances, developers can call *IFilter.GetFunction<TEntity, TFilter>* instead of calling *IFilter.GetExpression<TEntity, TFilter>*, or they can simple call *Compile* method from the expression to get the function, they are essentially the same.
## Extension Features
To fit the library with more proper use cases, the library allows users to filter with common comparison filterings and *not* operation.
### Filtery by Property
*FilterBuilder* clas provides a *Configure<TFilter, TEntity>* method for customizing the filtering like below:
```C#
// the below code is equivalent to var expressionMaker = new FilterBuilder().Register<Book, SearchForBookDto>().Build();
var expressionMaker = new FilterBuilder()
    .Configure<Book, SearchForBookDto>()
        .FilterByProperty(book => book.Name, Operator.Equality, dto => dto.Name, null, null, null)
        .FilterByProperty(book => book.PublishedYear, Operator.Equality, dto => dto.PublishedYear, null, null, null)
    .Finish()
.Build()
```
Definition of *FilterByProperty* method is like the following:
```C#
public IFilterConfigurationBuilder<TEntity, TFilter> FilterByProperty<TEntityProperty, TFilterProperty>(
    Expression<Func<TEntity, TEntityProperty>> entityPropertyExpression,
    Operator type,
    Expression<Func<TFilter, TFilterProperty>> filterPropertyExpression,
    Func<TFilter, bool>? includeNull = null,
    Func<TFilter, bool>? reverseIf = null,
    Func<TFilter, bool>? ignoreIf = null)
```
The first 3 parameters of *FilterByProperty* methods forms a "entity property is equal/greater than/less than or equal filter property" pattern. Note that *Register* method by default only match properties with the same names, but *Configure* method allows developers to pair properties with different names. With *Configure* method called, the library will still try to match the rest properties with same names and generate relevant comparing logic, if they are not included by the *Configure* method.
*FilterBy* enum representing the filtering type supports the following comparisons:
Name | Meaning
--- | ---
Equality | =
InEquality | !=
GreaterThan | >
GreaterThanOrEqual | >=
LessThan | <
LessThenOrEqual | <=
Contains | contains
NotContains | not contains
In | contained in
NotIn | not contained in

So if developers wants *SearchForBookDto.PublishedYear property to filter for books that aren't pubilshed in that year, they can simply replace the *FilterBy.Equality* with *FilterBy.InEquality* instead.
The last 3 input parameters are 3 functions to further dynamically customize the filtering, they all take *TFilter* type as input parameter, so developers may add some addicitonal properties or methods in the type to help customizing the filtering. They have default values to be null.

*includeNull* function decides if property entity is null, whether it is cosnidered "Positive" in the filtering. In this example, if we do the following for *Name* property (note that "null" becomes "f => true"):
```C#
.FilterByProperty(b => b.Name, FilterBy.Equality, dto => dto.Name, f => true, dto => false, null)
```
Then the filter will include any *Book* instance with null as its name. If this method returns true, it means if entity property is null, then it will be included in the filtering result; false meaning if entity property is null, it will not be included in the filtering result. And if the method is null, it means if the entity property is null, it will only be included in the filtering result if the null value satisfies the filtering type (equal, not equal, in not in, contains, not contains). Note that *includeNull* method is only meant for the TEntity property, not the TFilter property.

*reverseIf* method decides whether to reverse the filtering result. If it returns true, the result of the whole filtering will be reverted, it takes a *TFilter* type as input parameter to allow developers dynamically decides whether to revert the filtering result with certain TFilter properties or methods. Note that result of *includeNull* method will be reverted by this method, too, if this method returns true (e.g. if entity property is null, and includeNull returns true, then by default the entity will be include in the filtering result, but if refertIf returns true, then entity will not be included. Hence result of *includeNull* is reverted by *revertIf*).

*ignoreIf* method decides whether to ignore this property pair when filtering (meaning this property pair will not be used during the filtering). By default if filtering property is nullable or an interface/class, and value of this parameter is null, it will be automatically assigned a default function to ignore the property pair if filtering property is null, it's a shortcut designed to encourages developers to use null values to represent "If is null then ignore this property" pattern. Developers will have to overwrite this function if they don't want the default behavior.
### Filter by String Property
To support filtering by string property with more options, the library provides *FilterConfiguration.FilterByStringProperty* method. Definition of the method is like below:
```C#
public IFilterConfigurationBuilder<TEntity, TFilter> FilterByStringProperty(
    Expression<Func<TEntity, string?>> entityPropertyExpression,
    StringOperator type,
    Expression<Func<TFilter, string?>> filterPropertyExpression,
    Func<TFilter, bool>? includeNull = null,
    Func<TFilter, bool>? reverseIf = null,
    Func<TFilter, bool>? ignoreIf = null)
```
Options of *StringOperator* is listed below:
Name | Meaning
--- | ---
Equality | entity property string equals filter property string
InEquality | entity property string does not equal filter property string
Contains | entity property string contains filter property string
NotContains | entity property string does not contain filter property string
In | filter property contains entity property string
NotIn | filter property does not contain entity property string
StartsWith | entity property string starts with filter property string
NotStartsWith | entity property string does not start with filter property string
EndsWith | entity property string ends with filter property string
NotEndsWith | entity property string does not end with filter property string

For filtering a string type with a string type, the default operator is configurable by the parameter of *FilterBuilder* constrctor. If ignored, the default value is false, and the operator will be *Equal*, or else if set to true, then the default value will be *Contains*, which means property string of entity contains property string of filter. This parameter will help simply the registration code for certain systems if their most search cases need to filter entities with a partial of the property value. With *new FilterBuilder(true)*, developers won't have to specify *FilterByStringProperty(entity => entity.Value, StringOperator.Contains, filter.Value)* if there are really a lot of such cases.

Note that case-insensitive option is not provided in this method, as it's not supported by Linq to SQL.
### Filter by Range
The library also provides a maybe-not-ofter-used feature to allow developers to filter by range. Consider the use case above, if developers want to filter books published between year 2000 and 2020, they can declare the filter class like below:
```C#
public class FilterBookByYearRangeDto
{
    public int From { get; set; }
    public int To { get; set; }
}
```
Then configure the filtering like below:
```C#
var expressionMaker = new FilterBuilder()
    .Configure<Book, FilterBookByYearRangeDto>()
        .FilterByRangedFilter(dto => dto.From, RangeOperator.LessThanOrEqual, book => book.PublishedYear, RangeOperator.LessThanOrEqual, dto => dto.To, null, null, null)
    .Finish()
.Build()
```
Note that *FilterByRange* enum only has two values: *LessThan* and *LessThanOrEqual*, so first 5 input parameters of *FilterByRangedFilter* method forms a "min < value < max" pattern. The 3 function parameters of *FilterByRangedFilter* is similar to those of *FilterByProperty* method.
In case developers want to see if a value in TFilter is between min and max properties of a TEntity (which may be a rare case), the library also provides a method named *FilterByRangedFilter* for this purpose. The usage is similar to *FilterByRangedFilter*, only with comparison direction to be opposite.
## Possible Improvements and Futher Ideas
currently only filtering with properties directly on classes is allowed, that with properties on properties is not allowed (e.g. if developer want to find all books satisifies the condition Book.Author.Name = "<somebody>"). Supporting this may be hard, but if possible may be considered in the future.
## Feedback
There there be any questions or suggestions regarding the library, please send an email to keeper013@gmail.com for inquiry. When submitting bugs, it's preferred to submit a C# code file with a unit test to easily reproduce the bug.

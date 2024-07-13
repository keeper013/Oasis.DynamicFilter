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
The library uses ILGenerator class provided by C# to dynamically generate the "If not null then apply the search" logic code in IL to save developers the effort. *FilterBuilder.Register<TEntity, TFilter>* method automatically tries to scan over all public instance properties of the TEntity and TFilter classes, find the property pairs with the same names, detect types of both entity and filter properties, and apply default operation it should use for the filtering. The default filtering operation applied between properties types of certain types are listed below.
Entity Property Type | Filter Property Type | Default Operation
--- | --- | ---
C# primitive numeric | C# primitive numeric | Equal
string | string | Equal
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
## Custom Filtering Configuration
To fit the library with more general use cases, the library allows users to filter with custom configurations. For a more complicated use case to filter books with a partial of author name and author age when the book is published:
```C#
public sealed class Book
{
    public int PublishedYear { get; set; }

    public string Name { get; set; } = null!;

    public Author Author { get; set; } = null!;
}

public sealed class Author
{
    public int BirthYear { get; set; }

    public string Name { get; set; } = null!;
}

public sealed class BookFilter
{
    public int? PublishedYear { get; set; } = default!;

    public string? Name { get; set; }
}
```
To get all books from an author whose name contains string "John" and whose age is below 40 when the book is published, the following code should get the expression:
```C#
var expressionMaker = new FilterBuilder()
    .Configure<Book, AuthorFilter>()
        .Filter(filter => book => string.IsNullOrEmpty(filter.AuthorName) || book.Author.Name.Contains(filter.AuthorName))
        .Filter(filter => book => !filter.Age.HasValue || book.PublishedYear - book.Author.BirthYear < filter.Age)
        .Finish()
.Build();

var filter = new AuthorFilter { Age = 40, AuthorName = "John" };
var exp = expressionMaker.GetExpression<Book, AuthorFilter>(filter);
```
Note that the custom filtering expressions uses or operation to implement the behavior that if filter.AuthorName is empty, any author name will be included, and if filter.Age is empty, any author age when book was published will be included.
The filtering conditions in the 2 .Filter calls will be combined with "And" operator when filtering. Of course we can use a single .Filter call to implement the same behavior: .Filter(filter => book => (string.IsNullOrEmpty(filter.AuthorName) || book.Author.Name.Contains(filter.AuthorName)) && (!filter.Age.HasValue || book.PublishedYear - book.Author.BirthYear < filter.Age)).
This feature allows developers to filter entities with more complicated expressions rather than only with existing property values, which adds a lot of flexibility for developers and makes the library way more powerful.
Note that to make use of this feature when querying data in database, the expression passed in for entity property must be supported by Linq to SQL.
## Feedback
There there be any questions or suggestions regarding the library, please send an email to keeper013@gmail.com for inquiry. When submitting bugs, it's preferred to submit a C# code file with a unit test to easily reproduce the bug.

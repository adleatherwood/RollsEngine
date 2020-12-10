# RollsEngine

A abstract query execution engine based on JsonPath syntax

## Installation

```
todo nuget line
```

## Example Queries

Given a set of JSON documents:

```json
{ "id": 1, "name": "Fred Thimbleberry", "age": 23, "roles": [ { "title": "employee" } ] },
{ "id": 2, "name": "Tim Burklebunny",   "age": 45, "roles": [ { "title": "manager"  }, { "title": "employee" } ] },
{ "id": 3, "name": "Steve Forklemeyer", "age": 45, "roles": [ { "title": "employee" } ] }
```
You can write queries using a SQL-like syntax with JsonPath expression to pull data elements
from the documents.

```sql
SELECT {
    'employee': $.name,
    'isManager': ANY($.roles[?(@.title == 'manager')]),
}
FROM employees
KEYS 1, 2, 3
ORDER BY $.name DESC
LIMIT 2
```
Will give you a result that looks like so
```json
{ "name": "Tim Burklebunny", "isManager": true },
{ "name": "Steve Forklemeyer", "isManager": false }
{ "name": "Fred Thimbleberry", "isManager": false }
```

## Some Details

The main query engine is abstracted from any particular JSON engine.  So the `RollsEngine` NuGet package
 is not usable on it's own without some of the abstractions filled in.  This is where the secondary packages
such as `RollsEngine.Newtonsoft` come in.  That package is a Newtonsoft JSON specific implementation of
the required abstractions.

The main library only has one abstraction:

```cs
public interface IDataService<TObject>
{
    TObject New();
    decimal Number(IComparable comparable);
    IComparable Path(TObject document, string path);
    TObject Add(TObject document, string name, object value);
    IEnumerable<TObject> Read(string from);
    IEnumerable<TObject> Read(string from, string[] keys);
    IComparable Execute(TObject document, string name, object[] parameters);
}
```

The Newtonsoft library has most of this interface filled in for you with the `NewtonsoftService` class.
But the `Read` methods will always be custom.  So this is isolated into a smaller interface for the
developer to implement:

```cs
public interface INewtonsoftSource
{
    IEnumerable<JObject> Read(string from);
    IEnumerable<JObject> Read(string from, string[] keys);
}
```

So a typical arrangement for executing a query would look something like this:

```cs
var reader = new MyReader(); // implements INewtonsoftSource
var service = new NewtonsoftService(reader);

RollsQuery.Execute(service, "SELECT $ FROM my-data-source-name");
```
You can also add a custom function handler if you want to add your own functions.
```cs
var reader = new MyReader();       // implements INewtonsoftSource
var functions = new MyFunctions(); // implements INewtonsoftFunction
var service = new NewtonsoftService(reader, functions);

RollsQuery.Execute(service, "SELECT myFunc($) FROM my-data-source-name");
```

## Available Functions

---
### String Functions
---
| Name   | Parameters | Return
 ------- |----------- | ------
| SUB    | value (string), start (int), optional length (int) | string
| RTRIM  | value (string) | string
| LTRIM  | value (string) | string
| TRIM   | value (string) | string
| UPPER  | value (string) | string
| LOWER  | value (string) | string
| STARTS | value (string), optional ignoreCase (bool) | bool
| SPLIT  | value (string), delimiter (string) | array
| JOIN   | delimiter (string), value (string) | string

---
### Array Functions
---
| Name   | Parameters | Return
 ------- |----------- | ------
| ANY    | values (array) | bool
| FIRST  | values (array) | value
| LAST   | values (array) | value
| AT     | values (array), index (int) | value
| LENGTH | values (array) | int

---
### Date Time Functions
---

| Name   | Parameters | Return
 ------- |----------- | ------
| NOW    | n/a               | string (e.g. '2009-06-15T13:45:30.0000000-07:00')
| UTC    | n/a               | string (e.g. '2009-06-15T13:45:30.0000000Z')
| YEAR   | datetime (string) | int
| MONTH  | datetime (string) | int
| DAY    | datetime (string) | int
| HOUR   | datetime (string) | int
| MINUTE | datetime (string) | int
| SECOND | datetime (string) | int

---
### Aggregate Functions
---
| Name   | Parameters | Return
 ------- |----------- | ------
| COUNT  | $          | int
| SUM    | int        | int
| MIN    | int        | int
| MAX    | int        | int
| AVG    | int        | int

# JsonExtensionDataPatchDocumentAdapter

is a .NET package that transforms operation paths in JsonPatchDocuments such that they match the annotated JsonExtensionData property.

## Installation

Install it from nuget [JsonPatchDocumentExtensionDataAdapter](https://www.nuget.org/packages/JsonPatchDocumentExtensionDataAdapter ):

```bash
dotnet add package JsonPatchDocumentExtensionDataAdapter 
```

| Version | Number                                                                              |
|---------|-------------------------------------------------------------------------------------|
| Stable  | ![Nuget Package](https://badgen.net/nuget/v/JsonPatchDocumentExtensionDataAdapter ) |

## Motivation
Assume you have a model class that uses the [`JsonExtensionData` attribute](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonextensiondataattribute?view=net-8.0) to store additional properties that are not part of the model. 

```csharp
class MyClass
{
    public int? Foo { get; set; }
    public string? Bar { get; set; }

    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object>? MyExtensionData { get; set; }
}
```

Assume, you have an ASP.NET Core Server, that consumes `JsonPatchDocument<MyClass>` to update the model with an [RFC6902](https://www.rfc-editor.org/rfc/rfc6902) HTTP PATCH request ([docs](https://learn.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-8.0#json-patch-example)):

```csharp
public IActionResult<MyClass> Patch([FromBody] JsonPatchDocument<MyClass> patch)
{
    var myClass = new MyClass();
    patch.ApplyTo(myClass);
    return Ok(myClass);
}
```

If the client sends a PATCH request with the following body to your server:

```json
[
    {
        "op": "add",
        "path": "/AnUnmappedProperty",
        "value": "MyValue"
    }
]
```
The `.ApplyTo(...)` [will fail](https://github.com/dotnet/aspnetcore/issues/57711) with a `JsonPatchException`:
> The target location specified by path segment 'AnUnmappedProperty' was not found.

If the client sent the following body instead:

```json
[
    {
        "op": "add",
        "path": "/MyExtensionData/AnUnmappedProperty",
        "value": "MyValue"
    }
]
```
it would work, but the client doesn't and shouldn't know about the `MyExtensionData` property as it's transparent to the client.

## Use of this library
This library provides an adapter that transforms the paths in a given `JsonPatchDocument` such that they match the `JsonExtensionData` property. 

```csharp
using JsonExtensionDataPatchDocumentAdapter;
// ...

public IActionResult<MyClass> Patch([FromBody] JsonPatchDocument<MyClass> patch)
{
    var myClass = new MyClass();
    var modifiedPatch = patch.Adapt(x=>x.MyExtensionData, myClass);
    modifiedPatch.ApplyTo(myClass); // no longer raises a JsonPatchException
    return Ok(myClass);
}
```

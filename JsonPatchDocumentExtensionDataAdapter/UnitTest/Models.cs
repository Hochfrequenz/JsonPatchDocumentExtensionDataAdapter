namespace UnitTest;

/// <summary>
/// class that has the JsonExtensionData attribute on root level
/// </summary>
internal class MyClass
{
    public int? Foo { get; set; }
    public string? Bar { get; set; }

    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object>? MyExtensionData { get; set; }
}

/// <summary>
/// class that has the JsonExtensionData attribute _not_ on root level
/// </summary>
internal class MyClassWithNesting
{
    public int MyInteger { get; set; }

    public string? MyString { get; set; }

    public MyClass? MyModel { get; set; }
}

/// <summary>
/// similar to <see cref="MyClass"/> but with JsonPropertyName attributes
/// </summary>
internal class MyClassWithJsonNameAttributes
{
    [System.Text.Json.Serialization.JsonPropertyName("somethingLikeFoo")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "somethingLikeFoo")]
    public int Foo { get; set; }
    public string? Bar { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("thePropertyWithTheExtensionData")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "thePropertyWithTheExtensionData")]
    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object>? MyExtensionData { get; set; }
}

/// <summary>
/// Similar to <see cref="MyClassWithNesting"/> but with Newtonsoft and STJ attributes
/// </summary>
internal class MyClassWithNestingAndJsonNameAttribute
{
    [System.Text.Json.Serialization.JsonPropertyName("mySAdasdasdInteger")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "mySAdasdasdInteger")]
    public int MyInteger { get; set; }

    public string? MyString { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("moooooodel")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "moooooodel")]
    public MyClassWithJsonNameAttributes? MyModel { get; set; }
}

/// <summary>
/// similar to <see cref="MyClass"/> but with JsonPropertyName attributes
/// </summary>
internal class MyClassWithInconsistentJsonNameAttributes
{
    [System.Text.Json.Serialization.JsonPropertyName("somethingLikeFoo")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "somethingLikeFoo")]
    public int Foo { get; set; }
    public string? Bar { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("thePropertyWithTheExtensionData")] // this name is inconsistent but not relevant as it's the extension data themselves
    // no newtonsoft attribute here
    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object>? MyExtensionData { get; set; }
}

/// <summary>
/// similar to <see cref="MyClassWithNestingAndJsonNameAttribute"/> but with inconsistent/missing Newtonsoft attributes
/// </summary>
internal class MyClassWithNestingAndInconsistentJsonNameAttribute
{
    [System.Text.Json.Serialization.JsonPropertyName("mySAdasdasdInteger")]
    // no newtonsoft attribute here
    public int MyInteger { get; set; }

    public string? MyString { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("moooooodel")]
    [Newtonsoft.Json.JsonProperty(PropertyName = "muuuuuudel")]
    public MyClassWithJsonNameAttributes? MyModel { get; set; }
}

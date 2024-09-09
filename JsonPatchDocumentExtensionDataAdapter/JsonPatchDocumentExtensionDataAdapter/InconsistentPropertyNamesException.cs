using System;
using Newtonsoft.Json;

namespace JsonExtensionDataPatchDocumentAdapter;

/// <summary>
/// an error that is raised if <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> and <see cref="JsonPropertyAttribute.PropertyName"/> don't match
/// </summary>
/// <remarks>It's important for the models to use consistent property names because ASP.Net Core internally relies on Newtonsoft</remarks>
public class InconsistentPropertyNamesException : Exception
{
    /// <summary>
    /// name of the property
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// json name of the property using Newtonsoft
    /// </summary>
    public string? NewtonsoftJsonPropertyName { get; }

    /// <summary>
    /// json name of the property using System.Text.Json
    /// </summary>
    public string? SystemTextPropertyName { get; }

    internal InconsistentPropertyNamesException(
        string propertyName,
        string? newtonsoftJsonPropertyName = null,
        string? systemTextPropertyName = null
    )
        : base(
            $"The system.text.property name {systemTextPropertyName ?? "(unset)"} and the newtonsoft json property{newtonsoftJsonPropertyName ?? "(unset)"} name don't match for property {propertyName}. Because the logic of this package relies on System.Text.Json but ASP.NET Core relies on Newtonsoft internally, both have to match"
        )
    {
        PropertyName = propertyName;
        NewtonsoftJsonPropertyName = newtonsoftJsonPropertyName;
        SystemTextPropertyName = systemTextPropertyName;
    }
}

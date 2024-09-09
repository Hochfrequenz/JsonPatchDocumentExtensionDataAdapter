using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Serialization;

namespace JsonExtensionDataPatchDocumentAdapter;

/// <summary>
/// Assume <typeparamref name="TModel"/> has a property that is annotated with the <see cref="System.Text.Json.Serialization.JsonExtensionDataAttribute"/>.
/// The client does not know of this property and its behaviour.
/// If the client sends a <see cref="JsonPatchDocument{TModel}"/> in which it attempts to change properties which are internally stored in the extension data property on the server side,
/// this class modifies the patch document such that <see cref="Operation{TModel}"/>s with unknown paths are converted to paths that point to the annotated extension data property.
/// There are extensive unittests for this class.
/// </summary>
/// <remarks>
/// I'd love to just drop this piece of code entirely, because my gut feeling is, there must be a proper solution to the problem.
/// I opened an issue at ASP.NET Core: https://github.com/dotnet/aspnetcore/issues/57711, maybe one day it's solved or someone suggests the 'right' way to do it.
/// </remarks>
public class JsonPatchDocumentExtensionDataAdapter<TModel>
    where TModel : class
{
    /// <summary>
    /// path to the property of <typeparamref name="TModel"/>that is annotated with the <see cref="System.Text.Json.Serialization.JsonExtensionDataAttribute"/>
    /// </summary>
    protected readonly string ExtensionDataPropertyJsonPath;

    /// <summary>
    /// accessor for the property of <typeparamref name="TModel"/>that is annotated with the <see cref="System.Text.Json.Serialization.JsonExtensionDataAttribute"/>
    /// </summary>
    protected readonly Expression<
        Func<TModel, IDictionary<string, object>?>
    > ExtensionDataPropertyExpression;

    /// <summary>
    /// initialize the class by providing an expression that points to the annotated property
    /// </summary>
    /// <param name="extensionDataPropertyExpression">how to get from <typeparamref name="TModel"/>to the extension data</param>
    public JsonPatchDocumentExtensionDataAdapter(
        Expression<Func<TModel, IDictionary<string, object>?>> extensionDataPropertyExpression
    )
    {
        ExtensionDataPropertyExpression = extensionDataPropertyExpression;
        ExtensionDataPropertyJsonPath = GetJsonPathOfAnnotatedProperty(
            extensionDataPropertyExpression
        );
    }

    /// <summary>
    /// Get the JSON property path from the <paramref name="propertyExpression"/>
    /// </summary>
    /// <param name="propertyExpression">The expression pointing to the property.</param>
    /// <returns>The full JSON property path.</returns>
    private string GetJsonPathOfAnnotatedProperty(
        Expression<Func<TModel, IDictionary<string, object>?>> propertyExpression
    )
    {
        // List to store the path segments
        var pathSegments = new List<string>();

        // Traverse the expression tree and collect property names
        var expression = propertyExpression.Body;

        while (expression is MemberExpression memberExpression)
        {
            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo != null)
            {
                // Check if the property has a JsonPropertyName attribute
                var systemTextJsonPropertyNameAttribute =
                    propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
                var newtonsoftJsonPropertyAttribute =
                    propertyInfo.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
                var extensionDataAttribute =
                    propertyInfo.GetCustomAttribute<JsonExtensionDataAttribute>();
                bool propertyJsonNameIsRelevant = extensionDataAttribute is null;
                if (propertyJsonNameIsRelevant)
                {
                    if (
                        newtonsoftJsonPropertyAttribute?.PropertyName is not null
                        && systemTextJsonPropertyNameAttribute is null
                        && !string.Equals(
                            newtonsoftJsonPropertyAttribute.PropertyName,
                            propertyInfo.Name,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                    {
                        throw new InconsistentPropertyNamesException(
                            propertyInfo.Name,
                            newtonsoftJsonPropertyAttribute.PropertyName,
                            null
                        );
                    }

                    if (
                        systemTextJsonPropertyNameAttribute is not null
                        && newtonsoftJsonPropertyAttribute is null
                        && !string.Equals(
                            systemTextJsonPropertyNameAttribute.Name,
                            propertyInfo.Name,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                    {
                        throw new InconsistentPropertyNamesException(
                            propertyInfo.Name,
                            null,
                            systemTextJsonPropertyNameAttribute.Name
                        );
                    }

                    if (
                        newtonsoftJsonPropertyAttribute?.PropertyName is not null
                        && systemTextJsonPropertyNameAttribute is not null
                        && !string.Equals(
                            newtonsoftJsonPropertyAttribute.PropertyName!,
                            systemTextJsonPropertyNameAttribute.Name,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                    {
                        throw new InconsistentPropertyNamesException(
                            propertyInfo.Name,
                            newtonsoftJsonPropertyAttribute.PropertyName,
                            systemTextJsonPropertyNameAttribute.Name
                        );
                    }
                }

                var jsonPropertyName =
                    newtonsoftJsonPropertyAttribute?.PropertyName
                    ?? systemTextJsonPropertyNameAttribute?.Name
                    ?? propertyInfo.Name;

                pathSegments.Insert(0, jsonPropertyName);
            }

            expression =
                memberExpression.Expression
                ?? throw new ArgumentNullException(
                    nameof(memberExpression),
                    "The expression inside the property expression must not be null"
                );
        }

        if (pathSegments.Count == 0)
        {
            throw new ArgumentException("Invalid expression. Expected a property expression.");
        }

        return "/" + string.Join("/", pathSegments);
    }

    private static string GetLongestCommonPrefixIgnoreCase(string str1, string str2)
    {
        // Convert both strings to lowercase for case-insensitive comparison
        var lowerStr1 = str1.ToLower();
        var lowerStr2 = str2.ToLower();

        var minLength = Math.Min(lowerStr1.Length, lowerStr2.Length);
        var i = 0;

        // Compare characters one by one
        while (i < minLength && lowerStr1[i] == lowerStr2[i])
        {
            i++;
        }

        // Return the common prefix from the original case-sensitive string
        return str1.Substring(0, i);
    }

    private string GetPathRelativeToExtensionData(string originalPath)
    {
        var longestSubPath = GetLongestCommonPrefixIgnoreCase(
            ExtensionDataPropertyJsonPath,
            originalPath
        );
        var result = originalPath[longestSubPath.Length..];
        return result;
    }

    /// <summary>
    /// sanitizes the <paramref name="operation"/>
    /// </summary>
    /// <param name="operation">instance which will be modified</param>
    private void SanitizeOperation(Operation<TModel> operation)
    {
        if (
            !operation.path.StartsWith(
                ExtensionDataPropertyJsonPath,
                StringComparison.InvariantCultureIgnoreCase
            )
        )
        {
            return;
        }
        if (operation.value is JsonValue jsonValue)
        {
            operation.value = jsonValue.ToString();
        }
    }

    /// <summary>
    /// Returns a new JsonPatchDocument in which the <see cref="OperationBase.path"/> of the <see cref="JsonPatchDocument{TModel}.Operations"/>
    /// have been adapted such that they point to the extension data
    /// </summary>
    /// <param name="document">Document from the client which isn't aware of the ExtensionData</param>
    /// <param name="model">The model to which the document should be applied. It won't be modified but is necessary to distinguish between ExtensionData that are already there and which would be newly added.</param>
    public JsonPatchDocument<TModel> TransformDocument(
        JsonPatchDocument<TModel> document,
        in TModel model
    )
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }
        var testModel = System.Text.Json.JsonSerializer.Deserialize<TModel>(
            System.Text.Json.JsonSerializer.Serialize(model)
        );
        if (testModel is null)
        {
            throw new ArgumentException(
                "The model must be serializable with System.Text.json",
                nameof(model)
            );
        }
        var result = new JsonPatchDocument<TModel>();
        var expressionFunc = ExtensionDataPropertyExpression.Compile();
        var extensionData = expressionFunc(testModel) ?? new Dictionary<string, object>();
        var extensionDataAlreadyExists = extensionData.Any();
        JsonPatchError? applyError = null;
        foreach (var originalOperation in document.Operations)
        {
            applyError = null;
            originalOperation.Apply(
                testModel,
                new ObjectAdapter(
                    new DefaultContractResolver(),
                    error =>
                    {
                        applyError = error;
                    }
                )
            );
            if (applyError is null)
            {
                result.Operations.Add(originalOperation);
                continue;
            }

            var originalError = applyError!;
            var extensionDataKey = GetPathRelativeToExtensionData(originalOperation.path);
            if (extensionDataAlreadyExists)
            {
                Operation<TModel> newOperation;
                if (!extensionData.TryAdd(extensionDataKey, originalOperation.value))
                {
                    var replacementPath =
                        ExtensionDataPropertyJsonPath
                        + "/"
                        + GetPathRelativeToExtensionData(originalOperation.path);
                    newOperation = new Operation<TModel>(
                        op: "replace",
                        path: replacementPath,
                        from: null,
                        value: originalOperation.value
                    );
                }
                else
                {
                    var addPath =
                        ExtensionDataPropertyJsonPath
                        + "/"
                        + GetPathRelativeToExtensionData(originalOperation.path);
                    newOperation = new Operation<TModel>(
                        op: originalOperation.op,
                        path: addPath,
                        from: originalOperation.from,
                        value: originalOperation.value
                    );
                }

                applyError = null;
                newOperation.Apply(
                    testModel,
                    new ObjectAdapter(
                        new DefaultContractResolver(),
                        error =>
                        {
                            applyError = error;
                        }
                    )
                );
                if (applyError is not null)
                {
                    throw new InvalidOperationException(
                        $"The operation {originalOperation} could neither be applied to the model ({originalError.ErrorMessage}) nor be adapted to match the JsonExtensionData '{ExtensionDataPropertyJsonPath}' ({applyError.ErrorMessage})"
                    );
                }

                result.Operations.Add(newOperation);
            }
            else
            {
                extensionData.Add(extensionDataKey, originalOperation.value);
            }
        }

        var useOneOperationToAddEntireExtensiondata = !extensionDataAlreadyExists;
        if (useOneOperationToAddEntireExtensiondata)
        {
            var newOperation = new Operation<TModel>(
                op: "add",
                path: ExtensionDataPropertyJsonPath,
                null,
                extensionData
            );
            applyError = null;
            newOperation.Apply(
                testModel,
                new ObjectAdapter(
                    new DefaultContractResolver(),
                    error =>
                    {
                        applyError = error;
                    }
                )
            );
            if (applyError is not null)
            {
                throw new InvalidOperationException(
                    $"The JsonExtensionData '{ExtensionDataPropertyJsonPath}' could not be added: {applyError!.ErrorMessage}"
                );
            }

            result.Operations.Add(newOperation);
        }
        foreach (var operation in result.Operations)
        {
            SanitizeOperation(operation);
        }

        return result;
    }
}

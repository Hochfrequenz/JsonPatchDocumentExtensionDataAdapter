using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch;

namespace JsonExtensionDataPatchDocumentAdapter;

/// <summary>
/// allows to invoke the logic as extension method
/// </summary>
public static class JsonPatchDocumentExtensionMethods
{
    /// <summary>
    /// adapts a given <see cref="originalPatchDocument"/> such that the paths are adapted to the data at <see cref="extensionDataPropertyExpression"/>.
    /// </summary>
    /// <param name="originalPatchDocument">the document that shall be adapted; it won't be modified</param>
    /// <param name="extensionDataPropertyExpression"></param>
    /// <param name="model">state of the model to which the <paramref name="originalPatchDocument"/> shall be applied; This is necessary to check if the extension data are null or already carry values.</param>
    /// <typeparam name="T">the patchable model type</typeparam>
    /// <returns>a new patch document instance with adapted paths</returns>
    public static JsonPatchDocument<T> Adapt<T>(
        this JsonPatchDocument<T> originalPatchDocument,
        Expression<Func<T, IDictionary<string, object>?>> extensionDataPropertyExpression,
        in T model
    )
        where T : class
    {
        var adapter = new JsonPatchDocumentExtensionDataAdapter<T>(extensionDataPropertyExpression);
        var result = adapter.TransformDocument(originalPatchDocument, model);
        return result;
    }
}

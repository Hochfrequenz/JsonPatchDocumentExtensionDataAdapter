using FluentAssertions;
using JsonExtensionDataPatchDocumentAdapter;
using Newtonsoft.Json;

namespace UnitTest;

/// <summary>
/// tests that an exception is raised if the <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> and <see cref="JsonPropertyAttribute.PropertyName"/> differ
/// </summary>
[TestClass]
public class InconsistencyTests
{
    [TestMethod]
    public void Inconsistent_Serialization_Settings_Raise_Error()
    {
        var instantiatingWithInvalidAttributes = () =>
            new JsonPatchDocumentExtensionDataAdapter<MyClassWithNestingAndInconsistentJsonNameAttribute>(
                x => x.MyModel!.MyExtensionData
            );
        instantiatingWithInvalidAttributes
            .Should()
            .ThrowExactly<InconsistentPropertyNamesException>();
    }

    [TestMethod]
    public void Inconsistent_Serialization_Settings_Raise_NoError_On_ExtensionData_Name()
    {
        var instantiatingWithInvalidAttributes = () =>
            new JsonPatchDocumentExtensionDataAdapter<MyClassWithInconsistentJsonNameAttributes>(
                x => x.MyExtensionData
            );
        instantiatingWithInvalidAttributes.Should().NotThrow<InconsistentPropertyNamesException>();
    }
}

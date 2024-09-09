using FluentAssertions;
using JsonExtensionDataPatchDocumentAdapter;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace UnitTest;

[TestClass]
public class SimpleTests
{
    [TestMethod]
    [DataRow(true, false, false)]
    [DataRow(true, false, true)]
    [DataRow(false, true, true)]
    [DataRow(false, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, false, false)]
    public void TestPatchingExtensionData(
        bool initialExtensionDataAreEmpty,
        bool overwriteExistingExtensionData,
        bool useStringlyTypedPath
    )
    {
        if (initialExtensionDataAreEmpty && overwriteExistingExtensionData)
        {
            throw new Exception("This makes no sense");
        }

        string myEntityAsJson;
        if (initialExtensionDataAreEmpty)
        {
            myEntityAsJson = """
                                 {
                    "Foo": 17,
                                     "Bar": "asd"
                                 }
                """;
        }
        else
        {
            myEntityAsJson = """
                                 {
                    "Foo": 17,
                                     "Bar": "asd",
                                     "abc": "def"
                                 }
                """;
        }

        var myEntity = System.Text.Json.JsonSerializer.Deserialize<MyClass>(myEntityAsJson);
        myEntity.Should().NotBeNull();
        if (initialExtensionDataAreEmpty)
        {
            myEntity!.MyExtensionData.Should().BeNullOrEmpty();
        }
        else
        {
            myEntity!.MyExtensionData.Should().NotBeNullOrEmpty();
        }

        var myPatch = new JsonPatchDocument<MyClass>();
        myPatch.Add(x => x.Foo, 42);
        myPatch.Add(x => x.Bar, "fgh");
        string modifiedKey = overwriteExistingExtensionData ? "abc" : "uvw";

        if (!useStringlyTypedPath)
        {
            if (initialExtensionDataAreEmpty)
            {
                myPatch.Add(
                    x => x.MyExtensionData,
                    new Dictionary<string, object> { { modifiedKey, "xyz" } }
                );
            }
            else
            {
                myEntity.MyExtensionData.Should().NotBeNull();
                myPatch.Add(x => x.MyExtensionData![modifiedKey], "xyz");
            }
        }
        else
        {
            myPatch.Operations.Add(
                new Operation<MyClass>
                {
                    path = "/" + modifiedKey,
                    op = "add",
                    value = "xyz",
                }
            );
            myPatch = new JsonPatchDocumentExtensionDataAdapter<MyClass>(x =>
                x.MyExtensionData
            ).TransformDocument(myPatch, in myEntity);
        }

        myPatch.ApplyTo(myEntity);

        // Assertions
        myEntity.Foo.Should().Be(42);
        myEntity.Bar.Should().Be("fgh");
        myEntity.MyExtensionData.Should().NotBeNull();
        myEntity.MyExtensionData.Should().ContainKey(modifiedKey);
        myEntity.MyExtensionData![modifiedKey].Should().Be("xyz");

        if (!overwriteExistingExtensionData && !initialExtensionDataAreEmpty)
        {
            myEntity
                .MyExtensionData.Should()
                .ContainKey("abc")
                .WhoseValue.ToString() // todo: why do we need to string here? it's only relevant for the parametrizations where the initial extension data are not empty (initialExtensionDataAreEmpty = false, overwrite = false)
                .Should()
                .Be("def");
        }
    }
}

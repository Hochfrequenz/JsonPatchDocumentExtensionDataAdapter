using AwesomeAssertions;
using JsonExtensionDataPatchDocumentAdapter;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace UnitTest;

/// <summary>
/// other than <see cref="SimpleTests"/> this test is less theoretical and uses a real model class in our project.
/// </summary>
[TestClass]
public class NestedPropertiesTest
{
    [TestMethod]
    public void Test_Patching_Nested_ExtensionData_Where_No_ExtensionData_Exist()
    {
        var myNestedInstance = new MyClassWithNesting
        {
            MyString = "asd",
            MyInteger = 42,
            MyModel = new MyClass
            {
                Foo = 17,
                Bar = "Baz",
                // ExtensionData are still empty.
            },
        };
        JsonPatchDocument<MyClassWithNesting> patch = new JsonPatchDocument<MyClassWithNesting>
        {
            Operations =
            {
                new Operation<MyClassWithNesting> { op = "remove", path = "/MyModel/Foo" },
                new Operation<MyClassWithNesting>
                {
                    op = "add",
                    path = "/MyModel/Bar",
                    value = "something else",
                },
                new Operation<MyClassWithNesting>
                {
                    op = "add",
                    path = "/MyModel/lokationszuordnungen",
                },
            },
        };
        var patchingWithoutAdapter = () => patch.ApplyTo(myNestedInstance);
        patchingWithoutAdapter
            .Should()
            .ThrowExactly<JsonPatchException>()
            .Which.Message.Should()
            .Contain(
                "The target location specified by path segment 'lokationszuordnungen' was not found."
            ); // this is what gabriel experienced (see ticket)

        var modifiedPatch = new JsonPatchDocumentExtensionDataAdapter<MyClassWithNesting>(x =>
            x.MyModel!.MyExtensionData
        ).TransformDocument(patch, myNestedInstance);
        var patchingWithAdapter = () => modifiedPatch.ApplyTo(myNestedInstance);
        patchingWithAdapter.Should().NotThrow();
        myNestedInstance.MyModel.Foo.Should().BeNull();
        myNestedInstance.MyModel.MyExtensionData.Should().ContainKey("lokationszuordnungen");
    }

    /// <summary>
    /// other than <see cref="Test_Patching_Nested_ExtensionData_Where_No_ExtensionData_Exist"/> this test covers the case that there are UserProperties already
    /// </summary>
    [TestMethod]
    public void Test_Patching_Nested_ExtensionData_Where_ExtensionData_Exist_Already()
    {
        var myNestedInstance = new MyClassWithNesting
        {
            MyString = "asd",
            MyInteger = 42,
            MyModel = new MyClass
            {
                Foo = 17,
                Bar = "Baz",
                MyExtensionData = new Dictionary<string, object> { { "foo", "bar" } },
            },
        };
        JsonPatchDocument<MyClassWithNesting> patch = new JsonPatchDocument<MyClassWithNesting>
        {
            Operations =
            {
                new Operation<MyClassWithNesting> { op = "remove", path = "/MyModel/Foo" },
                new Operation<MyClassWithNesting>
                {
                    op = "add",
                    path = "/MyModel/Bar",
                    value = "asd",
                },
                new Operation<MyClassWithNesting>
                {
                    op = "add",
                    path = "/MyModel/lokationszuordnungen",
                },
            },
        };
        var patchingWithoutAdapter = () => patch.ApplyTo(myNestedInstance);
        patchingWithoutAdapter
            .Should()
            .ThrowExactly<JsonPatchException>()
            .Which.Message.Should()
            .Contain(
                "The target location specified by path segment 'lokationszuordnungen' was not found."
            ); // this is what gabriel experienced (see ticket)

        var modifiedPatch = new JsonPatchDocumentExtensionDataAdapter<MyClassWithNesting>(x =>
            x.MyModel!.MyExtensionData
        ).TransformDocument(patch, myNestedInstance);
        var patchingWithAdapter = () => modifiedPatch.ApplyTo(myNestedInstance);
        patchingWithAdapter.Should().NotThrow();
        myNestedInstance.MyModel.Foo.Should().BeNull();
        myNestedInstance.MyModel.MyExtensionData.Should().ContainKey("lokationszuordnungen");
        myNestedInstance
            .MyModel.MyExtensionData.Should()
            .ContainKey("foo")
            .WhoseValue.Should()
            .Be("bar");
    }

    /// <summary>
    /// Tests overwriting an existing user property
    /// </summary>
    [TestMethod]
    public void Test_Patching_Nested_ExtensionData_Where_ExtensionData_Key_Exist_Already()
    {
        var myNestedInstance = new MyClassWithNesting
        {
            MyString = "asd",
            MyInteger = 42,
            MyModel = new MyClass
            {
                Foo = 17,
                Bar = "Baz",
                MyExtensionData = new Dictionary<string, object> { { "unmappedProperty", "bar" } },
            },
        };
        JsonPatchDocument<MyClassWithNesting> patch = new JsonPatchDocument<MyClassWithNesting>
        {
            Operations =
            {
                new Operation<MyClassWithNesting>
                {
                    op = "replace",
                    path = "/MyString",
                    value = "my new string",
                },
                new Operation<MyClassWithNesting>
                {
                    op = "replace",
                    path = "/MyModel/unmappedProperty",
                    value = "xyz",
                },
            },
        };
        var patchingWithoutAdapter = () => patch.ApplyTo(myNestedInstance);
        patchingWithoutAdapter
            .Should()
            .ThrowExactly<JsonPatchException>()
            .Which.Message.Should()
            .Contain(
                "The target location specified by path segment 'unmappedProperty' was not found."
            ); // this is what gabriel experienced (see ticket)

        var modifiedPatch = new JsonPatchDocumentExtensionDataAdapter<MyClassWithNesting>(x =>
            x.MyModel!.MyExtensionData
        ).TransformDocument(patch, myNestedInstance);
        var patchingWithAdapter = () => modifiedPatch.ApplyTo(myNestedInstance);
        patchingWithAdapter.Should().NotThrow();
        myNestedInstance.MyString.Should().Be("my new string");
        myNestedInstance
            .MyModel.MyExtensionData.Should()
            .ContainKey("unmappedProperty")
            .WhoseValue.Should()
            .Be("xyz");
    }

    /// <summary>
    /// Tests overwriting an existing user property where the property has json name attributes
    /// </summary>
    [TestMethod]
    public void Test_Patching_Nested_ExtensionData_Where_ExtensionData_Key_Exist_Already_With_JsonNames()
    {
        var myNestedInstance = new MyClassWithNestingAndJsonNameAttribute
        {
            MyString = "asd",
            MyInteger = 42,
            MyModel = new MyClassWithJsonNameAttributes
            {
                Foo = 17,
                Bar = "Baz",
                MyExtensionData = new Dictionary<string, object> { { "unmappedProperty", "bar" } },
            },
        };
        JsonPatchDocument<MyClassWithNestingAndJsonNameAttribute> patch =
            new JsonPatchDocument<MyClassWithNestingAndJsonNameAttribute>
            {
                Operations =
                {
                    new Operation<MyClassWithNestingAndJsonNameAttribute>
                    {
                        op = "replace",
                        path = "/MyString",
                        value = "my new string",
                    },
                    new Operation<MyClassWithNestingAndJsonNameAttribute>
                    {
                        op = "replace",
                        path = "/mySAdasdasdInteger",
                        value = 43,
                    },
                    new Operation<MyClassWithNestingAndJsonNameAttribute>
                    {
                        op = "replace",
                        path = "/moooooodel/unmappedProperty",
                        value = "xyz",
                    },
                },
            };
        var patchingWithoutAdapter = () => patch.ApplyTo(myNestedInstance);
        patchingWithoutAdapter
            .Should()
            .ThrowExactly<JsonPatchException>()
            .Which.Message.Should()
            .Contain(
                "The target location specified by path segment 'unmappedProperty' was not found."
            ); // this is what gabriel experienced (see ticket)

        var modifiedPatch =
            new JsonPatchDocumentExtensionDataAdapter<MyClassWithNestingAndJsonNameAttribute>(x =>
                x.MyModel!.MyExtensionData
            ).TransformDocument(patch, myNestedInstance);
        var patchingWithAdapter = () => modifiedPatch.ApplyTo(myNestedInstance);
        patchingWithAdapter.Should().NotThrow();
        myNestedInstance.MyString.Should().Be("my new string");
        myNestedInstance.MyInteger.Should().Be(43);
        myNestedInstance
            .MyModel.MyExtensionData.Should()
            .ContainKey("unmappedProperty")
            .WhoseValue.Should()
            .Be("xyz");
    }
}

// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Data.Fields;

namespace VDrumExplorer.Model.Test.Data;

internal class DataValidationErrorTest
{
    private Module module = null!;

    [SetUp]
    public void SetUp()
    {
        module = TestData.LoadTD27();
    }

    [Test]
    public void None_IsEmptyEnumerable()
    {
        var none = DataValidationError.None;
        Assert.AreEqual(0, none.Count());
    }

    [Test]
    public void None_CanBeIteratedMultipleTimes()
    {
        // The None property returns a reusable enumerable (EmptyErrorCollection)
        var none = DataValidationError.None;
        Assert.AreEqual(0, none.Count());
        Assert.AreEqual(0, none.Count());
    }

    [Test]
    public void Constructor_WithFieldAndMessage_SetsProperties()
    {
        var dataField = GetDataField();
        var error = new DataValidationError(dataField, "Test error message");

        Assert.AreSame(dataField, error.Field);
        Assert.AreEqual("Test error message", error.Message);
        Assert.IsNull(error.OverlayParentField);
        Assert.IsNull(error.OverlayDescription);
    }

    [Test]
    public void Path_WithoutOverlay_ReturnsFieldPath()
    {
        var dataField = GetDataField();
        var error = new DataValidationError(dataField, "Test error");

        Assert.AreEqual(dataField.SchemaField.Path, error.Path);
    }

    [Test]
    public void ToString_ContainsMessage()
    {
        var dataField = GetDataField();
        var error = new DataValidationError(dataField, "Something went wrong");

        Assert.That(error.ToString(), Does.Contain("Something went wrong"));
    }

    [Test]
    public void ToString_ContainsPath()
    {
        var dataField = GetDataField();
        var error = new DataValidationError(dataField, "Something went wrong");

        Assert.That(error.ToString(), Does.Contain(dataField.SchemaField.Path));
    }

    [Test]
    public void Constructor_WithOverlay_SetsOverlayProperties()
    {
        var dataField = GetDataField();
        var overlayField = GetOverlayDataField();
        var error = new DataValidationError(dataField, "Test error", overlayField, "Overlay description");

        Assert.AreSame(dataField, error.Field);
        Assert.AreEqual("Test error", error.Message);
        Assert.AreSame(overlayField, error.OverlayParentField);
        Assert.AreEqual("Overlay description", error.OverlayDescription);
    }

    [Test]
    public void Path_WithOverlay_IncludesOverlayInfo()
    {
        var dataField = GetDataField();
        var overlayField = GetOverlayDataField();
        var error = new DataValidationError(dataField, "Test error", overlayField, "MyOverlay");

        var expectedPath = $"{overlayField.SchemaField.Path}/{{MyOverlay}}{dataField.SchemaField.Path}";
        Assert.AreEqual(expectedPath, error.Path);
    }

    [Test]
    public void ToString_WithOverlay_ContainsOverlayPath()
    {
        var dataField = GetDataField();
        var overlayField = GetOverlayDataField();
        var error = new DataValidationError(dataField, "Test error", overlayField, "MyOverlay");

        Assert.That(error.ToString(), Does.Contain(overlayField.SchemaField.Path));
    }

    private IDataField GetDataField()
    {
        var schemaField = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<VDrumExplorer.Model.Schema.Physical.FieldContainer>()
            .SelectMany(fc => fc.Fields)
            .First();
        return module.Data.GetDataField(schemaField);
    }

    private IDataField GetOverlayDataField()
    {
        var schemaField = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<VDrumExplorer.Model.Schema.Physical.FieldContainer>()
            .SelectMany(fc => fc.Fields)
            .OfType<VDrumExplorer.Model.Schema.Fields.OverlayField>()
            .First();
        return module.Data.GetDataField(schemaField);
    }
}

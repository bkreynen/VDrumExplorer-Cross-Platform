// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Schema.Fields;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.Model.Schema.Physical;
using VDrumExplorer.Model.Test.Helpers;

namespace VDrumExplorer.Model.Test.Data;

internal class ModuleDataTest
{
    private Module module = null!;
    private ModuleData data = null!;

    [SetUp]
    public void SetUp()
    {
        module = TestData.LoadTD27();
        data = module.Data;
    }

    [Test]
    public void Schema_ReturnsCorrectSchema()
    {
        Assert.AreSame(module.Schema, data.Schema);
    }

    [Test]
    public void LogicalRoot_IsNotNull()
    {
        Assert.IsNotNull(data.LogicalRoot);
    }

    [Test]
    public void CreateSnapshot_ReturnsNonEmptySnapshot()
    {
        var snapshot = data.CreateSnapshot();
        Assert.IsNotNull(snapshot);
        Assert.Greater(snapshot.SegmentCount, 0);
    }

    [Test]
    public void CreatePartialSnapshot_ForKitRoot_ReturnsSmallerSnapshot()
    {
        var fullSnapshot = data.CreateSnapshot();
        var kitRoot = module.Schema.GetKitRoot(1);
        var partialSnapshot = data.CreatePartialSnapshot(kitRoot);

        Assert.IsNotNull(partialSnapshot);
        Assert.Greater(partialSnapshot.SegmentCount, 0);
        Assert.Less(partialSnapshot.SegmentCount, fullSnapshot.SegmentCount);
    }

    [Test]
    public void CreatePartialSnapshot_ValidRoot_Succeeds()
    {
        // Previously misnamed CreatePartialSnapshot_InvalidSchema_Throws but actually tested the happy path.
        // This test documents the valid-root case explicitly.
        var kitRoot = module.Schema.GetKitRoot(1);
        var snapshot = data.CreatePartialSnapshot(kitRoot);
        Assert.Greater(snapshot.SegmentCount, 0);
        Assert.Less(snapshot.SegmentCount, data.CreateSnapshot().SegmentCount);
    }

    [Test]
    public void CreatePartialSnapshot_InvalidSchema_Throws()
    {
        // Use a synthetic schema mismatch via a different module identifier (TD-17 vs TD-27)
        // to trigger the ArgumentException for an incorrect schema.
        var td17Schema = ModuleSchema.KnownSchemas[ModuleIdentifier.TD17].Value;
        var foreignRoot = td17Schema.GetKitRoot(1);
        var ex = Assert.Throws<System.ArgumentException>(() => data.CreatePartialSnapshot(foreignRoot));
        Assert.That(ex!.Message, Does.Contain("schema").IgnoreCase);
    }

    [Test]
    public void GetDataField_ForBooleanField_ReturnsBooleanDataField()
    {
        var schemaField = ModelTestHelpers.FindBooleanField(module);
        var dataField = data.GetDataField(schemaField);
        Assert.IsInstanceOf<BooleanDataField>(dataField);
    }

    [Test]
    public void GetDataField_ForEnumField_ReturnsEnumDataField()
    {
        var schemaField = ModelTestHelpers.FindEnumField(module);
        var dataField = data.GetDataField(schemaField);
        Assert.IsInstanceOf<EnumDataField>(dataField);
    }

    [Test]
    public void GetDataField_ForNumericField_ReturnsNumericDataField()
    {
        var schemaField = ModelTestHelpers.FindNumericField(module);
        var dataField = data.GetDataField(schemaField);
        Assert.IsInstanceOf<NumericDataField>(dataField);
    }

    [Test]
    public void GetDataField_ForInstrumentField_ReturnsInstrumentDataField()
    {
        var schemaField = ModelTestHelpers.FindInstrumentField(module);
        var dataField = data.GetDataField(schemaField);
        Assert.IsInstanceOf<InstrumentDataField>(dataField);
    }

    [Test]
    public void GetDataField_ForOverlayField_ReturnsOverlayDataField()
    {
        var schemaField = ModelTestHelpers.FindOverlayKitMfx1(module).Overlay;
        var dataField = data.GetDataField(schemaField);
        Assert.IsInstanceOf<OverlayDataField>(dataField);
    }

    [Test]
    public void GetDataField_ForTempoField_ReturnsTempoDataField()
    {
        // TempoField instances are compound fields that only appear within overlay field lists,
        // not directly in FieldContainer.Fields. We find one via an overlay's current field list.
        var container = module.Schema.Kit1Root.Container.ResolveContainer("KitMfx[1]");
        var typeField = (EnumField)container.ResolveField("Type");
        var parametersField = (OverlayField)container.ResolveField("Parameters");

        var typeDataField = (EnumDataField)data.GetDataField(typeField);
        var overlayDataField = (OverlayDataField)data.GetDataField(parametersField);
        typeDataField.RawValue = 0; // Delay is the first MFX option, which contains tempo fields.

        var tempoDataField = overlayDataField.CurrentFieldList.Fields
            .First(f => f.SchemaField is TempoField);
        Assert.IsInstanceOf<TempoDataField>(tempoDataField);
    }

    [Test]
    public void GetDataFields_ReturnsAllFieldsForContainer()
    {
        var fieldContainer = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .First();
        var dataFields = data.GetDataFields(fieldContainer);
        Assert.AreEqual(fieldContainer.Fields.Count, dataFields.Count);
    }

    [Test]
    public void GetDataFields_ReturnsCorrectTypes()
    {
        var fieldContainer = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .First();
        var dataFields = data.GetDataFields(fieldContainer);
        for (int i = 0; i < fieldContainer.Fields.Count; i++)
        {
            Assert.AreSame(fieldContainer.Fields[i], dataFields[i].SchemaField);
        }
    }

    [Test]
    public void GetDataFieldFormattedValues_ReturnsFormattedStrings()
    {
        var fieldContainer = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .First();
        var values = data.GetDataFieldFormattedValues(fieldContainer).ToList();
        Assert.AreEqual(fieldContainer.Fields.Count, values.Count);
        foreach (var (name, text) in values)
        {
            Assert.IsFalse(string.IsNullOrEmpty(name));
            // FormattedText can throw for overlay fields, but the method handles that by expanding them
            Assert.IsNotNull(text);
        }
    }

    [Test]
    public void GetDataFieldFormattedValues_ExpandsOverlayFields()
    {
        // Find a container with an overlay field
        var containerWithOverlay = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .First(fc => fc.Fields.OfType<OverlayField>().Any());
        var overlaySchemaField = containerWithOverlay.Fields.OfType<OverlayField>().First();

        var values = data.GetDataFieldFormattedValues(containerWithOverlay).ToList();
        // The overlay field should be expanded into multiple entries with prefixed names
        var overlayDataField = (OverlayDataField)data.GetDataField(overlaySchemaField);
        var overlayFieldNames = values.Where(v => v.Item1.StartsWith(overlaySchemaField.Name + ".")).ToList();
        Assert.Greater(overlayFieldNames.Count, 0);
    }
}

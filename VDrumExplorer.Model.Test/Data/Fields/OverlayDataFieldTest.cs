// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Schema.Fields;
using VDrumExplorer.Model.Schema.Physical;

namespace VDrumExplorer.Model.Test.Data.Fields;

internal class OverlayDataFieldTest
{
    private Module module = null!;
    private OverlayDataField overlayField = null!;
    private EnumDataField switchField = null!;

    [SetUp]
    public void SetUp()
    {
        module = TestData.LoadTD27();
        var schema = module.Schema;

        // Find an OverlayField with an EnumField switch (like the MFX parameters).
        // The TempoDataFieldTest uses KitMfx[1] with Type/Parameters.
        var container = schema.Kit1Root.Container.ResolveContainer("KitMfx[1]");
        var typeField = (EnumField)container.ResolveField("Type");
        var parametersField = (OverlayField)container.ResolveField("Parameters");

        switchField = (EnumDataField)module.Data.GetDataField(typeField);
        overlayField = (OverlayDataField)module.Data.GetDataField(parametersField);
    }

    [Test]
    public void CurrentFieldList_IsNotNull()
    {
        Assert.IsNotNull(overlayField.CurrentFieldList);
    }

    [Test]
    public void CurrentFieldList_FieldsAreNonEmpty()
    {
        CollectionAssert.IsNotEmpty(overlayField.CurrentFieldList.Fields);
    }

    [Test]
    public void CurrentFieldList_MatchesSwitchFieldValue()
    {
        // The current field list key should match the switch field's current value.
        var switchValue = switchField.Value;
        var expectedDescription = overlayField.SchemaField.FieldLists[switchValue].Description;
        Assert.AreEqual(expectedDescription, overlayField.CurrentFieldList.Description);
    }

    [Test]
    public void Setup_UsesNamedFieldResolution()
    {
        // Pin to named fields instead of relying on .First() ordering — if the schema
        // is reordered, this test will fail explicitly rather than silently switching fixtures.
        var container = module.Schema.Kit1Root.Container.ResolveContainer("KitMfx[1]");
        var typeField = container.ResolveField("Type");
        var parametersField = container.ResolveField("Parameters");
        Assert.AreEqual("Type", typeField.Name);
        Assert.AreEqual("Parameters", parametersField.Name);
        Assert.AreSame(typeField, switchField.SchemaField);
        Assert.AreSame(parametersField, overlayField.SchemaField);
    }

    [Test]
    public void CurrentFieldList_ChangesWhenSwitchFieldChanges()
    {
        // Fail (inconclusive) if the fixture does not have at least two switch values —
        // previously this silently passed with zero asserts.
        Assume.That(switchField.SchemaField.Values.Count, Is.GreaterThan(1),
            "Requires at least 2 switch values to test switching");
        // Set the switch to the first value
        switchField.Value = switchField.SchemaField.Values[0];
        var firstFieldList = overlayField.CurrentFieldList;

        // Change to a different value
        switchField.Value = switchField.SchemaField.Values[1];
        var secondFieldList = overlayField.CurrentFieldList;
        Assert.AreNotSame(firstFieldList, secondFieldList);
    }

    [Test]
    public void FormattedText_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _ = overlayField.FormattedText);
    }

    [Test]
    public void TrySetFormattedText_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => overlayField.TrySetFormattedText("test"));
    }

    [Test]
    public void Reset_ResetsAllFieldsInCurrentList()
    {
        // Find a numeric field in the current list — previously used fields[0] is NumericDataFieldBase
        // which could silently skip the assertion if the first field was not numeric.
        var fields = overlayField.CurrentFieldList.Fields;
        var numericField = fields.OfType<NumericDataFieldBase>().FirstOrDefault();
        Assume.That(numericField, Is.Not.Null,
            "Current overlay field list should contain a NumericDataFieldBase to test reset");
        // Set to a non-default value to make the reset observable
        if (numericField!.SchemaField.Default != numericField.SchemaField.Min)
        {
            numericField.RawValue = numericField.SchemaField.Min;
        }
        else
        {
            numericField.RawValue = numericField.SchemaField.Max;
        }

        Assume.That(numericField.RawValue, Is.Not.EqualTo(numericField.SchemaField.Default),
            "Precondition: value should be non-default before reset");

        overlayField.Reset();

        // After reset, the field should be back to its default
        Assert.AreEqual(numericField.SchemaField.Default, numericField.RawValue);
    }

    [Test]
    public void CurrentFieldList_Change_TriggersPropertyChange()
    {
        Assume.That(switchField.SchemaField.Values.Count, Is.GreaterThan(1),
            "Requires at least 2 switch values to test property change");
        var recorder = new NotifyChangeRecorder(overlayField);
        // Change the switch field to trigger a CurrentFieldList change
        var current = switchField.Value;
        var newValue = switchField.SchemaField.Values.First(v => v != current);
        switchField.Value = newValue;
        // The overlay field should raise CurrentFieldList change
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(overlayField.CurrentFieldList));
    }
}

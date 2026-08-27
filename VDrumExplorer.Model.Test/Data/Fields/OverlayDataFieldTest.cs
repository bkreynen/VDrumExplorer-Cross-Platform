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
        Assert.IsNotNull(overlayField.CurrentFieldList);
        // The field list description should correspond to the switch value
        Assert.IsNotNull(overlayField.CurrentFieldList.Description);
    }

    [Test]
    public void CurrentFieldList_ChangesWhenSwitchFieldChanges()
    {
        // Set the switch to the first value
        switchField.Value = switchField.SchemaField.Values[0];
        var firstFieldList = overlayField.CurrentFieldList;

        // Change to a different value (if there are multiple)
        if (switchField.SchemaField.Values.Count > 1)
        {
            switchField.Value = switchField.SchemaField.Values[1];
            var secondFieldList = overlayField.CurrentFieldList;
            Assert.AreNotSame(firstFieldList, secondFieldList);
        }
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
        // Modify a field in the current list
        var fields = overlayField.CurrentFieldList.Fields;
        if (fields.Count > 0 && fields[0] is NumericDataFieldBase numericField)
        {
            // Set to a non-default value
            if (numericField.SchemaField.Default != numericField.SchemaField.Min)
            {
                numericField.RawValue = numericField.SchemaField.Min;
            }
            else
            {
                numericField.RawValue = numericField.SchemaField.Max;
            }

            overlayField.Reset();

            // After reset, the field should be back to its default
            Assert.AreEqual(numericField.SchemaField.Default, numericField.RawValue);
        }
    }

    [Test]
    public void CurrentFieldList_Change_TriggersPropertyChange()
    {
        var recorder = new NotifyChangeRecorder(overlayField);
        // Change the switch field to trigger a CurrentFieldList change
        if (switchField.SchemaField.Values.Count > 1)
        {
            var current = switchField.Value;
            var newValue = switchField.SchemaField.Values.First(v => v != current);
            switchField.Value = newValue;
            // The overlay field should raise CurrentFieldList change
            CollectionAssert.Contains(recorder.ChangedProperties, nameof(overlayField.CurrentFieldList));
        }
    }
}

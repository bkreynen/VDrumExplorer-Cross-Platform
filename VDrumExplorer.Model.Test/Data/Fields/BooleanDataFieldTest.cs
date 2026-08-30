// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.Model.Schema.Fields;
using VDrumExplorer.Model.Schema.Physical;
using VDrumExplorer.Model.Test.Helpers;

namespace VDrumExplorer.Model.Test.Data.Fields;

internal class BooleanDataFieldTest
{
    private BooleanDataField field = null!;

    [SetUp]
    public void SetUp()
    {
        var module = ModelTestHelpers.LoadTD27();
        // Use shared helper to avoid per-file DescendantsAndSelf().OfType<>().First() duplication.
        field = ModelTestHelpers.FindBooleanDataField(module);
    }

    [Test]
    public void Value_GetReturnsBool()
    {
        // Just verify we can read the value without error.
        var value = field.Value;
        Assert.That(value, Is.TypeOf<bool>());
    }

    [Test]
    public void Value_SetTrue_TogglesRawValueToOne()
    {
        field.Value = true;
        Assert.AreEqual(1, field.RawValue);
        Assert.IsTrue(field.Value);
    }

    [Test]
    public void Value_SetFalse_TogglesRawValueToZero()
    {
        field.Value = false;
        Assert.AreEqual(0, field.RawValue);
        Assert.IsFalse(field.Value);
    }

    [Test]
    public void FormattedText_OnWhenValueTrue()
    {
        field.Value = true;
        Assert.AreEqual("On", field.FormattedText);
    }

    [Test]
    public void FormattedText_OffWhenValueFalse()
    {
        field.Value = false;
        Assert.AreEqual("Off", field.FormattedText);
    }

    [Test]
    public void TrySetFormattedText_On_SetsValueTrue()
    {
        field.Value = false;
        Assert.IsTrue(field.TrySetFormattedText("On"));
        Assert.IsTrue(field.Value);
    }

    [Test]
    public void TrySetFormattedText_Off_SetsValueFalse()
    {
        field.Value = true;
        Assert.IsTrue(field.TrySetFormattedText("Off"));
        Assert.IsFalse(field.Value);
    }

    [Test]
    public void TrySetFormattedText_InvalidText_ReturnsFalse()
    {
        field.Value = true;
        Assert.IsFalse(field.TrySetFormattedText("invalid"));
        // Value should not have changed
        Assert.IsTrue(field.Value);
    }

    [Test]
    public void Value_Set_TriggersPropertyChanges()
    {
        field.Value = false; // Start from a known state
        var recorder = new NotifyChangeRecorder(field);
        field.Value = true;
        // BooleanDataField raises: RawValue, Value, FormattedText
        CollectionAssert.AreEquivalent(
            new[] { nameof(field.RawValue), nameof(field.Value), nameof(field.FormattedText) },
            recorder.ChangedProperties);
    }

    [Test]
    public void Value_SetToSameValue_DoesNotTriggerChange()
    {
        field.Value = true;
        var recorder = new NotifyChangeRecorder(field);
        field.Value = true;
        Assert.IsEmpty(recorder.ChangedProperties);
    }

    [Test]
    public void Reset_SetsToDefaultValue()
    {
        // Set to a non-default value first
        var defaultValue = field.SchemaField.Default == 1;
        field.Value = !defaultValue;

        field.Reset();

        // After reset, RawValue should be the schema default
        Assert.AreEqual(field.SchemaField.Default, field.RawValue);
        Assert.AreEqual(defaultValue, field.Value);
    }
}

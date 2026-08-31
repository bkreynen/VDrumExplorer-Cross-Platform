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
using VDrumExplorer.Model.Test.Helpers;

namespace VDrumExplorer.Model.Test.Data.Fields;

internal class EnumDataFieldTest
{
    private EnumDataField field = null!;

    [SetUp]
    public void SetUp()
    {
        var module = ModelTestHelpers.LoadTD27();
        field = ModelTestHelpers.FindEnumDataField(module);
    }

    [Test]
    public void Value_GetReturnsValidEnumName()
    {
        var value = field.Value;
        CollectionAssert.Contains(field.SchemaField.Values, value);
    }

    [Test]
    public void Value_SetWithValidName_UpdatesValue()
    {
        // Pick a value different from the current one
        var current = field.Value;
        var newValue = field.SchemaField.Values.First(v => v != current);
        field.Value = newValue;
        Assert.AreEqual(newValue, field.Value);
    }

    [Test]
    public void Value_SetWithInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => field.Value = "NonexistentEnumValue");
    }

    [Test]
    public void FormattedText_ReturnsCurrentValue()
    {
        Assert.AreEqual(field.Value, field.FormattedText);
    }

    [Test]
    public void TrySetFormattedText_ValidName_ReturnsTrue()
    {
        var current = field.Value;
        var newValue = field.SchemaField.Values.First(v => v != current);
        Assert.IsTrue(field.TrySetFormattedText(newValue));
        Assert.AreEqual(newValue, field.Value);
    }

    [Test]
    public void TrySetFormattedText_InvalidText_ReturnsFalse()
    {
        var originalValue = field.Value;
        Assert.IsFalse(field.TrySetFormattedText("NonexistentEnumValue"));
        // Value should not have changed
        Assert.AreEqual(originalValue, field.Value);
    }

    [Test]
    public void Value_Set_TriggersPropertyChanges()
    {
        var current = field.Value;
        var newValue = field.SchemaField.Values.First(v => v != current);
        var recorder = new NotifyChangeRecorder(field);
        field.Value = newValue;
        // EnumDataField raises: RawValue, Value, FormattedText
        CollectionAssert.AreEquivalent(
            new[] { nameof(field.RawValue), nameof(field.Value), nameof(field.FormattedText) },
            recorder.ChangedProperties);
    }

    [Test]
    public void Value_SetToSameValue_DoesNotTriggerChange()
    {
        var current = field.Value;
        var recorder = new NotifyChangeRecorder(field);
        field.Value = current;
        Assert.IsEmpty(recorder.ChangedProperties);
    }

    [Test]
    public void Reset_SetsToDefaultValue()
    {
        // Set to a non-default value first
        var defaultValue = field.SchemaField.NameByRawNumber[field.SchemaField.Default];
        var nonDefault = field.SchemaField.Values.First(v => v != defaultValue);
        field.Value = nonDefault;

        field.Reset();

        Assert.AreEqual(defaultValue, field.Value);
        Assert.AreEqual(field.SchemaField.Default, field.RawValue);
    }

    [Test]
    public void RawValue_SetWithInvalidValue_DoesNotChange()
    {
        // Find a raw value that's not in the enum's valid values
        var invalidValue = field.SchemaField.NameByRawNumber.Keys.Max() + 1;
        var originalRaw = field.RawValue;
        // TrySetRawValue should return false for invalid values
        Assert.IsFalse(field.SchemaField.NameByRawNumber.ContainsKey(invalidValue));
        // Setting via RawValue property should throw since TrySetRawValue returns false
        Assert.Throws<ArgumentOutOfRangeException>(() => field.RawValue = invalidValue);
        Assert.AreEqual(originalRaw, field.RawValue);
    }
}

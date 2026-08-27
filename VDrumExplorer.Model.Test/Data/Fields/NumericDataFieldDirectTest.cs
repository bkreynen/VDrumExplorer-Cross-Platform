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

/// <summary>
/// Tests for the data field <see cref="NumericDataField"/>, as opposed to the schema field
/// <see cref="NumericField"/> which is tested in <c>NumericFieldTest</c>.
/// </summary>
internal class NumericDataFieldDirectTest
{
    private NumericDataField field = null!;

    [SetUp]
    public void SetUp()
    {
        var module = TestData.LoadTD27();
        var schemaField = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .SelectMany(fc => fc.Fields)
            .OfType<NumericField>()
            .First();
        field = (NumericDataField)module.Data.GetDataField(schemaField);
    }

    [Test]
    public void FormattedText_MatchesSchemaFormatRawValue()
    {
        var expected = field.SchemaField.FormatRawValue(field.RawValue);
        Assert.AreEqual(expected, field.FormattedText);
    }

    [Test]
    public void TrySetFormattedText_ValidValue_ReturnsTrue()
    {
        // Use the min value's formatted text
        var minText = field.SchemaField.FormatRawValue(field.SchemaField.Min);
        Assert.IsTrue(field.TrySetFormattedText(minText));
        Assert.AreEqual(field.SchemaField.Min, field.RawValue);
    }

    [Test]
    public void TrySetFormattedText_AnotherValidValue_ReturnsTrue()
    {
        // Use the max value's formatted text
        var maxText = field.SchemaField.FormatRawValue(field.SchemaField.Max);
        Assert.IsTrue(field.TrySetFormattedText(maxText));
        Assert.AreEqual(field.SchemaField.Max, field.RawValue);
    }

    [Test]
    public void TrySetFormattedText_InvalidText_ReturnsFalse()
    {
        var originalRaw = field.RawValue;
        Assert.IsFalse(field.TrySetFormattedText("ThisIsNotAValidNumber"));
        Assert.AreEqual(originalRaw, field.RawValue);
    }

    [Test]
    public void RawValue_GetReturnsCurrent()
    {
        // Set a known value and verify get
        field.RawValue = field.SchemaField.Min;
        Assert.AreEqual(field.SchemaField.Min, field.RawValue);
    }

    [Test]
    public void RawValue_SetWithValidValue_UpdatesValue()
    {
        field.RawValue = field.SchemaField.Max;
        Assert.AreEqual(field.SchemaField.Max, field.RawValue);
    }

    [Test]
    public void RawValue_SetBelowMin_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => field.RawValue = field.SchemaField.Min - 1);
    }

    [Test]
    public void RawValue_SetAboveMax_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => field.RawValue = field.SchemaField.Max + 1);
    }

    [Test]
    public void Reset_SetsToDefaultValue()
    {
        // Set to a non-default value first
        if (field.SchemaField.Default != field.SchemaField.Min)
        {
            field.RawValue = field.SchemaField.Min;
        }
        else
        {
            field.RawValue = field.SchemaField.Max;
        }

        field.Reset();

        Assert.AreEqual(field.SchemaField.Default, field.RawValue);
    }

    [Test]
    public void RawValue_Set_TriggersPropertyChanges()
    {
        field.RawValue = field.SchemaField.Min;
        var recorder = new NotifyChangeRecorder(field);
        field.RawValue = field.SchemaField.Max;
        // NumericDataField raises: RawValue, FormattedText
        CollectionAssert.AreEquivalent(
            new[] { nameof(field.RawValue), nameof(field.FormattedText) },
            recorder.ChangedProperties);
    }

    [Test]
    public void RawValue_SetToSameValue_DoesNotTriggerChange()
    {
        field.RawValue = field.SchemaField.Min;
        var recorder = new NotifyChangeRecorder(field);
        field.RawValue = field.SchemaField.Min;
        Assert.IsEmpty(recorder.ChangedProperties);
    }
}

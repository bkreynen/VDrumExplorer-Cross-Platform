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
/// Extended tests for <see cref="TempoDataField"/>, complementing <see cref="TempoDataFieldTest"/>.
/// </summary>
internal class TempoDataFieldExtendedTest
{
    private Module module = null!;
    private TempoDataField field = null!;

    [SetUp]
    public void SetUp()
    {
        module = TestData.LoadTD27();
        var schema = module.Schema;

        // Follow the pattern from TempoDataFieldTest to find a TempoDataField.
        // Uses named field resolution (KitMfx[1]/Type and Parameters) so the test
        // is not brittle to schema ordering.
        var container = schema.Kit1Root.Container.ResolveContainer("KitMfx[1]");
        var typeField = (EnumField)container.ResolveField("Type");
        var parametersField = (OverlayField)container.ResolveField("Parameters");

        var typeDataField = (EnumDataField)module.Data.GetDataField(typeField);
        var overlayDataField = (OverlayDataField)module.Data.GetDataField(parametersField);
        typeDataField.RawValue = 0; // Delay is the first MFX option.

        // Pin to a TempoDataField by type rather than by index — Fields[0] is fragile
        // if the overlay's field order changes.
        var tempoField = overlayDataField.CurrentFieldList.Fields.OfType<TempoDataField>().FirstOrDefault();
        Assume.That(tempoField, Is.Not.Null, "Overlay field list should contain a TempoDataField");
        field = tempoField!;
    }

    [Test]
    public void TempoSync_GetReturnsBool()
    {
        // Just verify we can read the value without error.
        var value = field.TempoSync;
        Assert.That(value, Is.TypeOf<bool>());
    }

    [Test]
    public void TempoSync_SetTrue_TogglesSwitch()
    {
        field.TempoSync = true;
        Assert.IsTrue(field.TempoSync);
    }

    [Test]
    public void TempoSync_SetFalse_TogglesSwitch()
    {
        field.TempoSync = false;
        Assert.IsFalse(field.TempoSync);
    }

    [Test]
    public void FormattedText_TempoSyncMode_ReturnsTempoSyncPrefix()
    {
        field.TempoSync = true;
        Assert.IsTrue(field.FormattedText.StartsWith("Tempo sync: ", StringComparison.Ordinal));
    }

    [Test]
    public void FormattedText_FixedMode_ReturnsFixedPrefix()
    {
        field.TempoSync = false;
        Assert.IsTrue(field.FormattedText.StartsWith("Fixed: ", StringComparison.Ordinal));
    }

    [Test]
    public void TrySetFormattedText_TempoSyncFormat_ReturnsTrue()
    {
        // Get the current musical note text
        field.TempoSync = true;
        var noteText = field.FormattedText.Substring("Tempo sync: ".Length);

        Assert.IsTrue(field.TrySetFormattedText("Tempo sync: " + noteText));
        Assert.IsTrue(field.TempoSync);
    }

    [Test]
    public void TrySetFormattedText_FixedFormat_ReturnsTrue()
    {
        // Get the current numeric text
        field.TempoSync = false;
        var numericText = field.FormattedText.Substring("Fixed: ".Length);

        Assert.IsTrue(field.TrySetFormattedText("Fixed: " + numericText));
        Assert.IsFalse(field.TempoSync);
    }

    [Test]
    public void TrySetFormattedText_TempoSyncFormat_SetsTempoSyncTrue()
    {
        field.TempoSync = false;
        var noteText = field.MusicalNote;
        Assert.IsTrue(field.TrySetFormattedText("Tempo sync: " + noteText));
        Assert.IsTrue(field.TempoSync);
    }

    [Test]
    public void TrySetFormattedText_FixedFormat_SetsTempoSyncFalse()
    {
        field.TempoSync = true;
        var numericText = field.NumericFormattedText;
        Assert.IsTrue(field.TrySetFormattedText("Fixed: " + numericText));
        Assert.IsFalse(field.TempoSync);
    }

    [Test]
    public void TrySetFormattedText_InvalidText_ReturnsFalse()
    {
        Assert.IsFalse(field.TrySetFormattedText("Invalid format"));
    }

    [Test]
    public void TrySetFormattedText_InvalidTempoSyncNote_ReturnsFalse()
    {
        Assert.IsFalse(field.TrySetFormattedText("Tempo sync: NonexistentNote"));
    }

    [Test]
    public void TrySetFormattedText_InvalidFixedValue_ReturnsFalse()
    {
        Assert.IsFalse(field.TrySetFormattedText("Fixed: NotANumber"));
    }

    [Test]
    public void Reset_ResetsAllSubFields()
    {
        // Capture the defaults so the post-reset assertions are meaningful (not tautological).
        var switchDefault = field.SchemaField.SwitchField.Default;
        var numericDefault = field.SchemaField.NumericField.Default;
        var noteDefault = field.SchemaField.MusicalNoteField.Default;
        // Enum default string for MusicalNote (Value is string, Default is int raw number)
        var noteDefaultString = field.SchemaField.MusicalNoteField.NameByRawNumber[noteDefault];

        // Set each sub-field to a non-default value to make the reset observable.
        // For the boolean switch, flip to the opposite of the default.
        field.TempoSync = !switchDefault.Equals(1);
        Assume.That(field.TempoSync ? 1 : 0, Is.Not.EqualTo(switchDefault),
            "Precondition: TempoSync should be non-default before reset");
        // For the numeric sub-field, choose Max (or Min if Max == Default) to guarantee non-default.
        field.RawNumericValue = numericDefault == field.SchemaField.NumericField.Max
            ? field.SchemaField.NumericField.Min
            : field.SchemaField.NumericField.Max;
        Assume.That(field.RawNumericValue, Is.Not.EqualTo(numericDefault),
            "Precondition: RawNumericValue should be non-default before reset");
        // For the musical note, pick a different value from the default.
        var nonDefaultNote = field.SchemaField.MusicalNoteField.Values.First(v => v != noteDefaultString);
        field.MusicalNote = nonDefaultNote;
        Assume.That(field.MusicalNote, Is.Not.EqualTo(noteDefaultString),
            "Precondition: MusicalNote should be non-default before reset");

        field.Reset();

        // After reset, all sub-fields should be at their defaults — each assertion now
        // verifies that Reset restored a previously non-default value.
        Assert.AreEqual(switchDefault, field.TempoSync ? 1 : 0,
            "TempoSync should be reset to SwitchField.Default");
        Assert.AreEqual(numericDefault, field.RawNumericValue,
            "RawNumericValue should be reset to NumericField.Default");
        Assert.AreEqual(noteDefaultString, field.MusicalNote,
            "MusicalNote should be reset to MusicalNoteField.Default");
    }

    [Test]
    public void TempoSync_Set_TriggersPropertyChanges()
    {
        field.TempoSync = false;
        var recorder = new NotifyChangeRecorder(field);
        field.TempoSync = true;
        // TempoDataField raises: TempoSync, FormattedText
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.TempoSync));
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.FormattedText));
    }

    [Test]
    public void RawNumericValue_Set_TriggersPropertyChanges()
    {
        var recorder = new NotifyChangeRecorder(field);
        var newValue = field.RawNumericValue == field.SchemaField.NumericField.Min
            ? field.SchemaField.NumericField.Max
            : field.SchemaField.NumericField.Min;
        field.RawNumericValue = newValue;
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.RawNumericValue));
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.FormattedText));
    }

    [Test]
    public void MusicalNote_GetReturnsValidNote()
    {
        field.TempoSync = true;
        CollectionAssert.Contains(field.SchemaField.MusicalNoteField.Values, field.MusicalNote);
    }

    [Test]
    public void MusicalNote_Set_TriggersPropertyChanges()
    {
        field.TempoSync = true;
        var current = field.MusicalNote;
        var newNote = field.SchemaField.MusicalNoteField.Values.First(v => v != current);
        var recorder = new NotifyChangeRecorder(field);
        field.MusicalNote = newNote;
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.MusicalNote));
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.FormattedText));
    }
}

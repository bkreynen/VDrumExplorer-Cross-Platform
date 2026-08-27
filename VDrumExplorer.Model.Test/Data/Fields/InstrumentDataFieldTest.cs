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

internal class InstrumentDataFieldTest
{
    private InstrumentDataField field = null!;

    [SetUp]
    public void SetUp()
    {
        var module = TestData.LoadTD27();
        var schemaField = module.Schema.PhysicalRoot.DescendantsAndSelf()
            .OfType<FieldContainer>()
            .SelectMany(fc => fc.Fields)
            .OfType<InstrumentField>()
            .First();
        field = (InstrumentDataField)module.Data.GetDataField(schemaField);
    }

    [Test]
    public void Instrument_GetReturnsNonNull()
    {
        Assert.IsNotNull(field.Instrument);
    }

    [Test]
    public void Group_GetReturnsNonNull()
    {
        Assert.IsNotNull(field.Group);
    }

    [Test]
    public void Group_MatchesInstrumentGroup()
    {
        Assert.AreSame(field.Instrument.Group, field.Group);
    }

    [Test]
    public void FormattedText_ReturnsInstrumentName()
    {
        Assert.AreEqual(field.Instrument.Name, field.FormattedText);
    }

    [Test]
    public void TrySetFormattedText_ValidName_ReturnsTrue()
    {
        var instrument = field.Schema.PresetInstruments.First();
        // Make sure we pick a different instrument
        var current = field.Instrument;
        var target = field.Schema.PresetInstruments.FirstOrDefault(i => i.Name != current.Name)
            ?? field.Schema.PresetInstruments.First();

        Assert.IsTrue(field.TrySetFormattedText(target.Name));
        Assert.AreEqual(target.Name, field.Instrument.Name);
    }

    [Test]
    public void TrySetFormattedText_InvalidName_ReturnsFalse()
    {
        var originalInstrument = field.Instrument;
        Assert.IsFalse(field.TrySetFormattedText("NonexistentInstrument"));
        Assert.AreSame(originalInstrument, field.Instrument);
    }

    [Test]
    public void Reset_SetsToFirstPresetInstrument()
    {
        // Set to a non-default instrument first
        var firstPreset = field.Schema.PresetInstruments[0];
        var nonDefault = field.Schema.PresetInstruments.First(i => i.Id != firstPreset.Id);
        field.Instrument = nonDefault;

        field.Reset();

        Assert.AreSame(firstPreset, field.Instrument);
    }

    [Test]
    public void Instrument_Set_TriggersPropertyChanges()
    {
        var current = field.Instrument;
        var target = field.Schema.PresetInstruments.First(i => i != current);
        var recorder = new NotifyChangeRecorder(field);
        field.Instrument = target;
        // InstrumentDataField raises Instrument, and possibly Group if the group changed
        CollectionAssert.Contains(recorder.ChangedProperties, nameof(field.Instrument));
    }

    [Test]
    public void Instrument_SetToSameInstrument_DoesNotTriggerChange()
    {
        var current = field.Instrument;
        var recorder = new NotifyChangeRecorder(field);
        field.Instrument = current;
        Assert.IsEmpty(recorder.ChangedProperties);
    }

    [Test]
    public void Group_Set_ChangesInstrumentToFirstInGroup()
    {
        var currentGroup = field.Group;
        var otherGroup = field.Schema.InstrumentGroups.First(g => g != currentGroup && g.Preset);
        field.Group = otherGroup;
        Assert.AreSame(otherGroup, field.Group);
        Assert.AreSame(otherGroup.Instruments[0], field.Instrument);
    }

    [Test]
    public void Group_SetToSameGroup_DoesNotChangeInstrument()
    {
        var current = field.Instrument;
        field.Group = field.Group;
        Assert.AreSame(current, field.Instrument);
    }
}

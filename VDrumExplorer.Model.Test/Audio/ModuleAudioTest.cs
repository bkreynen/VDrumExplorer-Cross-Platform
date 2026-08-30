// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.Model.Test.Audio
{
    public class ModuleAudioTest
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(5);
            var instrument = schema.PresetInstruments.First();
            var captures = new List<InstrumentAudio>
            {
                new InstrumentAudio(instrument, new byte[] { 1, 2, 3 }),
                new InstrumentAudio(instrument, new byte[] { 4, 5, 6 })
            };

            var moduleAudio = new ModuleAudio(schema, format, duration, captures);

            Assert.AreSame(schema, moduleAudio.Schema);
            Assert.AreSame(format, moduleAudio.Format);
            Assert.AreEqual(duration, moduleAudio.DurationPerInstrument);
            Assert.AreSame(captures, moduleAudio.Captures);
            Assert.AreEqual(2, moduleAudio.Captures.Count);
        }

        [Test]
        public void Constructor_AcceptsEmptyCaptures()
        {
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(3);
            var captures = new List<InstrumentAudio>();

            var moduleAudio = new ModuleAudio(schema, format, duration, captures);

            Assert.AreEqual(0, moduleAudio.Captures.Count);
        }

        [Test]
        public void Constructor_DurationZero_IsPreserved()
        {
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(44100, 2, 16);
            var duration = TimeSpan.Zero;
            var captures = new List<InstrumentAudio>();
            var moduleAudio = new ModuleAudio(schema, format, duration, captures);
            Assert.AreEqual(TimeSpan.Zero, moduleAudio.DurationPerInstrument);
        }

        [Test]
        public void Constructor_NegativeDuration_IsPreserved_DocumentsNoValidation()
        {
            // Production ModuleAudio currently does not validate duration — negative is stored as-is.
            // If validation is added, this test should be updated to Assert.Throws.
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(-1);
            var captures = new List<InstrumentAudio>();
            var moduleAudio = new ModuleAudio(schema, format, duration, captures);
            Assert.AreEqual(TimeSpan.FromSeconds(-1), moduleAudio.DurationPerInstrument);
        }

        [Test]
        public void Constructor_Captures_IsSameReference_NoDefensiveCopy()
        {
            // ModuleAudio stores the captures list reference directly (no defensive copy), consistent with MidiMessage.
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(8000, 1, 8);
            var duration = TimeSpan.FromSeconds(2);
            var instrument = schema.PresetInstruments.First();
            var captures = new List<InstrumentAudio> { new InstrumentAudio(instrument, new byte[] { 9, 9 }) };
            var moduleAudio = new ModuleAudio(schema, format, duration, captures);
            Assert.AreSame(captures, moduleAudio.Captures);
            // Mutating the original list affects the instance — documents no-copy semantics.
            captures.Add(new InstrumentAudio(instrument, new byte[] { 1 }));
            Assert.AreEqual(2, moduleAudio.Captures.Count);
        }

        [Test]
        public void Constructor_CapturesMutability_AffectsInstance()
        {
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(48000, 2, 24);
            var duration = TimeSpan.FromSeconds(1);
            var instrument = schema.PresetInstruments.First();
            var captures = new List<InstrumentAudio>
            {
                new InstrumentAudio(instrument, new byte[] { 1, 2 })
            };
            var moduleAudio = new ModuleAudio(schema, format, duration, captures);
            // Clearing the original list clears the exposed Captures — documents shared reference.
            captures.Clear();
            Assert.AreEqual(0, moduleAudio.Captures.Count);
        }

        [Test]
        public void Constructor_DifferentFormat_IsPreserved()
        {
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(22050, 1, 8);
            var duration = TimeSpan.FromMilliseconds(500);
            var captures = new List<InstrumentAudio>();
            var moduleAudio = new ModuleAudio(schema, format, duration, captures);
            Assert.AreEqual(22050, moduleAudio.Format.Frequency);
            Assert.AreEqual(1, moduleAudio.Format.Channels);
            Assert.AreEqual(8, moduleAudio.Format.Bits);
            Assert.AreEqual(500, moduleAudio.DurationPerInstrument.TotalMilliseconds);
            // BytesPerSecond edge: 22050 *1*8/8 =22050
            Assert.AreEqual(22050, moduleAudio.Format.BytesPerSecond);
        }

        [Test]
        public void Constructor_NullCaptures_DocumentsNoValidation_AllowedOrNullReference()
        {
            // Production does not guard against null captures — stored as null if passed.
            // This documents current behavior; if a guard is added, change to Assert.Throws<ArgumentNullException>.
            var module = TestData.LoadTD27();
            var schema = module.Schema;
            var format = new AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(1);
            // Use null! to bypass compiler; runtime should store null (no throw) — verify.
            var moduleAudio = new ModuleAudio(schema, format, duration, null!);
            Assert.IsNull(moduleAudio.Captures);
        }
    }
}

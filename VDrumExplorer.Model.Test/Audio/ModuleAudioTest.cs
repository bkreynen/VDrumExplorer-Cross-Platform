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
    }
}

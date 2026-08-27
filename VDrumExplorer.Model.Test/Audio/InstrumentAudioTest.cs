// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Linq;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.Model.Test.Audio
{
    public class InstrumentAudioTest
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var module = TestData.LoadTD27();
            var instrument = module.Schema.PresetInstruments.First();
            var audio = new byte[] { 1, 2, 3, 4, 5 };

            var instrumentAudio = new InstrumentAudio(instrument, audio);

            Assert.AreSame(instrument, instrumentAudio.Instrument);
            Assert.AreSame(audio, instrumentAudio.Audio);
        }

        [Test]
        public void Constructor_AcceptsEmptyAudio()
        {
            var module = TestData.LoadTD27();
            var instrument = module.Schema.PresetInstruments.First();
            var audio = new byte[0];

            var instrumentAudio = new InstrumentAudio(instrument, audio);

            Assert.AreSame(instrument, instrumentAudio.Instrument);
            Assert.AreEqual(0, instrumentAudio.Audio.Length);
        }
    }
}

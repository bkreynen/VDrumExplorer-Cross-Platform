// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using VDrumExplorer.Model.Audio;

namespace VDrumExplorer.Model.Test.Audio
{
    public class AudioFormatTest
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var format = new AudioFormat(44100, 2, 16);
            Assert.AreEqual(44100, format.Frequency);
            Assert.AreEqual(2, format.Channels);
            Assert.AreEqual(16, format.Bits);
        }

        [Test]
        public void BytesPerSecond_TypicalCdQuality()
        {
            // CD-quality audio: 44100 Hz, 2 channels, 16 bits = 176400 bytes/sec
            var format = new AudioFormat(44100, 2, 16);
            Assert.AreEqual(176400, format.BytesPerSecond);
        }

        [Test]
        public void BytesPerSecond_Mono8Bit()
        {
            // Mono, 8-bit, 8000 Hz = 8000 bytes/sec
            var format = new AudioFormat(8000, 1, 8);
            Assert.AreEqual(8000, format.BytesPerSecond);
        }

        [Test]
        public void BytesPerSecond_Stereo8Bit()
        {
            // Stereo, 8-bit, 8000 Hz = 16000 bytes/sec
            var format = new AudioFormat(8000, 2, 8);
            Assert.AreEqual(16000, format.BytesPerSecond);
        }

        [Test]
        public void BytesPerSecond_Mono16Bit()
        {
            // Mono, 16-bit, 44100 Hz = 88200 bytes/sec
            var format = new AudioFormat(44100, 1, 16);
            Assert.AreEqual(88200, format.BytesPerSecond);
        }

        [Test]
        public void BytesPerSecond_24Bit()
        {
            // 24-bit, 48000 Hz, 2 channels = 288000 bytes/sec
            var format = new AudioFormat(48000, 2, 24);
            Assert.AreEqual(288000, format.BytesPerSecond);
        }
    }
}

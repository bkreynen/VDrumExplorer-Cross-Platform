// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;

namespace VDrumExplorer.Proto.Test
{
    public class AudioFormatProtoTest
    {
        [Test]
        public void ToModel_CreatesCorrectAudioFormat()
        {
            var protoFormat = new AudioFormat
            {
                Frequency = 44100,
                Channels = 2,
                Bits = 16
            };
            var modelFormat = protoFormat.ToModel();
            Assert.AreEqual(44100, modelFormat.Frequency);
            Assert.AreEqual(2, modelFormat.Channels);
            Assert.AreEqual(16, modelFormat.Bits);
        }

        [Test]
        public void FromModel_CreatesCorrectProtoAudioFormat()
        {
            var modelFormat = new Model.Audio.AudioFormat(48000, 1, 24);
            var protoFormat = AudioFormat.FromModel(modelFormat);
            Assert.AreEqual(48000, protoFormat.Frequency);
            Assert.AreEqual(1, protoFormat.Channels);
            Assert.AreEqual(24, protoFormat.Bits);
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_PreservesAllValues()
        {
            var modelFormat = new Model.Audio.AudioFormat(44100, 2, 16);
            var protoFormat = AudioFormat.FromModel(modelFormat);
            var result = protoFormat.ToModel();
            Assert.AreEqual(modelFormat.Frequency, result.Frequency);
            Assert.AreEqual(modelFormat.Channels, result.Channels);
            Assert.AreEqual(modelFormat.Bits, result.Bits);
        }

        [Test]
        public void RoundTrip_ToModelThenFromModel_PreservesAllValues()
        {
            var protoFormat = new AudioFormat
            {
                Frequency = 96000,
                Channels = 2,
                Bits = 32
            };
            var modelFormat = protoFormat.ToModel();
            var result = AudioFormat.FromModel(modelFormat);
            Assert.AreEqual(protoFormat.Frequency, result.Frequency);
            Assert.AreEqual(protoFormat.Channels, result.Channels);
            Assert.AreEqual(protoFormat.Bits, result.Bits);
        }

        [Test]
        public void ToModel_BytesPerSecondIsCorrect()
        {
            var protoFormat = new AudioFormat
            {
                Frequency = 44100,
                Channels = 2,
                Bits = 16
            };
            var modelFormat = protoFormat.ToModel();
            // 44100 Hz * 2 channels * 16 bits / 8 = 176400 bytes/sec — literal avoids re-implementing formula.
            Assert.AreEqual(176400, modelFormat.BytesPerSecond);
        }
    }
}

// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Google.Protobuf;
using NUnit.Framework;

namespace VDrumExplorer.Proto.Test
{
    public class InstrumentAudioProtoTest
    {
        private Model.ModuleSchema schema = null!;

        [SetUp]
        public void SetUp()
        {
            schema = TestData.LoadTD27Module().Schema;
        }

        [Test]
        public void FromModel_CreatesProtoInstrumentAudioWithCorrectInstrumentId()
        {
            var instrument = schema.PresetInstruments[5];
            var audioData = new byte[] { 1, 2, 3, 4, 5 };
            var modelAudio = new Model.Audio.InstrumentAudio(instrument, audioData);
            var protoAudio = InstrumentAudio.FromModel(modelAudio);
            Assert.AreEqual(instrument.Id, protoAudio.InstrumentId);
        }

        [Test]
        public void FromModel_CreatesProtoInstrumentAudioWithCorrectPresetFlag()
        {
            var instrument = schema.PresetInstruments[0];
            var audioData = new byte[] { 1, 2, 3 };
            var modelAudio = new Model.Audio.InstrumentAudio(instrument, audioData);
            var protoAudio = InstrumentAudio.FromModel(modelAudio);
            Assert.IsTrue(protoAudio.Preset);
        }

        [Test]
        public void FromModel_CreatesProtoInstrumentAudioWithCorrectAudioData()
        {
            var instrument = schema.PresetInstruments[3];
            var audioData = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var modelAudio = new Model.Audio.InstrumentAudio(instrument, audioData);
            var protoAudio = InstrumentAudio.FromModel(modelAudio);
            Assert.That(protoAudio.AudioData.ToByteArray(), Is.EqualTo(audioData));
        }

        [Test]
        public void ToModel_CreatesInstrumentAudioWithCorrectInstrument()
        {
            var instrument = schema.PresetInstruments[7];
            var protoAudio = new InstrumentAudio
            {
                InstrumentId = instrument.Id,
                Preset = true,
                AudioData = ByteString.CopyFrom(new byte[] { 1, 2, 3 })
            };
            var modelAudio = protoAudio.ToModel(schema);
            Assert.AreEqual(instrument.Id, modelAudio.Instrument.Id);
            Assert.AreEqual(instrument.Name, modelAudio.Instrument.Name);
        }

        [Test]
        public void ToModel_CreatesInstrumentAudioWithCorrectAudio()
        {
            var audioData = new byte[] { 0xAA, 0xBB, 0xCC };
            var protoAudio = new InstrumentAudio
            {
                InstrumentId = 0,
                Preset = true,
                AudioData = ByteString.CopyFrom(audioData)
            };
            var modelAudio = protoAudio.ToModel(schema);
            Assert.That(modelAudio.Audio, Is.EqualTo(audioData));
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_PreservesValues()
        {
            var instrument = schema.PresetInstruments[10];
            var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var modelAudio = new Model.Audio.InstrumentAudio(instrument, audioData);
            var protoAudio = InstrumentAudio.FromModel(modelAudio);
            var result = protoAudio.ToModel(schema);
            Assert.AreEqual(modelAudio.Instrument.Id, result.Instrument.Id);
            Assert.AreEqual(modelAudio.Instrument.Name, result.Instrument.Name);
            Assert.That(result.Audio, Is.EqualTo(modelAudio.Audio));
        }

        [Test]
        public void RoundTrip_ToModelThenFromModel_PreservesValues()
        {
            var audioData = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC };
            var protoAudio = new InstrumentAudio
            {
                InstrumentId = 2,
                Preset = true,
                AudioData = ByteString.CopyFrom(audioData)
            };
            var modelAudio = protoAudio.ToModel(schema);
            var result = InstrumentAudio.FromModel(modelAudio);
            Assert.AreEqual(protoAudio.InstrumentId, result.InstrumentId);
            Assert.AreEqual(protoAudio.Preset, result.Preset);
            Assert.That(result.AudioData.ToByteArray(), Is.EqualTo(audioData));
        }

        [Test]
        public void FromModel_UserSampleInstrument_SetsPresetToFalse()
        {
            // TD-27 has user samples
            if (schema.UserSampleInstruments.Count == 0)
            {
                Assert.Ignore("Module has no user sample instruments");
            }
            var instrument = schema.UserSampleInstruments[0];
            var modelAudio = new Model.Audio.InstrumentAudio(instrument, new byte[] { 1 });
            var protoAudio = InstrumentAudio.FromModel(modelAudio);
            Assert.IsFalse(protoAudio.Preset);
        }
    }
}

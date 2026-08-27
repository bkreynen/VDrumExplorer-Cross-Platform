// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Linq;

namespace VDrumExplorer.Proto.Test
{
    public class ModuleAudioProtoTest
    {
        private Model.ModuleSchema schema = null!;

        [SetUp]
        public void SetUp()
        {
            schema = TestData.LoadTD27Module().Schema;
        }

        [Test]
        public void FromModel_CreatesProtoModuleAudioWithCorrectIdentifier()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            Assert.IsNotNull(protoAudio.Identifier);
            Assert.AreEqual(schema.Identifier.Name, protoAudio.Identifier.Name);
            Assert.AreEqual(schema.Identifier.ModelId, protoAudio.Identifier.ModelId);
        }

        [Test]
        public void FromModel_CreatesProtoModuleAudioWithCorrectFormat()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            Assert.AreEqual(audio.Format.Frequency, protoAudio.Format.Frequency);
            Assert.AreEqual(audio.Format.Channels, protoAudio.Format.Channels);
            Assert.AreEqual(audio.Format.Bits, protoAudio.Format.Bits);
        }

        [Test]
        public void FromModel_CreatesProtoModuleAudioWithCorrectDuration()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            Assert.AreEqual(audio.DurationPerInstrument, protoAudio.DurationPerInstrument.ToTimeSpan());
        }

        [Test]
        public void FromModel_CreatesProtoModuleAudioWithCorrectCaptures()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            Assert.AreEqual(audio.Captures.Count, protoAudio.InstrumentCaptures.Count);
        }

        [Test]
        public void ToModel_CreatesModelModuleAudioWithCorrectSchema()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            var result = protoAudio.ToModel(NullLogger.Instance);
            Assert.AreEqual(schema.Identifier, result.Schema.Identifier);
        }

        [Test]
        public void ToModel_CreatesModelModuleAudioWithCorrectFormat()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            var result = protoAudio.ToModel(NullLogger.Instance);
            Assert.AreEqual(audio.Format.Frequency, result.Format.Frequency);
            Assert.AreEqual(audio.Format.Channels, result.Format.Channels);
            Assert.AreEqual(audio.Format.Bits, result.Format.Bits);
        }

        [Test]
        public void ToModel_CreatesModelModuleAudioWithCorrectDuration()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            var result = protoAudio.ToModel(NullLogger.Instance);
            Assert.AreEqual(audio.DurationPerInstrument, result.DurationPerInstrument);
        }

        [Test]
        public void ToModel_CreatesModelModuleAudioWithCorrectCaptures()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            var result = protoAudio.ToModel(NullLogger.Instance);
            Assert.AreEqual(audio.Captures.Count, result.Captures.Count);
            for (int i = 0; i < audio.Captures.Count; i++)
            {
                Assert.AreEqual(audio.Captures[i].Instrument.Id, result.Captures[i].Instrument.Id);
                Assert.That(result.Captures[i].Audio, Is.EqualTo(audio.Captures[i].Audio));
            }
        }

        [Test]
        public void RoundTrip_FromModelThenToModel_PreservesAllValues()
        {
            var audio = CreateSampleModuleAudio();
            var protoAudio = ModuleAudio.FromModel(audio);
            var result = protoAudio.ToModel(NullLogger.Instance);

            Assert.AreEqual(audio.Schema.Identifier, result.Schema.Identifier);
            Assert.AreEqual(audio.Format.Frequency, result.Format.Frequency);
            Assert.AreEqual(audio.Format.Channels, result.Format.Channels);
            Assert.AreEqual(audio.Format.Bits, result.Format.Bits);
            Assert.AreEqual(audio.DurationPerInstrument, result.DurationPerInstrument);
            Assert.AreEqual(audio.Captures.Count, result.Captures.Count);

            for (int i = 0; i < audio.Captures.Count; i++)
            {
                Assert.AreEqual(audio.Captures[i].Instrument.Id, result.Captures[i].Instrument.Id);
                Assert.That(result.Captures[i].Audio, Is.EqualTo(audio.Captures[i].Audio));
            }
        }

        [Test]
        public void RoundTrip_ToModelThenFromModel_PreservesAllValues()
        {
            var protoAudio = new ModuleAudio
            {
                Identifier = ModuleIdentifier.FromModel(schema.Identifier),
                Format = new AudioFormat { Frequency = 48000, Channels = 1, Bits = 24 },
                DurationPerInstrument = Duration.FromTimeSpan(TimeSpan.FromSeconds(3)),
                InstrumentCaptures =
                {
                    new InstrumentAudio { InstrumentId = 0, Preset = true, AudioData = Google.Protobuf.ByteString.CopyFrom(new byte[] { 1, 2 }) },
                    new InstrumentAudio { InstrumentId = 1, Preset = true, AudioData = Google.Protobuf.ByteString.CopyFrom(new byte[] { 3, 4 }) },
                }
            };
            var modelAudio = protoAudio.ToModel(NullLogger.Instance);
            var result = ModuleAudio.FromModel(modelAudio);

            Assert.AreEqual(protoAudio.Identifier.Name, result.Identifier.Name);
            Assert.AreEqual(protoAudio.Identifier.ModelId, result.Identifier.ModelId);
            Assert.AreEqual(protoAudio.Format.Frequency, result.Format.Frequency);
            Assert.AreEqual(protoAudio.Format.Channels, result.Format.Channels);
            Assert.AreEqual(protoAudio.Format.Bits, result.Format.Bits);
            Assert.AreEqual(protoAudio.DurationPerInstrument.ToTimeSpan(), result.DurationPerInstrument.ToTimeSpan());
            Assert.AreEqual(protoAudio.InstrumentCaptures.Count, result.InstrumentCaptures.Count);

            for (int i = 0; i < protoAudio.InstrumentCaptures.Count; i++)
            {
                Assert.AreEqual(protoAudio.InstrumentCaptures[i].InstrumentId, result.InstrumentCaptures[i].InstrumentId);
                Assert.AreEqual(protoAudio.InstrumentCaptures[i].Preset, result.InstrumentCaptures[i].Preset);
                Assert.That(result.InstrumentCaptures[i].AudioData.ToByteArray(),
                    Is.EqualTo(protoAudio.InstrumentCaptures[i].AudioData.ToByteArray()));
            }
        }

        private Model.Audio.ModuleAudio CreateSampleModuleAudio()
        {
            var format = new Model.Audio.AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(2);
            var captures = new[]
            {
                new Model.Audio.InstrumentAudio(schema.PresetInstruments[0], new byte[] { 1, 2, 3 }),
                new Model.Audio.InstrumentAudio(schema.PresetInstruments[1], new byte[] { 4, 5, 6 }),
            }.ToList().AsReadOnly();
            return new Model.Audio.ModuleAudio(schema, format, duration, captures);
        }
    }
}

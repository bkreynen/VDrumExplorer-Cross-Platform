// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Proto.Test
{
    public class ProtoIoTest
    {
        private static ILogger Logger => NullLogger.Instance;

        [Test]
        public void ReadModel_ModuleStream_RoundTripsData()
        {
            var module = TestData.LoadTD27Module();
            using var stream = new MemoryStream();
            module.Save(stream);
            stream.Position = 0;
            var result = (Model.Module)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(module.Data, result.Data);
        }

        [Test]
        public void ReadModel_KitStream_RoundTripsData()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            using var stream = new MemoryStream();
            kit.Save(stream);
            stream.Position = 0;
            var result = (Model.Kit)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(kit.Data, result.Data);
        }

        [Test]
        public void LoadModel_TempFile_RoundTripsModule()
        {
            var module = TestData.LoadTD27Module();
            var tempFile = Path.GetTempFileName();
            try
            {
                using (var file = File.OpenWrite(tempFile))
                {
                    module.Save(file);
                }
                var result = (Model.Module)ProtoIo.LoadModel(tempFile, Logger);
                AssertDataEqual(module.Data, result.Data);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void LoadModel_TempFile_RoundTripsKit()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            var tempFile = Path.GetTempFileName();
            try
            {
                using (var file = File.OpenWrite(tempFile))
                {
                    kit.Save(file);
                }
                var result = (Model.Kit)ProtoIo.LoadModel(tempFile, Logger);
                AssertDataEqual(kit.Data, result.Data);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void ReadDrumFile_InvalidMagicBytes_ThrowsInvalidDataException()
        {
            using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });
            Assert.Throws<InvalidDataException>(() => ProtoIo.ReadModel(stream, Logger));
        }

        [Test]
        public void ReadModel_NoFileCase_ThrowsInvalidDataException()
        {
            // DrumFile with no oneof set has FileCase == None; craft magic + empty protobuf.
            var magic = Encoding.UTF8.GetBytes("JLSVDRUM1");
            using var stream = new MemoryStream(magic);
            // No additional bytes — DrumFile.Parser.ParseFrom will return default instance with FileCase None.
            var ex = Assert.Throws<InvalidDataException>(() => ProtoIo.ReadModel(stream, NullLogger.Instance));
            Assert.That(ex!.Message, Does.Contain("Unknown file case"));
            Assert.That(ex.Message, Does.Contain("None"));
        }

        [Test]
        public void ReadDrumFile_InvalidMagicAtLastByte_ThrowsInvalidDataException_ReportsIndex8()
        {
            var bad = Encoding.UTF8.GetBytes("JLSVDRUM0");
            using var stream = new MemoryStream(bad);
            var ex = Assert.Throws<InvalidDataException>(() => ProtoIo.ReadModel(stream, Logger));
            Assert.That(ex!.Message, Does.Contain("Index=8"));
        }

        [Test]
        public void ReadDrumFile_EmptyStream_ThrowsEndOfStreamException()
        {
            using var stream = new MemoryStream();
            Assert.Throws<EndOfStreamException>(() => ProtoIo.ReadModel(stream, Logger));
        }

        [Test]
        public void ReadDrumFile_PartialMagicBytes_ThrowsEndOfStreamException()
        {
            // Only the first few bytes of the magic string, not all of "JLSVDRUM1"
            var partialMagic = Encoding.UTF8.GetBytes("JLSV");
            using var stream = new MemoryStream(partialMagic);
            Assert.Throws<EndOfStreamException>(() => ProtoIo.ReadModel(stream, Logger));
        }

        [Test]
        public void Write_ReadModel_RoundTrip_Module()
        {
            var module = TestData.LoadTD27Module();
            using var stream = new MemoryStream();
            ProtoIo.Write(stream, module);
            stream.Position = 0;
            var result = (Model.Module)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(module.Data, result.Data);
        }

        [Test]
        public void Write_ReadModel_RoundTrip_Kit()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            using var stream = new MemoryStream();
            ProtoIo.Write(stream, kit);
            stream.Position = 0;
            var result = (Model.Kit)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(kit.Data, result.Data);
        }

        [Test]
        public void Save_ExtensionMethod_Module()
        {
            var module = TestData.LoadTD27Module();
            using var stream = new MemoryStream();
            module.Save(stream);
            Assert.Greater(stream.Length, 0);
            stream.Position = 0;
            var result = (Model.Module)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(module.Data, result.Data);
        }

        [Test]
        public void Save_ExtensionMethod_Kit()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            using var stream = new MemoryStream();
            kit.Save(stream);
            Assert.Greater(stream.Length, 0);
            stream.Position = 0;
            var result = (Model.Kit)ProtoIo.ReadModel(stream, Logger);
            AssertDataEqual(kit.Data, result.Data);
        }

        [Test]
        public void Save_ExtensionMethod_ModuleAudio()
        {
            var module = TestData.LoadTD27Module();
            var schema = module.Schema;
            var format = new Model.Audio.AudioFormat(44100, 2, 16);
            var duration = TimeSpan.FromSeconds(2);
            var instrument = schema.PresetInstruments[0];
            var capture = new Model.Audio.InstrumentAudio(instrument, new byte[] { 1, 2, 3 });
            var audio = new Model.Audio.ModuleAudio(schema, format, duration, new[] { capture }.ToList().AsReadOnly());

            using var stream = new MemoryStream();
            audio.Save(stream);
            Assert.Greater(stream.Length, 0);
            stream.Position = 0;
            var result = (Model.Audio.ModuleAudio)ProtoIo.ReadModel(stream, Logger);
            Assert.AreEqual(audio.Schema.Identifier, result.Schema.Identifier);
            Assert.AreEqual(audio.Format.Frequency, result.Format.Frequency);
            Assert.AreEqual(audio.Format.Channels, result.Format.Channels);
            Assert.AreEqual(audio.Format.Bits, result.Format.Bits);
            Assert.AreEqual(audio.DurationPerInstrument, result.DurationPerInstrument);
            Assert.AreEqual(audio.Captures.Count, result.Captures.Count);
        }

        [Test]
        public void Write_VerifyMagicBytes()
        {
            var module = TestData.LoadTD27Module();
            using var stream = new MemoryStream();
            module.Save(stream);
            stream.Position = 0;
            var expectedMagic = Encoding.UTF8.GetBytes("JLSVDRUM1");
            var buffer = new byte[expectedMagic.Length];
            int read = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(expectedMagic.Length, read);
            Assert.That(buffer, Is.EqualTo(expectedMagic));
        }

        [Test]
        public void MultiRoundTrip_Module_IsStable()
        {
            var module = TestData.LoadTD27Module();
            using var stream1 = new MemoryStream();
            module.Save(stream1);
            stream1.Position = 0;
            var firstLoad = (Model.Module)ProtoIo.ReadModel(stream1, Logger);

            using var stream2 = new MemoryStream();
            firstLoad.Save(stream2);
            stream2.Position = 0;
            var secondLoad = (Model.Module)ProtoIo.ReadModel(stream2, Logger);

            AssertDataEqual(firstLoad.Data, secondLoad.Data);
        }

        [Test]
        public void MultiRoundTrip_Kit_IsStable()
        {
            var kit = TestData.LoadTD27Module().ExportKit(1);
            using var stream1 = new MemoryStream();
            kit.Save(stream1);
            stream1.Position = 0;
            var firstLoad = (Model.Kit)ProtoIo.ReadModel(stream1, Logger);

            using var stream2 = new MemoryStream();
            firstLoad.Save(stream2);
            stream2.Position = 0;
            var secondLoad = (Model.Kit)ProtoIo.ReadModel(stream2, Logger);

            AssertDataEqual(firstLoad.Data, secondLoad.Data);
        }

        private static void AssertDataEqual(ModuleData expectedData, ModuleData actualData)
        {
            var originalSegments = expectedData.CreateSnapshot().Segments.ToList();
            var newSegments = actualData.CreateSnapshot().Segments.ToList();
            Assert.AreEqual(originalSegments.Count, newSegments.Count);

            for (int i = 0; i < originalSegments.Count; i++)
            {
                var originalSegment = originalSegments[i];
                var newSegment = newSegments[i];
                Assert.AreEqual(originalSegment.Address, newSegment.Address, $"Address of segment {i}");
                Assert.AreEqual(originalSegment.Size, newSegment.Size, $"Size of segment {i}");
                Assert.AreEqual(originalSegment.CopyData(), newSegment.CopyData(), $"Data in segment starting at {originalSegment.Address}");
            }
        }
    }
}

// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Proto;

namespace VDrumExplorer.Model.Test.Proto;

public class ProtoIoTest
{
    private static ILogger Logger => NullLogger.Instance;

    [Test]
    public void ReadModel_Module_RoundTripsData()
    {
        var module = TestData.LoadTD27();
        using var stream = new MemoryStream();
        module.Save(stream);
        stream.Position = 0;
        var result = (Module)ProtoIo.ReadModel(stream, Logger);
        AssertDataEqual(module.Data, result.Data);
    }

    [Test]
    public void ReadModel_Kit_RoundTripsData()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        using var stream = new MemoryStream();
        kit.Save(stream);
        stream.Position = 0;
        var result = (Kit)ProtoIo.ReadModel(stream, Logger);
        AssertDataEqual(kit.Data, result.Data);
    }

    [Test]
    public void ReadModel_Module_PreservesSchemaIdentifier()
    {
        var module = TestData.LoadTD27();
        using var stream = new MemoryStream();
        module.Save(stream);
        stream.Position = 0;
        var result = (Module)ProtoIo.ReadModel(stream, Logger);
        Assert.AreEqual(module.Schema.Identifier, result.Schema.Identifier);
    }

    [Test]
    public void ReadModel_Kit_PreservesDefaultKitNumber()
    {
        var kit = TestData.LoadTD27().ExportKit(7);
        using var stream = new MemoryStream();
        kit.Save(stream);
        stream.Position = 0;
        var result = (Kit)ProtoIo.ReadModel(stream, Logger);
        Assert.AreEqual(kit.DefaultKitNumber, result.DefaultKitNumber);
    }

    [Test]
    public void ReadModel_Module_PreservesSchemaIdentifierAndData()
    {
        var module = TestData.LoadTD27();
        using var stream = new MemoryStream();
        module.Save(stream);
        stream.Position = 0;
        var result = (Module)ProtoIo.ReadModel(stream, Logger);
        Assert.AreEqual(module.Schema.Identifier, result.Schema.Identifier);
        AssertDataEqual(module.Data, result.Data);
    }

    [Test]
    public void ReadModel_Kit_PreservesSchemaIdentifierAndData()
    {
        var kit = TestData.LoadTD27().ExportKit(3);
        using var stream = new MemoryStream();
        kit.Save(stream);
        stream.Position = 0;
        var result = (Kit)ProtoIo.ReadModel(stream, Logger);
        Assert.AreEqual(kit.Schema.Identifier, result.Schema.Identifier);
        AssertDataEqual(kit.Data, result.Data);
    }

    [Test]
    public void LoadModel_FromFile_RoundTripsModule()
    {
        var module = TestData.LoadTD27();
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var file = File.OpenWrite(tempFile))
            {
                module.Save(file);
            }
            var result = (Module)ProtoIo.LoadModel(tempFile, Logger);
            AssertDataEqual(module.Data, result.Data);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadModel_FromFile_RoundTripsKit()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var file = File.OpenWrite(tempFile))
            {
                kit.Save(file);
            }
            var result = (Kit)ProtoIo.LoadModel(tempFile, Logger);
            AssertDataEqual(kit.Data, result.Data);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void Save_Module_WritesNonEmptyStream()
    {
        var module = TestData.LoadTD27();
        using var stream = new MemoryStream();
        module.Save(stream);
        Assert.Greater(stream.Length, 0);
    }

    [Test]
    public void Save_Kit_WritesNonEmptyStream()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        using var stream = new MemoryStream();
        kit.Save(stream);
        Assert.Greater(stream.Length, 0);
    }

    [Test]
    public void ReadModel_StreamWithMissingMagic_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });
        Assert.Throws<InvalidDataException>(() => ProtoIo.ReadModel(stream, Logger));
    }

    [Test]
    public void ReadModel_EmptyStream_Throws()
    {
        using var stream = new MemoryStream();
        Assert.Throws<EndOfStreamException>(() => ProtoIo.ReadModel(stream, Logger));
    }

    [Test]
    public void ReadModel_MagicOnly_Throws()
    {
        // The magic string is "JLSVDRUM1"; after that, the protobuf parser expects a DrumFile.
        // An empty protobuf payload is valid but produces a DrumFile with no file case set,
        // which causes ReadModel to throw InvalidDataException for an unknown file case.
        var magicBytes = System.Text.Encoding.UTF8.GetBytes("JLSVDRUM1");
        using var stream = new MemoryStream(magicBytes);
        Assert.Throws<InvalidDataException>(() => ProtoIo.ReadModel(stream, Logger));
    }

    [Test]
    public void Save_Module_ProducesStreamStartingWithMagic()
    {
        var module = TestData.LoadTD27();
        using var stream = new MemoryStream();
        module.Save(stream);
        stream.Position = 0;
        var magic = System.Text.Encoding.UTF8.GetBytes("JLSVDRUM1");
        var buffer = new byte[magic.Length];
        int read = stream.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(magic.Length, read);
        Assert.That(buffer, Is.EqualTo(magic));
    }

    [Test]
    public void Save_Kit_ProducesStreamStartingWithMagic()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        using var stream = new MemoryStream();
        kit.Save(stream);
        stream.Position = 0;
        var magic = System.Text.Encoding.UTF8.GetBytes("JLSVDRUM1");
        var buffer = new byte[magic.Length];
        int read = stream.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(magic.Length, read);
        Assert.That(buffer, Is.EqualTo(magic));
    }

    [Test]
    public void RoundTrip_Module_MultipleTimesIsStable()
    {
        var module = TestData.LoadTD27();
        using var stream1 = new MemoryStream();
        module.Save(stream1);
        stream1.Position = 0;
        var firstLoad = (Module)ProtoIo.ReadModel(stream1, Logger);

        using var stream2 = new MemoryStream();
        firstLoad.Save(stream2);
        stream2.Position = 0;
        var secondLoad = (Module)ProtoIo.ReadModel(stream2, Logger);

        AssertDataEqual(firstLoad.Data, secondLoad.Data);
    }

    [Test]
    public void RoundTrip_Kit_MultipleTimesIsStable()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        using var stream1 = new MemoryStream();
        kit.Save(stream1);
        stream1.Position = 0;
        var firstLoad = (Kit)ProtoIo.ReadModel(stream1, Logger);

        using var stream2 = new MemoryStream();
        firstLoad.Save(stream2);
        stream2.Position = 0;
        var secondLoad = (Kit)ProtoIo.ReadModel(stream2, Logger);

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

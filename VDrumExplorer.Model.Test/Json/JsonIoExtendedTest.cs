// Copyright 2023 Jon Skeet. All rights reserved.
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
using VDrumExplorer.Model.Json;

namespace VDrumExplorer.Model.Test.Json;

public class JsonIoExtendedTest
{
    private static ILogger Logger => NullLogger.Instance;

    /// <summary>
    /// The identifier JSON for the TD-27, used to build valid test payloads.
    /// </summary>
    private static string Td27IdentifierJson =>
        $"\"name\": \"TD-27\", \"modelId\": {ModuleIdentifier.TD27.ModelId}, " +
        $"\"familyCode\": {ModuleIdentifier.TD27.FamilyCode}, " +
        $"\"familyNumberCode\": {ModuleIdentifier.TD27.FamilyNumberCode}, " +
        $"\"softwareRevision\": {ModuleIdentifier.TD27.SoftwareRevision}";

    [Test]
    public void ReadModel_InvalidJson_Throws()
    {
        Assert.Throws<Newtonsoft.Json.JsonReaderException>(
            () => JsonIo.ReadModel("not valid json", Logger));
    }

    [Test]
    public void ReadModel_MissingIdentifier_Throws()
    {
        string json = "{}";
        Assert.Throws<InvalidDataException>(() => JsonIo.ReadModel(json, Logger));
    }

    [Test]
    public void ReadModel_IdentifierNotObject_Throws()
    {
        string json = "{\"identifier\": \"not-an-object\"}";
        Assert.Throws<InvalidDataException>(() => JsonIo.ReadModel(json, Logger));
    }

    [Test]
    public void ReadModel_BothKitAndModuleData_Throws()
    {
        string json = $"{{ \"identifier\": {{{Td27IdentifierJson}}}, " +
            "\"kitData\": {}, \"moduleData\": {} }";
        Assert.Throws<InvalidDataException>(() => JsonIo.ReadModel(json, Logger));
    }

    [Test]
    public void ReadModel_NeitherKitNorModuleData_Throws()
    {
        string json = $"{{ \"identifier\": {{{Td27IdentifierJson}}} }}";
        Assert.Throws<InvalidDataException>(() => JsonIo.ReadModel(json, Logger));
    }

    [Test]
    public void ReadModel_KitDataWithoutDefaultKitNumber_Throws()
    {
        string json = $"{{ \"identifier\": {{{Td27IdentifierJson}}}, \"kitData\": {{}} }}";
        Assert.Throws<InvalidDataException>(() => JsonIo.ReadModel(json, Logger));
    }

    [Test]
    public void ReadModel_TextReaderOverload_RoundTripsModule()
    {
        var module = TestData.LoadTD27();
        string json = module.ToJson();
        using var reader = new StringReader(json);
        var result = (Module)JsonIo.ReadModel(reader, Logger);
        AssertDataEqual(module.Data, result.Data);
    }

    [Test]
    public void ReadModel_TextReaderOverload_RoundTripsKit()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        string json = kit.ToJson();
        using var reader = new StringReader(json);
        var result = (Kit)JsonIo.ReadModel(reader, Logger);
        AssertDataEqual(kit.Data, result.Data);
    }

    [Test]
    public void LoadModel_FromFile_RoundTripsModule()
    {
        var module = TestData.LoadTD27();
        string json = module.ToJson();
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, json);
            var result = (Module)JsonIo.LoadModel(tempFile, Logger);
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
        string json = kit.ToJson();
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, json);
            var result = (Kit)JsonIo.LoadModel(tempFile, Logger);
            AssertDataEqual(kit.Data, result.Data);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ToJson_Module_ReturnsNonEmptyString()
    {
        var module = TestData.LoadTD27();
        string json = module.ToJson();
        Assert.IsFalse(string.IsNullOrEmpty(json));
        // Sanity check: the JSON should contain the identifier and moduleData properties.
        Assert.IsTrue(json.Contains("\"identifier\""));
        Assert.IsTrue(json.Contains("\"moduleData\""));
    }

    [Test]
    public void ToJson_Kit_ReturnsNonEmptyString()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        string json = kit.ToJson();
        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("\"identifier\""));
        Assert.IsTrue(json.Contains("\"kitData\""));
        Assert.IsTrue(json.Contains("\"defaultKitNumber\""));
    }

    [Test]
    public void SaveAsJson_Module_WritesToWriter()
    {
        var module = TestData.LoadTD27();
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        module.SaveAsJson(writer);
        string json = sb.ToString();
        Assert.IsFalse(string.IsNullOrEmpty(json));
        // Verify the written JSON can be read back.
        var result = (Module)JsonIo.ReadModel(json, Logger);
        AssertDataEqual(module.Data, result.Data);
    }

    [Test]
    public void SaveAsJson_Kit_WritesToWriter()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        kit.SaveAsJson(writer);
        string json = sb.ToString();
        Assert.IsFalse(string.IsNullOrEmpty(json));
        var result = (Kit)JsonIo.ReadModel(json, Logger);
        AssertDataEqual(kit.Data, result.Data);
    }

    [Test]
    public void ToJson_Module_IsConsistentWithSaveAsJson()
    {
        var module = TestData.LoadTD27();
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        module.SaveAsJson(writer);
        Assert.AreEqual(sb.ToString(), module.ToJson());
    }

    [Test]
    public void ToJson_Kit_IsConsistentWithSaveAsJson()
    {
        var kit = TestData.LoadTD27().ExportKit(1);
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        kit.SaveAsJson(writer);
        Assert.AreEqual(sb.ToString(), kit.ToJson());
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

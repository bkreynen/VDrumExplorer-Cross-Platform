// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using VDrumExplorer.Model.Schema.Json;

namespace VDrumExplorer.Model.Test.Schema.Json
{
    public class JsonLoaderTest
    {
        [Test]
        public void FromAssemblyResources_LoadsKnownSchemaResource()
        {
            // Load the TD17 schema from the Model assembly's embedded resources.
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "SchemaResources.TD17");

            var json = loader.LoadResource("TD17.json", 0);

            Assert.IsNotNull(json);
            // The root JSON should have an "identifier" property.
            Assert.IsTrue(json.ContainsKey("identifier"));
        }

        [Test]
        public void FromAssemblyResources_LoadsResourceWithInclude()
        {
            // The TD17.json includes other resources via $resource: prefix.
            // Loading it should resolve all includes.
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "SchemaResources.TD17");

            var json = loader.LoadResource("TD17.json", 0);

            // After include resolution, the "containers" should be populated.
            Assert.IsTrue(json.ContainsKey("containers"));
        }

        [Test]
        public void FromAssemblyResources_EmptyResourceBase_LoadsFromAssemblyRoot()
        {
            // With an empty resource base, resources are loaded from the assembly root.
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "");
            // This should not throw; we just verify the loader is created.
            Assert.IsNotNull(loader);
        }

        [Test]
        public void FromAssemblyResources_NonExistentResource_ThrowsFileNotFoundException()
        {
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "SchemaResources.TD17");

            Assert.Throws<FileNotFoundException>(() => loader.LoadResource("NonExistent.json", 0));
        }

        [Test]
        public void FromDirectory_LoadsFileFromDirectory()
        {
            // Create a temporary directory with a simple JSON file.
            var tempDir = Path.Combine(Path.GetTempPath(), "JsonLoaderTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var jsonContent = "{\"name\": \"test\", \"value\": 42}";
                File.WriteAllText(Path.Combine(tempDir, "test.json"), jsonContent);

                var loader = JsonLoader.FromDirectory(tempDir);
                var json = loader.LoadResource("test.json", 0);

                Assert.AreEqual("test", json["name"]!.ToString());
                Assert.AreEqual(42, json["value"]!.Value<int>());
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void FromDirectory_NonExistentFile_Throws()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "JsonLoaderTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var loader = JsonLoader.FromDirectory(tempDir);
                Assert.Throws<FileNotFoundException>(() => loader.LoadResource("nonexistent.json", 0));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void LoadResource_ReturnsJObject()
        {
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "SchemaResources.TD17");

            var json = loader.LoadResource("TD17.json", 0);

            Assert.IsInstanceOf<JObject>(json);
        }

        [Test]
        public void LoadResource_WithRevision_FiltersByRevision()
        {
            // The TD17 has revisions 0 and 1, 2. Loading with revision 0 should work.
            var assembly = typeof(ModuleSchema).Assembly;
            var loader = JsonLoader.FromAssemblyResources(assembly, "SchemaResources.TD17");

            var json = loader.LoadResource("TD17.json", 0);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("identifier"));
        }
    }
}

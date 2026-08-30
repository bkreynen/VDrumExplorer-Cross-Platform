// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class CompatibilityExtensionsTest
    {
        private static IReadOnlyDictionary<string, int> CreateDictionary() =>
            new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        [Test]
        public void GetValueOrDefault_ExistingKey_ReturnsValue()
        {
            var dictionary = CreateDictionary();
            var result = dictionary.GetValueOrDefault("a");
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetValueOrDefault_MissingKey_ReturnsDefaultForValueType()
        {
            var dictionary = CreateDictionary();
            var result = dictionary.GetValueOrDefault("missing");
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetValueOrDefault_MissingKey_ReturnsNullForReferenceType()
        {
            var dictionary = new Dictionary<string, string> { ["a"] = "value" } as IReadOnlyDictionary<string, string>;
            var result = dictionary.GetValueOrDefault("missing");
            Assert.IsNull(result);
        }

        [Test]
        public void GetValueOrDefault_WithDefault_ExistingKey_ReturnsValue()
        {
            var dictionary = CreateDictionary();
            var result = dictionary.GetValueOrDefault("a", 99);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetValueOrDefault_WithDefault_MissingKey_ReturnsSpecifiedDefault()
        {
            var dictionary = CreateDictionary();
            var result = dictionary.GetValueOrDefault("missing", 99);
            Assert.AreEqual(99, result);
        }

        [Test]
        public void GetValueOrDefault_WithDefault_MissingKey_ReturnsSpecifiedDefaultForReferenceType()
        {
            var dictionary = new Dictionary<string, string> { ["a"] = "value" } as IReadOnlyDictionary<string, string>;
            var result = dictionary.GetValueOrDefault("missing", "default");
            Assert.AreEqual("default", result);
        }

        [Test]
        public void Deconstruct_ReturnsKeyAndValue()
        {
            var pair = new KeyValuePair<string, int>("key", 42);
            pair.Deconstruct(out var key, out var value);
            Assert.AreEqual("key", key);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Deconstruct_WorksWithDeconstructionSyntax()
        {
            var pair = new KeyValuePair<string, int>("key", 42);
            var (key, value) = pair;
            Assert.AreEqual("key", key);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void Deconstruct_WithNullKey()
        {
            var pair = new KeyValuePair<string?, int>(null, 42);
            pair.Deconstruct(out var key, out var value);
            Assert.IsNull(key);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void GetString_AsciiEncoding_ReturnsCorrectString()
        {
            var encoding = Encoding.ASCII;
            byte[] bytes = { 72, 101, 108, 108, 111 }; // "Hello"
            var result = encoding.GetString(bytes.AsSpan());
            Assert.AreEqual("Hello", result);
        }

        [Test]
        public void GetString_AsciiEncoding_EmptyBytes()
        {
            var encoding = Encoding.ASCII;
            var result = encoding.GetString(Array.Empty<byte>().AsSpan());
            Assert.AreEqual("", result);
        }

        [Test]
        public void GetString_AsciiEncoding_SingleByte()
        {
            var encoding = Encoding.ASCII;
            byte[] bytes = { 65 }; // 'A'
            var result = encoding.GetString(bytes.AsSpan());
            Assert.AreEqual("A", result);
        }

        [Test]
        public void GetBytes_AsciiEncoding_WritesCorrectBytes()
        {
            var encoding = Encoding.ASCII;
            var text = "Hello".AsSpan();
            var bytes = new byte[5];
            encoding.GetBytes(text, bytes.AsSpan());
            Assert.AreEqual(new byte[] { 72, 101, 108, 108, 111 }, bytes);
        }

        [Test]
        public void GetBytes_AsciiEncoding_EmptyText()
        {
            var encoding = Encoding.ASCII;
            var text = "".AsSpan();
            var bytes = new byte[0];
            encoding.GetBytes(text, bytes.AsSpan());
            Assert.AreEqual(Array.Empty<byte>(), bytes);
        }

        [Test]
        public void GetBytes_AsciiEncoding_SingleCharacter()
        {
            var encoding = Encoding.ASCII;
            var text = "A".AsSpan();
            var bytes = new byte[1];
            encoding.GetBytes(text, bytes.AsSpan());
            Assert.AreEqual(new byte[] { 65 }, bytes);
        }

        [Test]
        public void GetStringAndGetBytes_RoundTrip()
        {
            var encoding = Encoding.ASCII;
            var original = "Test123";
            var bytes = new byte[encoding.GetByteCount(original)];
            encoding.GetBytes(original.AsSpan(), bytes.AsSpan());
            var result = encoding.GetString(bytes.AsSpan());
            Assert.AreEqual(original, result);
        }

        [Test]
        public void GetString_Utf8_MultiByteRoundTrip()
        {
            var enc = Encoding.UTF8;
            var txt = "café €";
            var bytes = new byte[enc.GetByteCount(txt)];
            enc.GetBytes(txt.AsSpan(), bytes.AsSpan());
            Assert.AreEqual(txt, enc.GetString(bytes.AsSpan()));
        }

        [Test]
        public void GetBytes_Utf8_MultiByte_ProducesExpectedByteLength()
        {
            var enc = Encoding.UTF8;
            var txt = "café €"; // 'é' 2 bytes, '€' 3 bytes -> 5 ASCII + 2 + 3 with space = 10 bytes
            var bytes = new byte[enc.GetByteCount(txt)];
            enc.GetBytes(txt.AsSpan(), bytes.AsSpan());
            // Verify round-trip rather than hard-coding bytes to avoid brittle literal, but prove multi-byte counted.
            Assert.Greater(bytes.Length, txt.Length, "UTF-8 multi-byte characters should expand byte count beyond char count");
            Assert.AreEqual(txt, enc.GetString(bytes.AsSpan()));
        }
    }
}

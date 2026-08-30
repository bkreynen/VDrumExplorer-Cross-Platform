// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class DictionaryExtensionsTest
    {
        [Test]
        public void AsReadOnly_ReturnsReadOnlyDictionary()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
            var readOnly = dictionary.AsReadOnly();
            Assert.AreEqual(2, readOnly.Count);
        }

        [Test]
        public void AsReadOnly_ReflectsUnderlyingDictionary()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
            var readOnly = dictionary.AsReadOnly();
            Assert.AreEqual(2, readOnly.Count);
            Assert.AreEqual(1, readOnly["a"]);
            Assert.AreEqual(2, readOnly["b"]);
        }

        [Test]
        public void AsReadOnly_ReflectsChangesToUnderlyingDictionary()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.AreEqual(1, readOnly.Count);
            dictionary["c"] = 3;
            Assert.AreEqual(2, readOnly.Count);
            Assert.AreEqual(3, readOnly["c"]);
        }

        [Test]
        public void AsReadOnly_ThrowsOnAddAttempt()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.Throws<NotSupportedException>(() => ((IDictionary)readOnly).Add("b", 2));
        }

        [Test]
        public void AsReadOnly_ThrowsOnRemoveAttempt()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.Throws<NotSupportedException>(() => ((IDictionary)readOnly).Remove("a"));
        }

        [Test]
        public void AsReadOnly_ThrowsOnClearAttempt()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.Throws<NotSupportedException>(() => ((IDictionary)readOnly).Clear());
        }

        [Test]
        public void AsReadOnly_ThrowsOnIndexerWriteAttempt()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.Throws<NotSupportedException>(() => ((IDictionary)readOnly)["a"] = 5);
        }

        [Test]
        public void AsReadOnly_EmptyDictionary()
        {
            var dictionary = new Dictionary<string, int>();
            var readOnly = dictionary.AsReadOnly();
            Assert.AreEqual(0, readOnly.Count);
        }

        [Test]
        public void AsReadOnly_TryGetValue_ExistingKey()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.IsTrue(readOnly.TryGetValue("a", out var value));
            Assert.AreEqual(1, value);
        }

        [Test]
        public void AsReadOnly_TryGetValue_MissingKey()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.IsFalse(readOnly.TryGetValue("b", out var value));
            Assert.AreEqual(0, value);
        }

        [Test]
        public void AsReadOnly_ContainsKey_ExistingKey()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.IsTrue(readOnly.ContainsKey("a"));
        }

        [Test]
        public void AsReadOnly_ContainsKey_MissingKey()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1 };
            var readOnly = dictionary.AsReadOnly();
            Assert.IsFalse(readOnly.ContainsKey("b"));
        }

        [Test]
        public void AsReadOnly_EnumeratesCorrectly()
        {
            var dictionary = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
            var readOnly = dictionary.AsReadOnly();
            var keys = new List<string>();
            var values = new List<int>();
            foreach (var pair in readOnly)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
            Assert.That(keys, Is.EquivalentTo(new[] { "a", "b" }));
            Assert.That(values, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public void AsReadOnly_GenericAdd_Throws()
        {
            var readOnly = new Dictionary<string, int> { ["a"] = 1 }.AsReadOnly();
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)readOnly).Add("b", 2));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)readOnly)["a"] = 5);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)readOnly).Remove("a"));
            Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<string, int>>)readOnly).Clear());
            Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<string, int>>)readOnly).Add(new KeyValuePair<string, int>("b", 2)));
            Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<string, int>>)readOnly).Remove(new KeyValuePair<string, int>("a", 1)));
        }
    }
}

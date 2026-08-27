// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class EnumerableTest
    {
        [Test]
        public void ToReadOnlyList_NoSelector_MaterializesSequence()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList();
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }

        [Test]
        public void ToReadOnlyList_NoSelector_ReturnsReadOnlyWrapper()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList();
            Assert.IsTrue(((System.Collections.IList)result).IsReadOnly);
        }

        [Test]
        public void ToReadOnlyList_NoSelector_EmptySequence()
        {
            var source = new List<int>();
            var result = source.ToReadOnlyList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ToReadOnlyList_NoSelector_ThrowsOnWriteAttempt()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList();
            Assert.Throws<NotSupportedException>(() => ((IList<int>)result).Add(4));
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IListPath_AppliesTransform()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(10, result[0]);
            Assert.AreEqual(20, result[1]);
            Assert.AreEqual(30, result[2]);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IListPath_ReturnsReadOnlyCollection()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.IsInstanceOf<ReadOnlyCollection<int>>(result);
            Assert.IsTrue(((System.Collections.IList)result).IsReadOnly);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IListPath_EmptyList()
        {
            var source = new List<int>();
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IEnumerablePath_AppliesTransform()
        {
            IEnumerable<int> source = Enumerable.Range(1, 3).Where(x => true);
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(10, result[0]);
            Assert.AreEqual(20, result[1]);
            Assert.AreEqual(30, result[2]);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IEnumerablePath_ReturnsReadOnlyCollection()
        {
            IEnumerable<int> source = Enumerable.Range(1, 3).Where(x => true);
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.IsTrue(((System.Collections.IList)result).IsReadOnly);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IEnumerablePath_EmptySequence()
        {
            IEnumerable<int> source = Enumerable.Empty<int>().Where(x => true);
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IListPath_ThrowsOnWriteAttempt()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.Throws<NotSupportedException>(() => ((IList<int>)result).Add(40));
        }

        [Test]
        public void ToReadOnlyList_WithSelector_IEnumerablePath_ThrowsOnWriteAttempt()
        {
            IEnumerable<int> source = Enumerable.Range(1, 3).Where(x => true);
            var result = source.ToReadOnlyList(x => x * 10);
            Assert.Throws<NotSupportedException>(() => ((IList<int>)result).Add(40));
        }

        [Test]
        public void ToReadOnlyList_WithSelector_StringTransform()
        {
            var source = new List<int> { 1, 2, 3 };
            var result = source.ToReadOnlyList(x => $"item{x}");
            Assert.AreEqual("item1", result[0]);
            Assert.AreEqual("item2", result[1]);
            Assert.AreEqual("item3", result[2]);
        }
    }
}

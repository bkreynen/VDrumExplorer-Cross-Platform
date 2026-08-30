// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class SingleItemCollectionTest
    {
        [Test]
        public void Of_CreatesInstanceWithItem()
        {
            var collection = SingleItemCollection.Of("hello");
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual("hello", collection[0]);
        }

        [Test]
        public void Of_CreatesInstanceWithDifferentType()
        {
            var collection = SingleItemCollection.Of(42);
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(42, collection[0]);
        }

        [Test]
        public void Constructor_SetsItem()
        {
            var collection = new SingleItemCollection<string>("world");
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual("world", collection[0]);
        }

        [Test]
        public void Count_ReturnsOne()
        {
            var collection = SingleItemCollection.Of("item");
            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void Indexer_Zero_ReturnsItem()
        {
            var item = new object();
            var collection = SingleItemCollection.Of(item);
            Assert.AreSame(item, collection[0]);
        }

        [Test]
        public void Indexer_Negative_ThrowsIndexOutOfRangeException()
        {
            var collection = SingleItemCollection.Of("item");
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = collection[-1]; });
        }

        [Test]
        public void Indexer_One_ThrowsIndexOutOfRangeException()
        {
            var collection = SingleItemCollection.Of("item");
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = collection[1]; });
        }

        [Test]
        public void Indexer_LargeIndex_ThrowsIndexOutOfRangeException()
        {
            var collection = SingleItemCollection.Of("item");
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = collection[100]; });
        }

        [Test]
        public void GetEnumerator_YieldsSingleItem()
        {
            var collection = SingleItemCollection.Of("item");
            var items = collection.ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("item", items[0]);
        }

        [Test]
        public void GetEnumerator_NonGenericEnumerator_YieldsSingleItem()
        {
            var collection = SingleItemCollection.Of("item");
            var items = new List<object?>();
            IEnumerable enumerable = collection;
            foreach (var item in enumerable)
            {
                items.Add(item);
            }
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("item", items[0]);
        }

        [Test]
        public void Enumerator_MoveNext_FirstCallReturnsTrue()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
        }

        [Test]
        public void Enumerator_MoveNext_SecondCallReturnsFalse()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void Enumerator_MoveNext_ThirdCallReturnsFalse()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void Enumerator_Current_BeforeMoveNext_ThrowsInvalidOperationException()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => { var _ = enumerator.Current; });
        }

        [Test]
        public void Enumerator_Current_AfterFirstMoveNext_ReturnsItem()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual("item", enumerator.Current);
        }

        [Test]
        public void Enumerator_Current_AfterSecondMoveNext_ThrowsInvalidOperationException()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            Assert.Throws<InvalidOperationException>(() => { var _ = enumerator.Current; });
        }

        [Test]
        public void Enumerator_Reset_DoesNotThrow()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            Assert.DoesNotThrow(() => enumerator.Reset());
        }

        [Test]
        public void Enumerator_Reset_BeforeMoveNext_IsNoOp()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            Assert.DoesNotThrow(() => enumerator.Reset());
            // After Reset (no-op) the enumerator remains at initial state; first MoveNext still yields the item.
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("item", enumerator.Current);
        }

        [Test]
        public void Enumerator_Reset_IsNoOp_DoesNotAllowReEnumeration()
        {
            // The Reset method is intentionally a no-op in this implementation.
            // After enumeration is complete, Reset does not restore the enumerator
            // to its initial state. This is documented on SingleItemCollection.Enumerator.Reset.
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            Assert.AreEqual("item", enumerator.Current);
            enumerator.MoveNext();
            enumerator.Reset();
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test]
        public void Enumerator_Dispose_DoesNotThrow()
        {
            var collection = SingleItemCollection.Of("item");
            var enumerator = collection.GetEnumerator();
            Assert.DoesNotThrow(() => enumerator.Dispose());
        }

        [Test]
        public void Enumerator_NonGenericCurrent_BeforeMoveNext_Throws()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            IEnumerator nonGeneric = enumerator;
            Assert.Throws<InvalidOperationException>(() => { var _ = nonGeneric.Current; });
        }

        [Test]
        public void Enumerator_NonGenericCurrent_AfterMoveNext_ReturnsItem()
        {
            var collection = SingleItemCollection.Of("item");
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            IEnumerator nonGeneric = enumerator;
            Assert.AreEqual("item", nonGeneric.Current);
        }

        [Test]
        public void GetEnumerator_TwoEnumerators_Independent()
        {
            var collection = SingleItemCollection.Of("item");
            using var e1 = collection.GetEnumerator();
            using var e2 = collection.GetEnumerator();
            Assert.IsTrue(e1.MoveNext());
            Assert.IsTrue(e2.MoveNext());
            Assert.AreEqual("item", e1.Current);
            Assert.AreEqual("item", e2.Current);
            Assert.IsFalse(e1.MoveNext());
            Assert.IsFalse(e2.MoveNext());
            // Ensure e1 exhaustion did not affect e2's already-exhausted state and vice versa — independence proven by both having yielded.
        }

        [Test]
        public void GetEnumerator_TwoEnumerators_Interleaved()
        {
            var collection = SingleItemCollection.Of(42);
            using var e1 = collection.GetEnumerator();
            using var e2 = collection.GetEnumerator();
            // Interleave MoveNext calls to prove they don't share index.
            Assert.IsTrue(e1.MoveNext());
            Assert.AreEqual(42, e1.Current);
            Assert.IsTrue(e2.MoveNext());
            Assert.AreEqual(42, e2.Current);
            Assert.IsFalse(e1.MoveNext());
            Assert.IsFalse(e2.MoveNext());
        }
    }
}

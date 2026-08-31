// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class NotifyPropertyChangedHelperTest
    {
        private static PropertyChangedEventHandler CreateHandler() =>
            new PropertyChangedEventHandler((sender, e) => { });

        [Test]
        public void AddHandler_NullValue_ReturnsFalse()
        {
            PropertyChangedEventHandler? field = null;
            var result = NotifyPropertyChangedHelper.AddHandler(ref field, null);
            Assert.IsFalse(result);
            Assert.IsNull(field);
        }

        [Test]
        public void AddHandler_FirstHandler_ReturnsTrue()
        {
            PropertyChangedEventHandler? field = null;
            var handler = CreateHandler();
            var result = NotifyPropertyChangedHelper.AddHandler(ref field, handler);
            Assert.IsTrue(result);
            Assert.AreSame(handler, field);
        }

        [Test]
        public void AddHandler_SecondHandler_ReturnsFalse()
        {
            PropertyChangedEventHandler? field = CreateHandler();
            var handler = CreateHandler();
            var result = NotifyPropertyChangedHelper.AddHandler(ref field, handler);
            Assert.IsFalse(result);
            Assert.IsNotNull(field);
        }

        [Test]
        public void AddHandler_MultipleHandlers_AccumulateInField()
        {
            PropertyChangedEventHandler? field = null;
            var handler1 = CreateHandler();
            var handler2 = CreateHandler();
            var handler3 = CreateHandler();

            NotifyPropertyChangedHelper.AddHandler(ref field, handler1);
            NotifyPropertyChangedHelper.AddHandler(ref field, handler2);
            NotifyPropertyChangedHelper.AddHandler(ref field, handler3);

            Assert.IsNotNull(field);
            var invocationList = field!.GetInvocationList();
            Assert.AreEqual(3, invocationList.Length);
        }

        [Test]
        public void RemoveHandler_NullValue_ReturnsFalse()
        {
            PropertyChangedEventHandler? field = CreateHandler();
            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, null);
            Assert.IsFalse(result);
            Assert.IsNotNull(field);
        }

        [Test]
        public void RemoveHandler_NullField_ReturnsFalse()
        {
            PropertyChangedEventHandler? field = null;
            var handler = CreateHandler();
            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler);
            Assert.IsFalse(result);
            Assert.IsNull(field);
        }

        [Test]
        public void RemoveHandler_RemovingLastHandler_ReturnsTrue()
        {
            var handler = CreateHandler();
            PropertyChangedEventHandler? field = handler;
            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler);
            Assert.IsTrue(result);
            Assert.IsNull(field);
        }

        [Test]
        public void RemoveHandler_RemovingFirstOfTwoHandlers_ReturnsFalse()
        {
            var handler1 = CreateHandler();
            var handler2 = CreateHandler();
            PropertyChangedEventHandler? field = handler1 + handler2;
            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler1);
            Assert.IsFalse(result);
            Assert.AreSame(handler2, field);
        }

        [Test]
        public void RemoveHandler_RemovingSecondOfTwoHandlers_ReturnsFalse()
        {
            var handler1 = CreateHandler();
            var handler2 = CreateHandler();
            PropertyChangedEventHandler? field = handler1 + handler2;
            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler2);
            Assert.IsFalse(result);
            Assert.AreSame(handler1, field);
        }

        [Test]
        public void RemoveHandler_RemovingBothHandlers_ReturnsTrueForLast()
        {
            var handler1 = CreateHandler();
            var handler2 = CreateHandler();
            PropertyChangedEventHandler? field = handler1 + handler2;

            var result1 = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler1);
            Assert.IsFalse(result1);

            var result2 = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler2);
            Assert.IsTrue(result2);
            Assert.IsNull(field);
        }

        [Test]
        public void AddAndRemove_SymmetricOperations()
        {
            PropertyChangedEventHandler? field = null;
            var handler = CreateHandler();

            var addResult = NotifyPropertyChangedHelper.AddHandler(ref field, handler);
            Assert.IsTrue(addResult);

            var removeResult = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler);
            Assert.IsTrue(removeResult);
            Assert.IsNull(field);
        }

        private static PropertyChangedEventHandler CreateDistinctHandler(object token) =>
            (sender, e) => token.ToString();

        [Test]
        public void RemoveHandler_NotInList_ReturnsFalse_PreservesField()
        {
            // Use distinct handlers (different target closures) so equality is by reference, not by method.
            // CreateHandler() would create delegates that are == due to same static lambda; distinct tokens avoid that.
            var token1 = new object();
            var token2 = new object();
            var token3 = new object();
            var handler1 = CreateDistinctHandler(token1);
            var handler2 = CreateDistinctHandler(token2);
            var handler3 = CreateDistinctHandler(token3);
            PropertyChangedEventHandler? field = handler1 + handler2;
            var originalInvocationCount = field!.GetInvocationList().Length;

            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler3);

            Assert.IsFalse(result, "Removing a handler not in the invocation list should return false");
            Assert.IsNotNull(field, "Field should still be non-null when removal was a no-op (other handlers remain)");
            Assert.AreEqual(originalInvocationCount, field!.GetInvocationList().Length, "Invocation list length should be unchanged after no-op removal");
            Assert.IsTrue(field.GetInvocationList().Contains(handler1));
            Assert.IsTrue(field.GetInvocationList().Contains(handler2));
        }

        [Test]
        public void RemoveHandler_NotInList_SingleHandler_PreservesField()
        {
            var token1 = new object();
            var token2 = new object();
            var handler1 = CreateDistinctHandler(token1);
            var handler2 = CreateDistinctHandler(token2);
            PropertyChangedEventHandler? field = handler1;

            var result = NotifyPropertyChangedHelper.RemoveHandler(ref field, handler2);

            Assert.IsFalse(result);
            Assert.AreSame(handler1, field, "Original handler should be preserved when removing absent handler");
        }
    }
}

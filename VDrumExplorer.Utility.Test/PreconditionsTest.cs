// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class PreconditionsTest
    {
        [Test]
        public void CheckNotNull_NullValue_ThrowsArgumentNullException()
        {
            string? value = null;
            var ex = Assert.Throws<ArgumentNullException>(() => Preconditions.CheckNotNull(value!, "myParam"));
            Assert.AreEqual("myParam", ex!.ParamName);
        }

        [Test]
        public void CheckNotNull_NullParamName_ThrowsArgumentNullExceptionWithNullParamName()
        {
            string? value = null;
            string? paramName = null;
            // Preconditions.CheckNotNull translates null paramName via ArgumentNullException(null) — ParamName is null.
            // This documents the edge case where caller passes null as paramName; production guards still throw.
            var ex = Assert.Throws<ArgumentNullException>(() => Preconditions.CheckNotNull(value!, paramName!));
            Assert.IsNull(ex!.ParamName);
        }

        [Test]
        public void CheckNotNull_NonNullValue_ReturnsSameValue()
        {
            var value = new object();
            var result = Preconditions.CheckNotNull(value, "myParam");
            Assert.AreSame(value, result);
        }

        [Test]
        public void CheckNotNull_NonNullString_ReturnsSameValue()
        {
            var value = "hello";
            var result = Preconditions.CheckNotNull(value, "myParam");
            Assert.AreSame(value, result);
        }

        [Test]
        public void AssertNotNull_NullValue_ThrowsInvalidOperationException()
        {
            string? value = null;
            Assert.Throws<InvalidOperationException>(() => Preconditions.AssertNotNull(value));
        }

        [Test]
        public void AssertNotNull_NonNullValue_ReturnsSameValue()
        {
            var value = new object();
            var result = Preconditions.AssertNotNull(value);
            Assert.AreSame(value, result);
        }

        [Test]
        public void AssertNotNull_NonNullString_ReturnsSameValue()
        {
            var value = "hello";
            var result = Preconditions.AssertNotNull(value);
            Assert.AreSame(value, result);
        }
    }
}

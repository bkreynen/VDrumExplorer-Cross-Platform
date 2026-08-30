// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;

namespace VDrumExplorer.Model.Test
{
    public class ModuleOffsetTest
    {
        [Test]
        public void Zero_HasZeroValues()
        {
            var zero = ModuleOffset.Zero;
            Assert.AreEqual(0, zero.LogicalValue);
            Assert.AreEqual(0, zero.DisplayValue);
        }

        [Test]
        public void FromDisplayValue_Zero()
        {
            var offset = ModuleOffset.FromDisplayValue(0);
            Assert.AreEqual(0, offset.LogicalValue);
            Assert.AreEqual(0, offset.DisplayValue);
        }

        [Test]
        public void FromDisplayValue_Max()
        {
            var offset = ModuleOffset.FromDisplayValue(0x7f7f7f7f);
            Assert.AreEqual(0x0fffffff, offset.LogicalValue);
            Assert.AreEqual(0x7f7f7f7f, offset.DisplayValue);
        }

        [Test]
        public void FromDisplayValue_MidRange()
        {
            var offset = ModuleOffset.FromDisplayValue(0x01020304);
            // Logical value is computed by compressing the 7-bit chunks.
            int expectedLogical =
                ((0x01020304 & 0x7f_00_00_00) >> 3) |
                ((0x01020304 & 0x00_7f_00_00) >> 2) |
                ((0x01020304 & 0x00_00_7f_00) >> 1) |
                ((0x01020304 & 0x00_00_00_7f) >> 0);
            Assert.AreEqual(expectedLogical, offset.LogicalValue);
        }

        [Test]
        public void FromDisplayValue_WithHighBitInLowByte_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleOffset.FromDisplayValue(0x80));
        }

        [Test]
        public void FromDisplayValue_WithAllHighBitsSet_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleOffset.FromDisplayValue(unchecked((int)0x80808080)));
        }

        [Test]
        public void FromDisplayValue_WithHighBitInThirdByte_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleOffset.FromDisplayValue(0x800000));
        }

        [Test]
        public void OperatorPlusWithInt_AddsLogicalGap()
        {
            var offset = ModuleOffset.FromDisplayValue(0x10);
            var result = offset + 5;
            // Logical of 0x10 display is 0x10; adding 5 gives 0x15.
            Assert.AreEqual(0x15, result.LogicalValue);
        }

        [Test]
        public void OperatorPlusWithInt_ZeroGap()
        {
            var offset = ModuleOffset.FromDisplayValue(0x10);
            var result = offset + 0;
            Assert.AreEqual(offset.LogicalValue, result.LogicalValue);
        }

        [Test]
        public void OperatorPlusWithInt_NegativeGap()
        {
            var offset = ModuleOffset.FromDisplayValue(0x10);
            var result = offset + (-5);
            Assert.AreEqual(0x0b, result.LogicalValue);
        }

        [Test]
        public void ToString_ReturnsHexFormat()
        {
            var offset = ModuleOffset.Zero;
            Assert.AreEqual("00000000", offset.ToString());

            var offset2 = ModuleOffset.FromDisplayValue(0x12345678);
            Assert.AreEqual("12345678", offset2.ToString());
        }

        [Test]
        public void Equals_Typed_SameValue_ReturnsTrue()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1.Equals(o2));
        }

        [Test]
        public void Equals_Typed_DifferentValue_ReturnsFalse()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsFalse(o1.Equals(o2));
        }

        [Test]
        public void GetHashCode_ConsistentWithEquals()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.AreEqual(o1.GetHashCode(), o2.GetHashCode());
        }

        [Test]
        public void GetHashCode_BasedOnDisplayValue()
        {
            var offset = ModuleOffset.FromDisplayValue(0x123);
            Assert.AreEqual(0x123, offset.GetHashCode());
        }

        [Test]
        public void Equals_Object_SameValue_ReturnsTrue()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1.Equals((object)o2));
        }

        [Test]
        public void Equals_Object_Null_ReturnsFalse()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1.Equals((object)null));
        }

        [Test]
        public void Equals_Object_WrongType_ReturnsFalse()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1.Equals("not an offset"));
        }

        [Test]
        public void Equals_Object_ModuleAddress_ReturnsFalse()
        {
            var offset = ModuleOffset.FromDisplayValue(0x10);
            var addr = ModuleAddress.FromLogicalValue(0x10);
            Assert.IsFalse(offset.Equals((object)addr));
            Assert.IsFalse(addr.Equals((object)offset));
        }

        [Test]
        public void EqualityOperator_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1 == o2);
        }

        [Test]
        public void EqualityOperator_NotEqual()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsFalse(o1 == o2);
        }

        [Test]
        public void InequalityOperator_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1 != o2);
        }

        [Test]
        public void InequalityOperator_NotEqual()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsTrue(o1 != o2);
        }

        [Test]
        public void GreaterThanOrEqual_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1 >= o2);
        }

        [Test]
        public void GreaterThanOrEqual_Greater()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x20);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1 >= o2);
        }

        [Test]
        public void GreaterThanOrEqual_Less()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsFalse(o1 >= o2);
        }

        [Test]
        public void LessThanOrEqual_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1 <= o2);
        }

        [Test]
        public void LessThanOrEqual_Less()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsTrue(o1 <= o2);
        }

        [Test]
        public void LessThanOrEqual_Greater()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x20);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1 <= o2);
        }

        [Test]
        public void GreaterThan_Greater()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x20);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsTrue(o1 > o2);
        }

        [Test]
        public void GreaterThan_Less()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsFalse(o1 > o2);
        }

        [Test]
        public void GreaterThan_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1 > o2);
        }

        [Test]
        public void LessThan_Less()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x20);
            Assert.IsTrue(o1 < o2);
        }

        [Test]
        public void LessThan_Greater()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x20);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1 < o2);
        }

        [Test]
        public void LessThan_Equal()
        {
            var o1 = ModuleOffset.FromDisplayValue(0x10);
            var o2 = ModuleOffset.FromDisplayValue(0x10);
            Assert.IsFalse(o1 < o2);
        }
    }
}

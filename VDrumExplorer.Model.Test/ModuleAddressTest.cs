// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;

namespace VDrumExplorer.Model.Test
{
    public class ModuleAddressTest
    {
        // The maximum valid logical value is (1 << 28) - 1, as FromLogicalValue rejects values >= 1 << 28.
        private const int MaxLogicalValue = 0x0fffffff;

        [Test]
        public void FromLogicalValue_Zero()
        {
            var address = ModuleAddress.FromLogicalValue(0);
            Assert.AreEqual(0, address.LogicalValue);
            Assert.AreEqual(0, address.DisplayValue);
        }

        [Test]
        public void FromLogicalValue_Max()
        {
            var address = ModuleAddress.FromLogicalValue(MaxLogicalValue);
            Assert.AreEqual(MaxLogicalValue, address.LogicalValue);
            // Each 7-bit chunk of the logical value maps to a 7-bit chunk in the display value,
            // but shifted into the corresponding byte position.
            Assert.AreEqual(0x7f7f7f7f, address.DisplayValue);
        }

        [Test]
        public void FromLogicalValue_MidRange()
        {
            // A logical value with bits in each 7-bit chunk.
            int logical = 0x01_02_03_04;
            var address = ModuleAddress.FromLogicalValue(logical);
            Assert.AreEqual(logical, address.LogicalValue);
            // Display value shifts each 7-bit chunk into a byte.
            int expectedDisplay =
                ((logical & 0b1111111) << 0) |
                ((logical & 0b1111111_0000000) << 1) |
                ((logical & 0b1111111_0000000_0000000) << 2) |
                ((logical & 0b1111111_0000000_0000000_0000000) << 3);
            Assert.AreEqual(expectedDisplay, address.DisplayValue);
        }

        [Test]
        public void FromLogicalValue_Negative_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleAddress.FromLogicalValue(-1));
        }

        [Test]
        public void FromLogicalValue_TooLarge_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleAddress.FromLogicalValue(1 << 28));
        }

        [Test]
        public void FromDisplayValue_Zero()
        {
            var address = ModuleAddress.FromDisplayValue(0);
            Assert.AreEqual(0, address.LogicalValue);
            Assert.AreEqual(0, address.DisplayValue);
        }

        [Test]
        public void FromDisplayValue_Max()
        {
            var address = ModuleAddress.FromDisplayValue(0x7f7f7f7f);
            Assert.AreEqual(MaxLogicalValue, address.LogicalValue);
            Assert.AreEqual(0x7f7f7f7f, address.DisplayValue);
        }

        [Test]
        public void FromDisplayValue_WithHighBitInLowByte_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleAddress.FromDisplayValue(0x80));
        }

        [Test]
        public void FromDisplayValue_WithAllHighBitsSet_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleAddress.FromDisplayValue(unchecked((int)0x80808080)));
        }

        [Test]
        public void FromDisplayValue_WithHighBitInSecondByte_Throws()
        {
            Assert.Throws<ArgumentException>(() => ModuleAddress.FromDisplayValue(0x8000));
        }

        [Test]
        public void RoundTrip_FromLogicalValue_ToDisplayValue_BackToLogicalValue()
        {
            // Pick several logical values across the range, including values that exercise
            // each 7-bit chunk.
            int[] values = { 0, 1, 0x7f, 0x80, 0x3fff, 0x4000, 0x1fffff, 0x200000, MaxLogicalValue };
            foreach (var logical in values)
            {
                var address = ModuleAddress.FromLogicalValue(logical);
                var roundTripped = ModuleAddress.FromDisplayValue(address.DisplayValue);
                Assert.AreEqual(logical, roundTripped.LogicalValue, $"Logical value {logical:x8} did not round-trip");
            }
        }

        [Test]
        public void PlusLogicalOffset_Positive()
        {
            var address = ModuleAddress.FromLogicalValue(0x100);
            var offset = address.PlusLogicalOffset(5);
            Assert.AreEqual(0x105, offset.LogicalValue);
        }

        [Test]
        public void PlusLogicalOffset_Negative()
        {
            var address = ModuleAddress.FromLogicalValue(0x100);
            var offset = address.PlusLogicalOffset(-5);
            Assert.AreEqual(0xfb, offset.LogicalValue);
        }

        [Test]
        public void PlusLogicalOffset_ToZero()
        {
            var address = ModuleAddress.FromLogicalValue(0x100);
            var offset = address.PlusLogicalOffset(-0x100);
            Assert.AreEqual(0, offset.LogicalValue);
        }

        [Test]
        public void OperatorPlus_SimpleOffset()
        {
            var address = ModuleAddress.FromLogicalValue(0);
            var offset = ModuleOffset.FromDisplayValue(0x10);
            var result = address + offset;
            Assert.AreEqual(0x10, result.LogicalValue);
        }

        [Test]
        public void OperatorPlus_CarryCompensation_LowByte()
        {
            // Address 0x7f (display) + offset 1 (display) should produce logical 0x80
            // because the carry compensation adds 0x80 when the low byte overflows,
            // turning display 0x80 into display 0x100, which maps to logical 0x80.
            var address = ModuleAddress.FromDisplayValue(0x7f);
            var offset = ModuleOffset.FromDisplayValue(0x01);
            var result = address + offset;
            Assert.AreEqual(0x80, result.LogicalValue);
        }

        [Test]
        public void OperatorPlus_CarryCompensation_SecondByte()
        {
            // Address 0x7f00 (display) + offset 0x0100 (display) should trigger carry in the second byte.
            // 0x7f00 + 0x0100 = 0x8000 before compensation, which has the 0x80 bit set in the second byte,
            // so the operator adds 0x8000, yielding display 0x10000.
            var address = ModuleAddress.FromDisplayValue(0x7f00);
            var offset = ModuleOffset.FromDisplayValue(0x0100);
            var result = address + offset;
            Assert.AreEqual(0x10000, result.DisplayValue);
            Assert.AreEqual(0x4000, result.LogicalValue);
            // Cross-check via FromDisplayValue
            Assert.AreEqual(ModuleAddress.FromDisplayValue(0x10000).LogicalValue, result.LogicalValue);
            Assert.AreEqual(ModuleAddress.FromDisplayValue(0x10000).DisplayValue, result.DisplayValue);
        }

        [Test]
        public void ToString_ReturnsHexFormat()
        {
            var address = ModuleAddress.FromLogicalValue(0);
            Assert.AreEqual("00000000", address.ToString());

            var address2 = ModuleAddress.FromDisplayValue(0x12345678);
            Assert.AreEqual("12345678", address2.ToString());
        }

        [Test]
        public void Equals_SameValue_ReturnsTrue()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            var a2 = ModuleAddress.FromLogicalValue(0x100);
            Assert.IsTrue(a1.Equals(a2));
            Assert.IsTrue(a1.Equals((object)a2));
        }

        [Test]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            var a2 = ModuleAddress.FromLogicalValue(0x200);
            Assert.IsFalse(a1.Equals(a2));
            Assert.IsFalse(a1.Equals((object)a2));
        }

        [Test]
        public void Equals_NullObject_ReturnsFalse()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            Assert.IsFalse(a1.Equals((object)null));
        }

        [Test]
        public void Equals_WrongType_ReturnsFalse()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            Assert.IsFalse(a1.Equals("not an address"));
        }

        [Test]
        public void GetHashCode_ConsistentWithEquals()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            var a2 = ModuleAddress.FromLogicalValue(0x100);
            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
        }

        [Test]
        public void GetHashCode_BasedOnLogicalValue()
        {
            var address = ModuleAddress.FromLogicalValue(0x123);
            Assert.AreEqual(0x123, address.GetHashCode());
        }

        [Test]
        public void CompareTo_Typed_LessThan()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            var a2 = ModuleAddress.FromLogicalValue(0x200);
            Assert.Less(a1.CompareTo(a2), 0);
        }

        [Test]
        public void CompareTo_Typed_GreaterThan()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x200);
            var a2 = ModuleAddress.FromLogicalValue(0x100);
            Assert.Greater(a1.CompareTo(a2), 0);
        }

        [Test]
        public void CompareTo_Typed_Equal()
        {
            var a1 = ModuleAddress.FromLogicalValue(0x100);
            var a2 = ModuleAddress.FromLogicalValue(0x100);
            Assert.AreEqual(0, a1.CompareTo(a2));
        }

        [Test]
        public void CompareTo_Untyped_Equal()
        {
            IComparable a1 = ModuleAddress.FromLogicalValue(0x100);
            object a2 = ModuleAddress.FromLogicalValue(0x100);
            Assert.AreEqual(0, a1.CompareTo(a2));
        }

        [Test]
        public void CompareTo_Untyped_LessThan()
        {
            IComparable a1 = ModuleAddress.FromLogicalValue(0x100);
            object a2 = ModuleAddress.FromLogicalValue(0x200);
            Assert.Less(a1.CompareTo(a2), 0);
        }

        [Test]
        public void CompareTo_Null_Throws()
        {
            IComparable a1 = ModuleAddress.FromLogicalValue(0x100);
            Assert.Throws<ArgumentException>(() => a1.CompareTo(null));
        }

        [Test]
        public void CompareTo_WrongType_Throws()
        {
            IComparable a1 = ModuleAddress.FromLogicalValue(0x100);
            Assert.Throws<ArgumentException>(() => a1.CompareTo("not an address"));
        }
    }
}

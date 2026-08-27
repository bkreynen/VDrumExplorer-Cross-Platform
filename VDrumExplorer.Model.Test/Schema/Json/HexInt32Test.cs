// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;
using VDrumExplorer.Model.Schema.Json;

namespace VDrumExplorer.Model.Test.Schema.Json;

internal class HexInt32Test
{
    // HexInt32 is internal, but accessible via InternalsVisibleTo.

    // --- Parse: valid cases ---

    [Test]
    [TestCase("0x0", 0)]
    [TestCase("0x1", 1)]
    [TestCase("0x10", 16)]
    [TestCase("0x100", 256)]
    [TestCase("0x7f", 127)]
    [TestCase("0x7FFFFFFF", int.MaxValue)]
    [TestCase("-0x10", -16)]
    [TestCase("-0x1", -1)]
    [TestCase("-0x7FFFFFFF", -int.MaxValue)]
    public void Parse_ValidHex_ReturnsValue(string text, int expectedValue)
    {
        var hex = HexInt32.Parse(text);
        Assert.AreEqual(expectedValue, hex.Value);
    }

    [Test]
    public void Parse_WithUnderscoreSeparator_ReturnsSameValue()
    {
        var hex = HexInt32.Parse("0x1_00");
        Assert.AreEqual(256, hex.Value);
    }

    [Test]
    public void Parse_WithMultipleUnderscores_ReturnsSameValue()
    {
        var hex = HexInt32.Parse("0x1_0_0");
        Assert.AreEqual(256, hex.Value);
    }

    [Test]
    public void Parse_UnderscoreAndNoUnderscore_ProduceEqualValues()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x1_00");
        Assert.AreEqual(a.Value, b.Value);
        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void Parse_PreservesOriginalText()
    {
        var hex = HexInt32.Parse("0x1_00");
        Assert.AreEqual("0x1_00", hex.Text);
    }

    [Test]
    public void Parse_NegativeWithUnderscore()
    {
        var hex = HexInt32.Parse("-0x1_00");
        Assert.AreEqual(-256, hex.Value);
        Assert.AreEqual("-0x1_00", hex.Text);
    }

    // --- Parse: invalid cases ---

    [Test]
    public void Parse_NoPrefix_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => HexInt32.Parse("100"));
    }

    [Test]
    public void Parse_EmptyString_Throws()
    {
        // Empty string fails the "0x" prefix check, so FormatException.
        Assert.Throws<FormatException>(() => HexInt32.Parse(""));
    }

    [Test]
    public void Parse_Overflow_ThrowsFormatException()
    {
        // 0x80000000 overflows int (becomes negative, which is rejected).
        Assert.Throws<FormatException>(() => HexInt32.Parse("0x80000000"));
    }

    [Test]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => HexInt32.Parse(null!));
    }

    [Test]
    public void Parse_Just0xPrefix_ThrowsFormatException()
    {
        // "0x" with nothing after it - int.TryParse on empty string fails.
        Assert.Throws<FormatException>(() => HexInt32.Parse("0x"));
    }

    // --- Text property ---

    [Test]
    public void Text_ReturnsOriginalText()
    {
        var hex = HexInt32.Parse("0x7f");
        Assert.AreEqual("0x7f", hex.Text);
    }

    // --- ToString ---

    [Test]
    public void ToString_ReturnsOriginalText()
    {
        var hex = HexInt32.Parse("0x1_00");
        Assert.AreEqual("0x1_00", hex.ToString());
    }

    // --- Equals(HexInt32?) ---

    [Test]
    public void Equals_Typed_SameValue_ReturnsTrue()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x100");
        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void Equals_Typed_DifferentValue_ReturnsFalse()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x200");
        Assert.IsFalse(a.Equals(b));
    }

    [Test]
    public void Equals_Typed_DifferentTextSameValue_ReturnsTrue()
    {
        // Equality is numeric, not textual.
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x1_00");
        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void Equals_Typed_Null_ReturnsFalse()
    {
        var a = HexInt32.Parse("0x100");
        Assert.IsFalse(a.Equals(null));
    }

    // --- Equals(object?) ---

    [Test]
    public void Equals_Object_SameValue_ReturnsTrue()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x100");
        Assert.IsTrue(a.Equals((object)b));
    }

    [Test]
    public void Equals_Object_DifferentValue_ReturnsFalse()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x200");
        Assert.IsFalse(a.Equals((object)b));
    }

    [Test]
    public void Equals_Object_Null_ReturnsFalse()
    {
        var a = HexInt32.Parse("0x100");
        Assert.IsFalse(a.Equals((object?)null));
    }

    [Test]
    public void Equals_Object_WrongType_ReturnsFalse()
    {
        var a = HexInt32.Parse("0x100");
        Assert.IsFalse(a.Equals("not a HexInt32"));
    }

    // --- GetHashCode ---

    [Test]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x1_00");
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void GetHashCode_BasedOnValue()
    {
        var hex = HexInt32.Parse("0x100");
        Assert.AreEqual(256.GetHashCode(), hex.GetHashCode());
    }

    [Test]
    public void GetHashCode_DifferentValues_ReturnDifferentHashes()
    {
        var a = HexInt32.Parse("0x100");
        var b = HexInt32.Parse("0x200");
        Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
    }
}

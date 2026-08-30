// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;
using VDrumExplorer.Model.Schema.Fields;

namespace VDrumExplorer.Model.Test.Schema.Fields;

internal class NumericCodecTest
{
    // The codecs are internal, but accessible via InternalsVisibleTo.

    [Test]
    public void Range8_Properties()
    {
        var codec = NumericCodec.Range8;
        Assert.AreEqual(1, codec.Size);
        Assert.AreEqual(0, codec.Min);
        Assert.AreEqual(127, codec.Max);
    }

    [Test]
    public void Range16_Properties()
    {
        var codec = NumericCodec.Range16;
        Assert.AreEqual(2, codec.Size);
        Assert.AreEqual(-128, codec.Min);
        Assert.AreEqual(127, codec.Max);
    }

    [Test]
    public void URange16_Properties()
    {
        var codec = NumericCodec.URange16;
        Assert.AreEqual(2, codec.Size);
        Assert.AreEqual(0, codec.Min);
        Assert.AreEqual(255, codec.Max);
    }

    [Test]
    public void Full24_Properties()
    {
        var codec = NumericCodec.Full24;
        Assert.AreEqual(3, codec.Size);
        Assert.AreEqual(0, codec.Min);
        Assert.AreEqual((1 << 21) - 1, codec.Max);
    }

    [Test]
    public void Range32_Properties()
    {
        var codec = NumericCodec.Range32;
        Assert.AreEqual(4, codec.Size);
        Assert.AreEqual(short.MinValue, codec.Min);
        Assert.AreEqual(short.MaxValue, codec.Max);
    }

    [Test]
    public void Fixme32_Properties()
    {
        var codec = NumericCodec.Fixme32;
        Assert.AreEqual(4, codec.Size);
        Assert.AreEqual(-16384, codec.Min);
        Assert.AreEqual(16383, codec.Max);
    }

    [Test]
    [TestCaseSource(nameof(Range8RoundTripCases))]
    public void Range8_RoundTrip(int value) => AssertRoundTrip(NumericCodec.Range8, value);

    [Test]
    [TestCaseSource(nameof(Range16RoundTripCases))]
    public void Range16_RoundTrip(int value) => AssertRoundTrip(NumericCodec.Range16, value);

    [Test]
    [TestCaseSource(nameof(URange16RoundTripCases))]
    public void URange16_RoundTrip(int value) => AssertRoundTrip(NumericCodec.URange16, value);

    [Test]
    [TestCaseSource(nameof(Full24RoundTripCases))]
    public void Full24_RoundTrip(int value) => AssertRoundTrip(NumericCodec.Full24, value);

    [Test]
    [TestCaseSource(nameof(Range32RoundTripCases))]
    public void Range32_RoundTrip(int value) => AssertRoundTrip(NumericCodec.Range32, value);

    [Test]
    [TestCaseSource(nameof(Fixme32RoundTripCases))]
    public void Fixme32_RoundTrip(int value) => AssertRoundTrip(NumericCodec.Fixme32, value);

    // --- Test case sources ---

    private static int[] Range8RoundTripCases() => new[] { 0, 1, 64, 127 };

    private static int[] Range16RoundTripCases() => new[] { -128, -1, 0, 1, 64, 127 };

    private static int[] URange16RoundTripCases() => new[] { 0, 1, 128, 200, 255 };

    private static int[] Full24RoundTripCases() => new[] { 0, 1, 0x7f, 0x80, 0x3fff, 0x4000, (1 << 21) - 1 };

    private static int[] Range32RoundTripCases() => new[] { short.MinValue, -1, 0, 1, 100, short.MaxValue };

    private static int[] Fixme32RoundTripCases() => new[] { -16384, -1, 0, 1, 100, 16383 };

    // --- Helper ---

    /// <summary>
    /// Writes the value using the codec, then reads it back and asserts equality.
    /// The buffer is larger than any codec size, so we always have room.
    /// </summary>
    private static void AssertRoundTrip(NumericCodec codec, int value)
    {
        var buffer = new byte[codec.Size];
        var span = buffer.AsSpan();
        codec.WriteInt32(span, value);
        var result = codec.ReadInt32(span);
        Assert.AreEqual(value, result, $"Round-trip failed for value {value} with codec (size={codec.Size}, min={codec.Min}, max={codec.Max})");
    }

    // --- Byte-level verification tests ---

    [Test]
    public void Range8_WritesSingleByte()
    {
        var buffer = new byte[1];
        NumericCodec.Range8.WriteInt32(buffer, 42);
        Assert.AreEqual(new byte[] { 42 }, buffer);
    }

    [Test]
    public void Range16_WritesNibbles()
    {
        // Value 42 = 0x2A. High nibble = 0x02, low nibble = 0x0A.
        var buffer = new byte[2];
        NumericCodec.Range16.WriteInt32(buffer, 42);
        Assert.AreEqual(new byte[] { 0x02, 0x0A }, buffer);
    }

    [Test]
    public void URange16_WritesNibbles()
    {
        // Value 200 = 0xC8. High nibble = 0x0C, low nibble = 0x08.
        var buffer = new byte[2];
        NumericCodec.URange16.WriteInt32(buffer, 200);
        Assert.AreEqual(new byte[] { 0x0C, 0x08 }, buffer);
    }

    [Test]
    public void Full24_Writes7BitChunks()
    {
        // Value 0x123456 is too large (max is 0x1FFFFF). Use 0x12345.
        // 0x12345 = 0b0_0001_0010_0011_0100_0101
        // Split into 7-bit chunks (from MSB): 0b0000100, 0b1000110, 0b1000101
        // = 0x04, 0x46, 0x45
        var buffer = new byte[3];
        NumericCodec.Full24.WriteInt32(buffer, 0x12345);
        Assert.AreEqual(new byte[] { 0x04, 0x46, 0x45 }, buffer);
    }

    [Test]
    public void Range32_WritesNibbles()
    {
        // Value 0x1234. Nibbles: 0x1, 0x2, 0x3, 0x4
        var buffer = new byte[4];
        NumericCodec.Range32.WriteInt32(buffer, 0x1234);
        Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, buffer);
    }

    [Test]
    public void Range16_NegativeValue_RoundTrips()
    {
        // -1 in sbyte is 0xFF. As nibbles: high=0x0F, low=0x0F.
        var buffer = new byte[2];
        NumericCodec.Range16.WriteInt32(buffer, -1);
        Assert.AreEqual(new byte[] { 0x0F, 0x0F }, buffer);
        Assert.AreEqual(-1, NumericCodec.Range16.ReadInt32(buffer));
    }

    [Test]
    public void Range32_NegativeValue_RoundTrips()
    {
        // short.MinValue = -32768 = 0x8000. Nibbles: 0x8, 0x0, 0x0, 0x0
        var buffer = new byte[4];
        NumericCodec.Range32.WriteInt32(buffer, short.MinValue);
        Assert.AreEqual(new byte[] { 0x08, 0x00, 0x00, 0x00 }, buffer);
        Assert.AreEqual(short.MinValue, NumericCodec.Range32.ReadInt32(buffer));
    }

    [Test]
    public void Fixme32_WritesWithFlagBit()
    {
        var buf = new byte[4];
        NumericCodec.Fixme32.WriteInt32(buf, 16383);
        Assert.AreEqual(new byte[] { 0x03, 0x0F, 0x0F, 0x0F }, buf);
    }

    [Test]
    public void Fixme32_HighNibbleRead()
    {
        var buf = new byte[] { 0x08, 0x00, 0x00, 0x00 };
        Assert.AreEqual(-32768, NumericCodec.Fixme32.ReadInt32(buf));
    }

    [Test]
    public void Fixme32_ReadEqualsRange32_ForSameBytes()
    {
        var bytes = new byte[] { 0x02, 0x03, 0x04, 0x05 };
        Assert.AreEqual(NumericCodec.Range32.ReadInt32(bytes), NumericCodec.Fixme32.ReadInt32(bytes));
    }
}

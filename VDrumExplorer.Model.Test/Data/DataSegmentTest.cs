// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Schema.Fields;

namespace VDrumExplorer.Model.Test.Data;

internal class DataSegmentTest
{
    // DataSegment's Clone and WithAddress methods are internal, but accessible via InternalsVisibleTo.
    // ReadInt32/WriteInt32/ReadBytes/WriteBytes are also internal.

    private static readonly ModuleAddress SampleAddress = ModuleAddress.FromLogicalValue(0x100);

    private static DataSegment CreateSegment(int size) =>
        new DataSegment(SampleAddress, new byte[size]);

    private static DataSegment CreateSegmentWithData(params byte[] data) =>
        new DataSegment(SampleAddress, data);

    [Test]
    public void Constructor_SetsAddress()
    {
        var segment = CreateSegment(10);
        Assert.AreEqual(SampleAddress, segment.Address);
    }

    [Test]
    public void Size_ReturnsDataLength()
    {
        var segment = CreateSegment(16);
        Assert.AreEqual(16, segment.Size);
    }

    [Test]
    public void CopyData_ReturnsIndependentCopy()
    {
        var original = CreateSegmentWithData(1, 2, 3, 4);
        var copy = original.CopyData();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, copy);

        // Modify the original's data via WriteBytes, copy should be unchanged.
        original.WriteBytes(ModuleOffset.Zero, new byte[] { 9, 9, 9, 9 }.AsSpan());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, copy);
    }

    [Test]
    public void CopyData_ReturnsNewArrayEachTime()
    {
        var segment = CreateSegmentWithData(1, 2, 3);
        var copy1 = segment.CopyData();
        var copy2 = segment.CopyData();
        Assert.AreNotSame(copy1, copy2);
        Assert.AreNotSame(copy1, segment.CopyData());
    }

    [Test]
    public void Clone_ReturnsIndependentCopyWithSameAddress()
    {
        var original = CreateSegmentWithData(1, 2, 3, 4);
        var clone = original.Clone();
        Assert.AreEqual(original.Address, clone.Address);
        Assert.AreEqual(original.Size, clone.Size);
        CollectionAssert.AreEqual(original.CopyData(), clone.CopyData());

        // Modify the original, clone should be unchanged.
        original.WriteBytes(ModuleOffset.Zero, new byte[] { 9, 9, 9, 9 }.AsSpan());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, clone.CopyData());
    }

    [Test]
    public void WithAddress_ReturnsNewSegmentWithDifferentAddressButSameData()
    {
        var original = CreateSegmentWithData(1, 2, 3, 4);
        var newAddress = ModuleAddress.FromLogicalValue(0x200);
        var relocated = original.WithAddress(newAddress);

        Assert.AreEqual(newAddress, relocated.Address);
        Assert.AreNotEqual(original.Address, relocated.Address);
        Assert.AreEqual(original.Size, relocated.Size);

        // The data should be the same initially (shared array).
        CollectionAssert.AreEqual(original.CopyData(), relocated.CopyData());

        // WithAddress shares the underlying data array, so modifying the original
        // affects the relocated segment as well.
        original.WriteBytes(ModuleOffset.Zero, new byte[] { 9, 9, 9, 9 }.AsSpan());
        CollectionAssert.AreEqual(new byte[] { 9, 9, 9, 9 }, relocated.CopyData());
    }

    // --- ReadInt32 / WriteInt32 round-trip ---

    [Test]
    [TestCaseSource(nameof(CodecRoundTripCases))]
    public void ReadWriteInt32_RoundTrip(NumericCodec codec, int value)
    {
        var segment = CreateSegment(codec.Size + 2); // extra room so offset 2 is valid
        var offset = ModuleOffset.FromDisplayValue(2); // logical value 2
        segment.WriteInt32(offset, codec, value);
        var result = segment.ReadInt32(offset, codec);
        Assert.AreEqual(value, result);
    }

    private static IEnumerable<TestCaseData> CodecRoundTripCases()
    {
        yield return new TestCaseData(NumericCodec.Range8, 0);
        yield return new TestCaseData(NumericCodec.Range8, 127);
        yield return new TestCaseData(NumericCodec.Range16, -128);
        yield return new TestCaseData(NumericCodec.Range16, 127);
        yield return new TestCaseData(NumericCodec.URange16, 0);
        yield return new TestCaseData(NumericCodec.URange16, 255);
        yield return new TestCaseData(NumericCodec.Full24, 0);
        yield return new TestCaseData(NumericCodec.Full24, (1 << 21) - 1);
        yield return new TestCaseData(NumericCodec.Range32, short.MinValue);
        yield return new TestCaseData(NumericCodec.Range32, short.MaxValue);
        yield return new TestCaseData(NumericCodec.Fixme32, -16384);
        yield return new TestCaseData(NumericCodec.Fixme32, 16383);
    }

    // --- ReadBytes / WriteBytes round-trip ---

    [Test]
    public void ReadWriteBytes_RoundTrip()
    {
        var segment = CreateSegment(16);
        var sourceData = new byte[] { 10, 20, 30, 40, 50 };
        var offset = ModuleOffset.FromDisplayValue(5); // logical value 5
        segment.WriteBytes(offset, sourceData.AsSpan());

        var destination = new byte[5];
        segment.ReadBytes(offset, destination);
        CollectionAssert.AreEqual(sourceData, destination);
    }

    [Test]
    public void WriteBytes_OverwritesExistingData()
    {
        var segment = CreateSegmentWithData(0, 0, 0, 0, 0, 0, 0, 0);
        segment.WriteBytes(ModuleOffset.Zero, new byte[] { 1, 2, 3 }.AsSpan());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 0, 0, 0, 0, 0 }, segment.CopyData());
    }

    [Test]
    public void ReadBytes_DoesNotModifySegment()
    {
        var segment = CreateSegmentWithData(1, 2, 3, 4, 5);
        var dest = new byte[3];
        segment.ReadBytes(ModuleOffset.Zero, dest);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, segment.CopyData());
    }

    // --- ValidateRange error cases ---

    [Test]
    public void ReadBytes_OffsetBeyondSegment_Throws()
    {
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(5); // logical 5, beyond length 4
        var dest = new byte[2];
        Assert.Throws<ArgumentException>(() => segment.ReadBytes(offset, dest));
    }

    [Test]
    public void ReadBytes_OffsetPlusLengthExceedsSegment_Throws()
    {
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(2); // logical 2
        var dest = new byte[4]; // 2 + 4 = 6 > 4
        Assert.Throws<ArgumentException>(() => segment.ReadBytes(offset, dest));
    }

    [Test]
    public void WriteBytes_OffsetBeyondSegment_Throws()
    {
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(5); // logical 5, beyond length 4
        Assert.Throws<ArgumentException>(() => segment.WriteBytes(offset, new byte[2].AsSpan()));
    }

    [Test]
    public void WriteBytes_OffsetPlusLengthExceedsSegment_Throws()
    {
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(2); // logical 2
        Assert.Throws<ArgumentException>(() => segment.WriteBytes(offset, new byte[4].AsSpan()));
    }

    [Test]
    public void ReadInt32_OffsetBeyondSegment_Throws()
    {
        var segment = CreateSegment(2);
        var offset = ModuleOffset.FromDisplayValue(2); // logical 2, beyond length 2
        Assert.Throws<ArgumentException>(() => segment.ReadInt32(offset, NumericCodec.Range8));
    }

    [Test]
    public void ReadInt32_OffsetPlusSizeExceedsSegment_Throws()
    {
        var segment = CreateSegment(2);
        var offset = ModuleOffset.FromDisplayValue(1); // logical 1
        // Range16 needs 2 bytes, but only 1 byte is available (1 + 2 = 3 > 2)
        Assert.Throws<ArgumentException>(() => segment.ReadInt32(offset, NumericCodec.Range16));
    }

    [Test]
    public void WriteInt32_OffsetBeyondSegment_Throws()
    {
        var segment = CreateSegment(2);
        var offset = ModuleOffset.FromDisplayValue(2); // logical 2, beyond length 2
        Assert.Throws<ArgumentException>(() => segment.WriteInt32(offset, NumericCodec.Range8, 0));
    }

    [Test]
    public void WriteInt32_OffsetPlusSizeExceedsSegment_Throws()
    {
        var segment = CreateSegment(2);
        var offset = ModuleOffset.FromDisplayValue(1); // logical 1
        Assert.Throws<ArgumentException>(() => segment.WriteInt32(offset, NumericCodec.Range16, 0));
    }

    [Test]
    public void ReadBytes_AtExactEndOffset_Throws()
    {
        // Offset equal to length is invalid (start >= data.Length)
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(4); // logical 4 == length
        var dest = new byte[1];
        Assert.Throws<ArgumentException>(() => segment.ReadBytes(offset, dest));
    }

    [Test]
    public void ReadBytes_WithZeroLength_AtValidOffset_DoesNotThrow()
    {
        var segment = CreateSegment(4);
        var offset = ModuleOffset.FromDisplayValue(2); // logical 2
        var dest = new byte[0];
        Assert.DoesNotThrow(() => segment.ReadBytes(offset, dest));
    }
}

// Copyright 2023 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;
using VDrumExplorer.Model.Data;

namespace VDrumExplorer.Model.Test.Data;

internal class ModuleDataSnapshotTest
{
    private Module module = null!;
    private ModuleDataSnapshot snapshot = null!;

    [SetUp]
    public void SetUp()
    {
        module = TestData.LoadTD27();
        snapshot = module.Data.CreateSnapshot();
    }

    [Test]
    public void Segments_IsNonEmpty()
    {
        CollectionAssert.IsNotEmpty(snapshot.Segments);
    }

    [Test]
    public void SegmentCount_MatchesSegmentsCount()
    {
        var segmentsList = snapshot.Segments.ToList();
        Assert.AreEqual(segmentsList.Count, snapshot.SegmentCount);
    }

    [Test]
    public void SegmentCount_IsPositive()
    {
        Assert.Greater(snapshot.SegmentCount, 0);
    }

    [Test]
    public void TryGetSegment_ExistingAddress_ReturnsTrueAndSegment()
    {
        var firstSegment = snapshot.Segments.First();
        var found = snapshot.TryGetSegment(firstSegment.Address, out var segment);
        Assert.IsTrue(found);
        Assert.IsNotNull(segment);
        Assert.AreEqual(firstSegment.Address, segment.Address);
        Assert.AreEqual(firstSegment.Size, segment.Size);
    }

    [Test]
    public void TryGetSegment_NonExistingAddress_ReturnsFalse()
    {
        // Use an address that's in a gap between segments (the first segment starts at 0,
        // and the next starts at a much higher address). Address 1 is within the first
        // segment's range but is not a segment start address, so it won't be found.
        var nonExistingAddress = ModuleAddress.FromLogicalValue(1);
        var found = snapshot.TryGetSegment(nonExistingAddress, out var segment);
        Assert.IsFalse(found);
        Assert.IsNull(segment);
    }

    [Test]
    public void Relocated_MovesAllSegments()
    {
        var firstSegment = snapshot.Segments.First();
        var fromAddress = firstSegment.Address;
        var toAddress = ModuleAddress.FromLogicalValue(fromAddress.LogicalValue + 0x1000);

        var relocated = snapshot.Relocated(fromAddress, toAddress);

        Assert.AreEqual(snapshot.SegmentCount, relocated.SegmentCount);
        // The first segment should now be at the new address
        var found = relocated.TryGetSegment(toAddress, out var relocatedSegment);
        Assert.IsTrue(found);
        Assert.IsNotNull(relocatedSegment);
    }

    [Test]
    public void Relocated_PreservesSegmentSizes()
    {
        var firstSegment = snapshot.Segments.First();
        var fromAddress = firstSegment.Address;
        var toAddress = ModuleAddress.FromLogicalValue(fromAddress.LogicalValue + 0x1000);

        var relocated = snapshot.Relocated(fromAddress, toAddress);

        var originalSizes = snapshot.Segments.Select(s => s.Size).OrderBy(s => s).ToList();
        var relocatedSizes = relocated.Segments.Select(s => s.Size).OrderBy(s => s).ToList();
        CollectionAssert.AreEqual(originalSizes, relocatedSizes);
    }

    [Test]
    public void Relocated_PreservesSegmentData()
    {
        var firstSegment = snapshot.Segments.First();
        var fromAddress = firstSegment.Address;
        var toAddress = ModuleAddress.FromLogicalValue(fromAddress.LogicalValue + 0x1000);

        var relocated = snapshot.Relocated(fromAddress, toAddress);

        var originalData = firstSegment.CopyData();
        var found = relocated.TryGetSegment(toAddress, out var relocatedSegment);
        Assert.IsTrue(found);
        CollectionAssert.AreEqual(originalData, relocatedSegment.CopyData());
    }

    [Test]
    public void Relocated_WithTreeNode_PreservesSegmentCount()
    {
        var kitRoot = module.Schema.GetKitRoot(1);
        var kit1Root = module.Schema.Kit1Root;
        var partialSnapshot = module.Data.CreatePartialSnapshot(kitRoot);

        var relocated = partialSnapshot.Relocated(kitRoot, kit1Root);
        Assert.AreEqual(partialSnapshot.SegmentCount, relocated.SegmentCount);
    }
}

// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class NodeSnapshotTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        [Fact]
        public void Path_ReturnsSourceNodePath()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var snapshot = module.Data.CreatePartialSnapshot(kitRoot);
            var nodeSnapshot = new NodeSnapshot(kitRoot, snapshot);

            Assert.Equal(kitRoot.Path, nodeSnapshot.Path);
        }

        [Fact]
        public void IsValidForTarget_NullNode_ReturnsFalse()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var snapshot = module.Data.CreatePartialSnapshot(kitRoot);
            var nodeSnapshot = new NodeSnapshot(kitRoot, snapshot);

            Assert.False(nodeSnapshot.IsValidForTarget(null));
        }

        [Fact]
        public void IsValidForTarget_SameNode_ReturnsTrue()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var snapshot = module.Data.CreatePartialSnapshot(kitRoot);
            var nodeSnapshot = new NodeSnapshot(kitRoot, snapshot);

            Assert.True(nodeSnapshot.IsValidForTarget(kitRoot));
        }

        [Fact]
        public void IsValidForTarget_DifferentKitNumber_ReturnsTrue()
        {
            // Kit 1 and Kit 2 have paths like /Kit[1]/... and /Kit[2]/...
            // After variable removal, both become /Kit[]/... so they should match.
            var kit1Root = module.Schema.GetKitRoot(1);
            var kit2Root = module.Schema.GetKitRoot(2);
            var snapshot = module.Data.CreatePartialSnapshot(kit1Root);
            var nodeSnapshot = new NodeSnapshot(kit1Root, snapshot);

            Assert.True(nodeSnapshot.IsValidForTarget(kit2Root));
        }

        [Fact]
        public void IsValidForTarget_CompletelyDifferentNode_ReturnsFalse()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var snapshot = module.Data.CreatePartialSnapshot(kitRoot);
            var nodeSnapshot = new NodeSnapshot(kitRoot, snapshot);

            // The module logical root is a completely different path
            Assert.False(nodeSnapshot.IsValidForTarget(module.Schema.LogicalRoot));
        }

        [Fact]
        public void Relocated_TargetPreservesDataBytes()
        {
            // Copy from Kit 1 then relocate to Kit 2 — underlying bytes must be identical, only addresses offset.
            var kit1Root = module.Schema.GetKitRoot(1);
            var kit2Root = module.Schema.GetKitRoot(2);
            var snapshot = module.Data.CreatePartialSnapshot(kit1Root);
            var nodeSnapshot = new NodeSnapshot(kit1Root, snapshot);

            var relocatedData = snapshot.Relocated(kit1Root, kit2Root);
            var relocatedSnapshot = new NodeSnapshot(kit2Root, relocatedData);

            Assert.Equal(kit2Root.Path, relocatedSnapshot.Path);
            // IsValidForTarget must still hold after relocation
            Assert.True(nodeSnapshot.IsValidForTarget(kit2Root));
            Assert.True(relocatedSnapshot.IsValidForTarget(kit2Root));
            // Data equality: each segment's CopyData must match after relocation (addresses differ, bytes identical)
            Assert.Equal(snapshot.SegmentCount, relocatedData.SegmentCount);
            var originalSegments = snapshot.Segments.OrderBy(s => s.Address.LogicalValue).ToList();
            var relocatedSegments = relocatedData.Segments.OrderBy(s => s.Address.LogicalValue).ToList();
            for (int i = 0; i < originalSegments.Count; i++)
            {
                Assert.Equal(originalSegments[i].CopyData(), relocatedSegments[i].CopyData());
                // Address must be offset by kit distance
                var expectedAddress = originalSegments[i].Address.PlusLogicalOffset(
                    kit2Root.DescendantFieldContainers().Min(fc => fc.Address).LogicalValue -
                    kit1Root.DescendantFieldContainers().Min(fc => fc.Address).LogicalValue);
                Assert.Equal(expectedAddress, relocatedSegments[i].Address);
            }
        }

        [Fact]
        public void Relocated_SameNode_PreservesBytesAndPath()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var snapshot = module.Data.CreatePartialSnapshot(kitRoot);
            var relocated = snapshot.Relocated(kitRoot, kitRoot);
            Assert.Equal(snapshot.Segments.Count(), relocated.Segments.Count());
            foreach (var pair in snapshot.Segments.Zip(relocated.Segments, (a, b) => new { a, b }))
            {
                Assert.Equal(pair.a.CopyData(), pair.b.CopyData());
                Assert.Equal(pair.a.Address, pair.b.Address);
            }
        }

        [Fact]
        public void NodeSnapshot_Path_EqualsSourceNodePath()
        {
            var kit1Root = module.Schema.GetKitRoot(1);
            var kit2Root = module.Schema.GetKitRoot(2);
            var snapshot = module.Data.CreatePartialSnapshot(kit1Root);
            var relocatedData = snapshot.Relocated(kit1Root, kit2Root);
            // New snapshot wrapping kit2 should report kit2 path
            var relocatedNodeSnapshot = new NodeSnapshot(kit2Root, relocatedData);
            Assert.Equal(kit2Root.Path, relocatedNodeSnapshot.Path);
            Assert.NotEqual(kit1Root.Path, relocatedNodeSnapshot.Path);
        }
    }
}

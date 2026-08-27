// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

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
    }
}

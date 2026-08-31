// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class MultiPasteViewModelTest
    {
        private readonly Module module;
        private readonly TreeNode kit1Root;
        private readonly TreeNode kit2Root;
        private readonly NodeSnapshot snapshot;

        public MultiPasteViewModelTest()
        {
            module = TestData.LoadTD27Module();
            kit1Root = module.Schema.GetKitRoot(1);
            kit2Root = module.Schema.GetKitRoot(2);
            var data = ModuleData.FromLogicalRootNode(kit1Root);
            var dataSnapshot = data.CreatePartialSnapshot(kit1Root);
            snapshot = new NodeSnapshot(kit1Root, dataSnapshot);
        }

        [Fact]
        public void Constructor_SetsSnapshot()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            Assert.Same(snapshot, vm.Snapshot);
        }

        [Fact]
        public void Constructor_SetsCandidatesFromList()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            Assert.Single(vm.Candidates);
            Assert.Same(kit2Root, vm.Candidates[0].Candidate);
        }

        [Fact]
        public void Constructor_WithMultipleCandidates_CreatesCheckableCandidateForEach()
        {
            var kit3Root = module.Schema.GetKitRoot(3);
            var candidates = new List<TreeNode> { kit2Root, kit3Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            Assert.Equal(2, vm.Candidates.Count);
            Assert.Same(kit2Root, vm.Candidates[0].Candidate);
            Assert.Same(kit3Root, vm.Candidates[1].Candidate);
        }

        [Fact]
        public void Constructor_WithEmptyCandidates_CreatesEmptyList()
        {
            var vm = new MultiPasteViewModel(snapshot, new List<TreeNode>());
            Assert.Empty(vm.Candidates);
        }

        [Fact]
        public void CheckableCandidate_Checked_DefaultIsFalse()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            Assert.False(vm.Candidates[0].Checked);
        }

        [Fact]
        public void CheckableCandidate_Checked_SetToTrue_FiresPropertyChanged()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm.Candidates[0]).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.Candidates[0].Checked = true;
            Assert.True(vm.Candidates[0].Checked);
            Assert.Contains("Checked", changedProperties);
        }

        [Fact]
        public void CheckableCandidate_Checked_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            vm.Candidates[0].Checked = true;
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm.Candidates[0]).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.Candidates[0].Checked = true; // Same value
            Assert.Empty(changedProperties);
        }

        [Fact]
        public void CheckableCandidate_Candidate_ReturnsCorrectTreeNode()
        {
            var candidates = new List<TreeNode> { kit2Root };
            var vm = new MultiPasteViewModel(snapshot, candidates);
            Assert.Same(kit2Root, vm.Candidates[0].Candidate);
        }
    }
}

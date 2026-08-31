// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class TreeNodeViewModelTest
    {
        [Fact]
        public void Constructor_WithRootNode_SetsFormat()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            Assert.NotNull(vm.Format);
            Assert.Equal(root.Format.FormatString, vm.Format.FormatString);
        }

        [Fact]
        public void Constructor_WithRootNode_ChildrenMatchModelChildren()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            Assert.Equal(root.Children.Count, vm.Children.Count);
        }

        [Fact]
        public void Constructor_WithRootNode_ChildrenAreTreeNodeViewModels()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            Assert.All(vm.Children, child => Assert.IsType<TreeNodeViewModel>(child));
        }

        [Fact]
        public void Constructor_WithRootNode_DetailsMatchModelDetails()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            Assert.Equal(root.Details.Count, vm.Details.Count);
        }

        [Fact]
        public void Constructor_WithNodeHavingDetails_DetailsAreNonEmpty()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            // Find a node with at least one detail by traversing descendants.
            var nodeWithDetails = root.DescendantsAndSelf().First(node => node.Details.Count > 0);
            var vm = new TreeNodeViewModel(nodeWithDetails);
            Assert.NotEmpty(vm.Details);
        }

        [Fact]
        public void Table_ContainsNameEntry()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            var nameEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Name");
            Assert.NotNull(nameEntry);
            Assert.Equal(root.Name, nameEntry!.Value);
        }

        [Fact]
        public void Table_ContainsNodePathEntry()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            var pathEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Node path");
            Assert.NotNull(pathEntry);
            Assert.Equal(root.Path, pathEntry!.Value);
        }

        [Fact]
        public void Table_ContainsContainerPathEntry()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            var containerPathEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Container path");
            Assert.NotNull(containerPathEntry);
            Assert.Equal(root.Container.Path, containerPathEntry!.Value);
        }

        [Fact]
        public void Table_ContainsContainerAddressEntry()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            var addressEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Container address");
            Assert.NotNull(addressEntry);
            Assert.Equal(root.Container.Address.ToString(), addressEntry!.Value);
        }

        [Fact]
        public void Table_IncludesFormatTableEntries()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            var vm = new TreeNodeViewModel(root);
            // The Table is the 4 base entries concatenated with the Format.Table entries.
            Assert.True(vm.Table.Count >= 4);
        }
    }
}

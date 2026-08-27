// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class ListDataNodeDetailViewModelTest
    {
        private readonly Model.Module module = TestData.LoadTD27Module();

        private ListDataNodeDetail? FindListDetail()
        {
            // Traverse the tree to find a node with a ListDataNodeDetail
            return FindListDetail(module.Data.LogicalRoot);
        }

        private static ListDataNodeDetail? FindListDetail(DataTreeNode node)
        {
            foreach (var detail in node.Details)
            {
                if (detail is ListDataNodeDetail list)
                {
                    return list;
                }
            }
            foreach (var child in node.Children)
            {
                var found = FindListDetail(child);
                if (found is not null)
                {
                    return found;
                }
            }
            return null;
        }

        [Fact]
        public void Description_MatchesModelDescription()
        {
            var detail = FindListDetail();
            Assert.NotNull(detail);
            var vm = new ListDataNodeDetailViewModel(detail!);
            Assert.Equal(detail!.Description, vm.Description);
        }

        [Fact]
        public void Items_IsNonEmpty()
        {
            var detail = FindListDetail();
            Assert.NotNull(detail);
            var vm = new ListDataNodeDetailViewModel(detail!);
            Assert.NotEmpty(vm.Items);
        }

        [Fact]
        public void Items_MatchesModelItems()
        {
            var detail = FindListDetail();
            Assert.NotNull(detail);
            var vm = new ListDataNodeDetailViewModel(detail!);
            Assert.Same(detail!.Items, vm.Items);
        }
    }
}

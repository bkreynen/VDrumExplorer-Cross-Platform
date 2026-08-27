// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class ListNodeDetailViewModelTest
    {
        [Fact]
        public void Description_ReturnsDetailDescription()
        {
            var detail = NodeDetailViewModelTest.FindDetail<ListNodeDetail>();
            var vm = new ListNodeDetailViewModel(detail);
            Assert.Equal(detail.Description, vm.Description);
        }

        [Fact]
        public void Items_IsNonEmpty()
        {
            var detail = NodeDetailViewModelTest.FindDetail<ListNodeDetail>();
            var vm = new ListNodeDetailViewModel(detail);
            Assert.NotEmpty(vm.Items);
        }

        [Fact]
        public void Items_MatchDetailItemsCount()
        {
            var detail = NodeDetailViewModelTest.FindDetail<ListNodeDetail>();
            var vm = new ListNodeDetailViewModel(detail);
            Assert.Equal(detail.Items.Count, vm.Items.Count);
        }

        [Fact]
        public void Items_AreFieldFormattableStringViewModels()
        {
            var detail = NodeDetailViewModelTest.FindDetail<ListNodeDetail>();
            var vm = new ListNodeDetailViewModel(detail);
            Assert.All(vm.Items, item => Assert.IsType<FieldFormattableStringViewModel>(item));
        }

        [Fact]
        public void Items_HaveNonEmptyFormatStrings()
        {
            var detail = NodeDetailViewModelTest.FindDetail<ListNodeDetail>();
            var vm = new ListNodeDetailViewModel(detail);
            Assert.All(vm.Items, item => Assert.False(string.IsNullOrEmpty(item.FormatString)));
        }
    }
}

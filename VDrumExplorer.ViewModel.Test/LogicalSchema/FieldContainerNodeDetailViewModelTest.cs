// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class FieldContainerNodeDetailViewModelTest
    {
        [Fact]
        public void Description_ReturnsDetailDescription()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            Assert.Equal(detail.Description, vm.Description);
        }

        [Fact]
        public void Fields_IsNonEmpty()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            Assert.NotEmpty(vm.Fields);
        }

        [Fact]
        public void Fields_MatchContainerFieldsCount()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            Assert.Equal(detail.Container.Fields.Count, vm.Fields.Count);
        }

        [Fact]
        public void Fields_HaveNonEmptyKeys()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            Assert.All(vm.Fields, kv => Assert.False(string.IsNullOrEmpty(kv.Key)));
        }

        [Fact]
        public void Fields_HaveNonEmptyValues()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            Assert.All(vm.Fields, kv => Assert.False(string.IsNullOrEmpty(kv.Value)));
        }

        [Fact]
        public void Fields_KeyContainsOffsetAndDescription()
        {
            var detail = NodeDetailViewModelTest.FindDetail<FieldContainerNodeDetail>();
            var vm = new FieldContainerNodeDetailViewModel(detail);
            foreach (var (field, kv) in detail.Container.Fields.Zip(vm.Fields))
            {
                Assert.Equal($"{field.Offset}: {field.Description}", kv.Key);
            }
        }
    }
}

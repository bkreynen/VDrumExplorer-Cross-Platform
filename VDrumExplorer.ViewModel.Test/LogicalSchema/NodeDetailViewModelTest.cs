// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class NodeDetailViewModelTest
    {
        [Fact]
        public void Create_WithListNodeDetail_ReturnsListNodeDetailViewModel()
        {
            var detail = FindDetail<ListNodeDetail>();
            var vm = NodeDetailViewModel.Create(detail);
            Assert.IsType<ListNodeDetailViewModel>(vm);
        }

        [Fact]
        public void Create_WithFieldContainerNodeDetail_ReturnsFieldContainerNodeDetailViewModel()
        {
            var detail = FindDetail<FieldContainerNodeDetail>();
            var vm = NodeDetailViewModel.Create(detail);
            Assert.IsType<FieldContainerNodeDetailViewModel>(vm);
        }

        [Fact]
        public void Create_WithUnknownType_ThrowsArgumentException()
        {
            var unknown = new UnknownNodeDetail("test");
            Assert.Throws<ArgumentException>(() => NodeDetailViewModel.Create(unknown));
        }

        [Fact]
        public void Description_ReturnsDetailDescription()
        {
            var detail = FindDetail<FieldContainerNodeDetail>();
            var vm = NodeDetailViewModel.Create(detail);
            Assert.Equal(detail.Description, vm.Description);
        }

        [Fact]
        public void Description_ReturnsDetailDescriptionForListNodeDetail()
        {
            var detail = FindDetail<ListNodeDetail>();
            var vm = NodeDetailViewModel.Create(detail);
            Assert.Equal(detail.Description, vm.Description);
        }

        internal static T FindDetail<T>() where T : class, INodeDetail
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            return root.DescendantsAndSelf()
                .SelectMany(node => node.Details)
                .OfType<T>()
                .First();
        }

        private sealed class UnknownNodeDetail : INodeDetail
        {
            public UnknownNodeDetail(string description) => Description = description;
            public string Description { get; }
        }
    }
}

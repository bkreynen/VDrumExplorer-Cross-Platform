// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class FieldContainerDataNodeDetailViewModelTest
    {
        private readonly Module module = TestData.LoadTD27Module();
        private readonly ModuleExplorerViewModel root;

        public FieldContainerDataNodeDetailViewModelTest()
        {
            root = new ModuleExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module);
        }

        private FieldContainerDataNodeDetailViewModel CreateViewModel()
        {
            // Find a FieldContainerDataNodeDetail from the kit root's details
            var kitRootNode = FindKitRoot(root.Root[0]);
            var details = kitRootNode.CreateDetails();
            var fcDetail = details.OfType<FieldContainerDataNodeDetailViewModel>().First();
            return fcDetail;
        }

        private static DataTreeNodeViewModel FindKitRoot(DataTreeNodeViewModel node)
        {
            if (node.IsKitRoot)
            {
                return node;
            }
            foreach (var child in node.Children)
            {
                var result = FindKitRoot(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null!;
        }

        [Fact]
        public void Description_MatchesModelDescription()
        {
            var vm = CreateViewModel();
            var modelDetail = GetModelDetail();
            Assert.Equal(modelDetail.Description, vm.Description);
        }

        [Fact]
        public void Fields_NonEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.Fields);
        }

        [Fact]
        public void Fields_ContainsDataFieldViewModels()
        {
            var vm = CreateViewModel();
            foreach (var field in vm.Fields)
            {
                Assert.IsAssignableFrom<DataFieldViewModel>(field);
            }
        }

        [Fact]
        public void Fields_InitiallyReadOnly_ContainsReadOnlyDataFieldViewModels()
        {
            var vm = CreateViewModel();
            // The root starts in read-only mode, so all fields should be ReadOnlyDataFieldViewModel
            foreach (var field in vm.Fields)
            {
                Assert.IsType<ReadOnlyDataFieldViewModel>(field);
            }
        }

        [Fact]
        public void Fields_PropertyChangedOnReadOnlyChange_RefreshesFields()
        {
            var vm = CreateViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Subscribe to root's PropertyChanged to trigger the OnPropertyChangedHasSubscribers
            // The FieldContainerDataNodeDetailViewModel subscribes to root.PropertyChanged when
            // it has subscribers itself. So we need to subscribe to the VM first.
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => { };

            // Enter edit mode to change ReadOnly from true to false
            root.EditCommand.Execute(null!);

            // The Fields property should have been refreshed
            Assert.Contains(nameof(vm.Fields), changedProperties);

            // After entering edit mode, fields should be editable (not ReadOnlyDataFieldViewModel
            // for non-overlay fields)
            root.CancelEditCommand.Execute(null!);
        }

        [Fact]
        public void Fields_AfterCancelEdit_AreReadOnlyAgain()
        {
            var vm = CreateViewModel();
            // Subscribe to trigger the PropertyChanged subscription
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => { };

            // Enter edit mode
            root.EditCommand.Execute(null!);
            // Fields should now be editable
            // Cancel edit
            root.CancelEditCommand.Execute(null!);

            // Fields should be read-only again
            foreach (var field in vm.Fields)
            {
                Assert.IsType<ReadOnlyDataFieldViewModel>(field);
            }
        }

        [Fact]
        public void Description_NotEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.Description);
        }

        private FieldContainerDataNodeDetail GetModelDetail()
        {
            var kitRoot = module.Schema.GetKitRoot(1);
            var dataKitRoot = new DataTreeNode(module.Data, kitRoot);
            return dataKitRoot.Details.OfType<FieldContainerDataNodeDetail>().First();
        }
    }
}

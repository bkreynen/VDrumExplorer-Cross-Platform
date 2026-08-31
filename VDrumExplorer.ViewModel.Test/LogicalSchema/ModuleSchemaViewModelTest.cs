// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class ModuleSchemaViewModelTest
    {
        [Fact]
        public void Constructor_WithSchema_SetsRoot()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.NotNull(vm.Root);
        }

        [Fact]
        public void Constructor_WithSchema_RootIsSingleItemCollectionWithOneItem()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.Single(vm.Root);
        }

        [Fact]
        public void Constructor_WithSchema_RootNodeWrapsSchemaLogicalRoot()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            // The root TreeNodeViewModel wraps the schema's LogicalRoot.
            // We verify this by checking the Format string matches.
            Assert.Equal(schema.LogicalRoot.Format.FormatString, vm.Root[0].Format.FormatString);
        }

        [Fact]
        public void Title_ContainsSchemaName()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.Contains(schema.Identifier.Name, vm.Title);
        }

        [Fact]
        public void Title_ContainsSchemaExplorerPrefix()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.StartsWith("Schema Explorer:", vm.Title);
        }

        [Fact]
        public void Title_ContainsSoftwareRevisionInHex()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.Contains($"rev 0x{schema.Identifier.SoftwareRevision:x}", vm.Title);
        }

        [Fact]
        public void SelectedNode_InitiallySetToRootNode()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            Assert.Same(vm.Root[0], vm.SelectedNode);
        }

        [Fact]
        public void SelectedNode_SetToNewValue_FiresPropertyChanged()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            var newSelection = vm.Root[0].Children[0];
            vm.SelectedNode = newSelection;

            Assert.Same(newSelection, vm.SelectedNode);
            Assert.Contains(nameof(ModuleSchemaViewModel.SelectedNode), changedProperties);
        }

        [Fact]
        public void SelectedNode_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.SelectedNode = vm.SelectedNode; // Same value

            Assert.Empty(changedProperties);
        }

        [Fact]
        public void SelectedNode_SetToNull_FiresPropertyChanged()
        {
            var schema = TestData.LoadTD27Schema();
            var vm = new ModuleSchemaViewModel(schema);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.SelectedNode = null;

            Assert.Null(vm.SelectedNode);
            Assert.Contains(nameof(ModuleSchemaViewModel.SelectedNode), changedProperties);
        }
    }
}

// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.Model;
using VDrumExplorer.Proto;
using VDrumExplorer.ViewModel.Dialogs;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class CopyKitViewModelTest
    {
        private static Module LoadModule()
        {
            using (var stream = typeof(CopyKitViewModelTest).Assembly.GetManifestResourceStream("td27.vdrum"))
            {
                return (Module)ProtoIo.ReadModel(stream!, NullLogger.Instance);
            }
        }

        private readonly Module module;
        private readonly Kit kit;

        public CopyKitViewModelTest()
        {
            module = LoadModule();
            kit = module.ExportKit(1);
        }

        [Fact]
        public void Constructor_SetsSourceKitName()
        {
            var vm = new CopyKitViewModel(module, kit);
            Assert.Equal(kit.GetKitName(), vm.SourceKitName);
        }

        [Fact]
        public void DestinationKitNumber_DefaultIsOne()
        {
            var vm = new CopyKitViewModel(module, kit);
            Assert.Equal(1, vm.DestinationKitNumber);
        }

        [Fact]
        public void DestinationKitNumber_SetValidValue_UpdatesProperty()
        {
            var vm = new CopyKitViewModel(module, kit);
            vm.DestinationKitNumber = 5;
            Assert.Equal(5, vm.DestinationKitNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DestinationKitNumber_InvalidValue_Throws(int value)
        {
            var vm = new CopyKitViewModel(module, kit);
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.DestinationKitNumber = value);
        }

        [Fact]
        public void DestinationKitNumber_AboveMax_Throws()
        {
            var vm = new CopyKitViewModel(module, kit);
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.DestinationKitNumber = module.Schema.Kits + 1);
        }

        [Fact]
        public void DestinationKitNumber_SetValidValue_FiresPropertyChangedForDestinationKitNameAndCopyEnabled()
        {
            var vm = new CopyKitViewModel(module, kit);
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.DestinationKitNumber = 3;
            Assert.Contains(nameof(vm.DestinationKitNumber), changedProperties);
            Assert.Contains(nameof(vm.DestinationKitName), changedProperties);
            Assert.Contains(nameof(vm.CopyEnabled), changedProperties);
        }

        [Fact]
        public void DestinationKitName_ReturnsNameFromModule()
        {
            var vm = new CopyKitViewModel(module, kit);
            Assert.Equal(module.GetKitName(vm.DestinationKitNumber), vm.DestinationKitName);
        }

        [Fact]
        public void CopyEnabled_SameKitNumber_ReturnsFalse()
        {
            kit.DefaultKitNumber = 1;
            var vm = new CopyKitViewModel(module, kit);
            Assert.False(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_DifferentKitNumber_ReturnsTrue()
        {
            kit.DefaultKitNumber = 1;
            var vm = new CopyKitViewModel(module, kit);
            vm.DestinationKitNumber = 2;
            Assert.True(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_SetDestinationToSameAsDefault_ReturnsFalse()
        {
            kit.DefaultKitNumber = 3;
            var vm = new CopyKitViewModel(module, kit);
            vm.DestinationKitNumber = 3;
            Assert.False(vm.CopyEnabled);
        }
    }
}

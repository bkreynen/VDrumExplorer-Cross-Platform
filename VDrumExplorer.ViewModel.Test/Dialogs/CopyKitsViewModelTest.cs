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
    public class CopyKitsViewModelTest
    {
        private static Module LoadModule()
        {
            using (var stream = typeof(CopyKitsViewModelTest).Assembly.GetManifestResourceStream("td27.vdrum"))
            {
                return (Module)ProtoIo.ReadModel(stream!, NullLogger.Instance);
            }
        }

        private readonly Module module;
        private readonly CopyKitsViewModel vm;

        public CopyKitsViewModelTest()
        {
            module = LoadModule();
            vm = new CopyKitsViewModel(module);
        }

        [Fact]
        public void KitCount_MatchesModuleSchemaKits()
        {
            Assert.Equal(module.Schema.Kits, vm.KitCount);
        }

        [Fact]
        public void SourceFrom_DefaultIsOne()
        {
            Assert.Equal(1, vm.SourceFrom);
        }

        [Fact]
        public void SourceTo_DefaultIsKitCount()
        {
            Assert.Equal(vm.KitCount, vm.SourceTo);
        }

        [Fact]
        public void DestinationFrom_DefaultIsOne()
        {
            Assert.Equal(1, vm.DestinationFrom);
        }

        [Fact]
        public void SourceFrom_SetValidValue_UpdatesProperty()
        {
            vm.SourceFrom = 3;
            Assert.Equal(3, vm.SourceFrom);
        }

        [Fact]
        public void SourceTo_SetValidValue_UpdatesProperty()
        {
            vm.SourceTo = 5;
            Assert.Equal(5, vm.SourceTo);
        }

        [Fact]
        public void DestinationFrom_SetValidValue_UpdatesProperty()
        {
            vm.DestinationFrom = 4;
            Assert.Equal(4, vm.DestinationFrom);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SourceFrom_InvalidValue_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.SourceFrom = value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SourceTo_InvalidValue_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.SourceTo = value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DestinationFrom_InvalidValue_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.DestinationFrom = value);
        }

        [Fact]
        public void SourceFrom_AboveMax_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.SourceFrom = vm.KitCount + 1);
        }

        [Fact]
        public void CopyCount_SourceToGreaterThanOrEqualSourceFrom_ReturnsCount()
        {
            vm.SourceFrom = 2;
            vm.SourceTo = 5;
            Assert.Equal(4, vm.CopyCount);
        }

        [Fact]
        public void CopyCount_SourceToLessThanSourceFrom_ReturnsZero()
        {
            vm.SourceFrom = 5;
            vm.SourceTo = 2;
            Assert.Equal(0, vm.CopyCount);
        }

        [Fact]
        public void CopyCount_SingleKit_ReturnsOne()
        {
            vm.SourceFrom = 3;
            vm.SourceTo = 3;
            Assert.Equal(1, vm.CopyCount);
        }

        [Fact]
        public void CopyEnabled_SourceToLessThanSourceFrom_ReturnsFalse()
        {
            vm.SourceFrom = 5;
            vm.SourceTo = 2;
            Assert.False(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_SameSourceAndDestination_ReturnsFalse()
        {
            vm.SourceFrom = 1;
            vm.SourceTo = 1;
            vm.DestinationFrom = 1;
            Assert.False(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_DestinationRangeExceedsKitCount_ReturnsFalse()
        {
            vm.SourceFrom = 1;
            vm.SourceTo = 3;
            vm.DestinationFrom = vm.KitCount; // destEnd = KitCount + 2 > KitCount
            Assert.False(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_ValidDifferentRange_ReturnsTrue()
        {
            vm.SourceFrom = 1;
            vm.SourceTo = 3;
            vm.DestinationFrom = 5;
            Assert.True(vm.CopyEnabled);
        }

        [Fact]
        public void CopyEnabled_DestinationEndExactlyAtKitCount_ReturnsTrue()
        {
            vm.SourceFrom = 1;
            vm.SourceTo = 3;
            vm.DestinationFrom = vm.KitCount - 2; // destEnd = KitCount
            Assert.True(vm.CopyEnabled);
        }

        [Fact]
        public void SourceFrom_SetValidValue_FiresPropertyChangedForCopyCountAndCopyEnabled()
        {
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.SourceFrom = 3;
            Assert.Contains(nameof(vm.SourceFrom), changedProperties);
            Assert.Contains(nameof(vm.CopyCount), changedProperties);
            Assert.Contains(nameof(vm.CopyEnabled), changedProperties);
        }

        [Fact]
        public void SourceTo_SetValidValue_FiresPropertyChangedForCopyCountAndCopyEnabled()
        {
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.SourceTo = 5;
            Assert.Contains(nameof(vm.SourceTo), changedProperties);
            Assert.Contains(nameof(vm.CopyCount), changedProperties);
            Assert.Contains(nameof(vm.CopyEnabled), changedProperties);
        }

        [Fact]
        public void DestinationFrom_SetValidValue_FiresPropertyChangedForCopyCountAndCopyEnabled()
        {
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.DestinationFrom = 5;
            Assert.Contains(nameof(vm.DestinationFrom), changedProperties);
            Assert.Contains(nameof(vm.CopyEnabled), changedProperties);
        }
    }
}

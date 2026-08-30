// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class DeviceViewModelTest
    {
        [Fact]
        public void InitialState_WithoutDevice_HasExpectedDefaults()
        {
            // Collapsed duplicate initial-state checks (DeviceConnected, ConnectedDeviceName, ConnectedDevice).
            var vm = new DeviceViewModel();
            Assert.False(vm.DeviceConnected);
            Assert.Equal("(None)", vm.ConnectedDeviceName);
            Assert.Null(vm.ConnectedDevice);
        }

        [Fact]
        public void ConnectedDevice_SetNull_DoesNotFirePropertyChanged()
        {
            var vm = new DeviceViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.ConnectedDevice = null; // Already null, no change
            Assert.Empty(changedProperties);
        }
    }
}

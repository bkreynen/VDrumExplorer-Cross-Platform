// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using VDrumExplorer.ViewModel.Status;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class StatusViewModelTest
    {
        [Fact]
        public void InitialState_IsEmpty()
        {
            var vm = new StatusViewModel();
            Assert.Equal("", vm.Message);
            Assert.Equal("", vm.ErrorMessage);
        }

        [Fact]
        public void SetMessage_UpdatesMessageAndFiresPropertyChanged()
        {
            var vm = new StatusViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetMessage("Switched to kit 12, Jazz");
            Assert.Equal("Switched to kit 12, Jazz", vm.Message);
            Assert.Contains(nameof(StatusViewModel.Message), handler.ChangedProperties);
        }

        [Fact]
        public void SetError_UpdatesErrorMessageAndFiresPropertyChanged()
        {
            var vm = new StatusViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetError("Could not load kit: timeout");
            Assert.Equal("Could not load kit: timeout", vm.ErrorMessage);
            Assert.Contains(nameof(StatusViewModel.ErrorMessage), handler.ChangedProperties);
        }

        [Fact]
        public void SetMessage_WithIdenticalText_StillFiresPropertyChanged()
        {
            var vm = new StatusViewModel();
            vm.SetMessage("Kick volume set to 80");
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetMessage("Kick volume set to 80");
            Assert.Contains(nameof(StatusViewModel.Message), handler.ChangedProperties);
        }

        [Fact]
        public void SetError_WithIdenticalText_StillFiresPropertyChanged()
        {
            var vm = new StatusViewModel();
            vm.SetError("Device not connected");
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetError("Device not connected");
            Assert.Contains(nameof(StatusViewModel.ErrorMessage), handler.ChangedProperties);
        }

        [Fact]
        public void SetMessage_DoesNotTouchErrorMessage()
        {
            var vm = new StatusViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetMessage("Kit loaded in 12 s");
            Assert.Equal("", vm.ErrorMessage);
            Assert.DoesNotContain(nameof(StatusViewModel.ErrorMessage), handler.ChangedProperties);
        }

        [Fact]
        public void SetError_DoesNotTouchMessage()
        {
            var vm = new StatusViewModel();
            var handler = new PropertyChangedHandler();
            vm.PropertyChanged += handler.OnPropertyChanged;
            vm.SetError("Could not switch kit: device not connected");
            Assert.Equal("", vm.Message);
            Assert.DoesNotContain(nameof(StatusViewModel.Message), handler.ChangedProperties);
        }

        [Fact]
        public void SetMessage_WithNull_StoresEmptyString()
        {
            var vm = new StatusViewModel();
            vm.SetMessage(null!);
            Assert.Equal("", vm.Message);
        }

        [Fact]
        public void SetError_WithNull_StoresEmptyString()
        {
            var vm = new StatusViewModel();
            vm.SetError(null!);
            Assert.Equal("", vm.ErrorMessage);
        }

        private sealed class PropertyChangedHandler
        {
            internal List<string> ChangedProperties { get; } = new List<string>();

            public void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
                ChangedProperties.Add(e.PropertyName!);
        }
    }
}

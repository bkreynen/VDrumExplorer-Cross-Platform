// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class KeyValueViewModelTest
    {
        [Fact]
        public void Constructor_TwoArguments_SetsKeyAndValue()
        {
            var vm = new KeyValueViewModel("myKey", "myValue");
            Assert.Equal("myKey", vm.Key);
            Assert.Equal("myValue", vm.Value);
        }

        [Fact]
        public void Constructor_TwoArguments_ToolTipsAreNullByDefault()
        {
            var vm = new KeyValueViewModel("key", "value");
            Assert.Null(vm.KeyToolTip);
            Assert.Null(vm.ValueToolTip);
        }

        [Fact]
        public void Constructor_FourArguments_SetsAllProperties()
        {
            var vm = new KeyValueViewModel("key", "value", "keyTip", "valueTip");
            Assert.Equal("key", vm.Key);
            Assert.Equal("value", vm.Value);
            Assert.Equal("keyTip", vm.KeyToolTip);
            Assert.Equal("valueTip", vm.ValueToolTip);
        }

        [Fact]
        public void Constructor_FourArguments_NullToolTipsAccepted()
        {
            var vm = new KeyValueViewModel("key", "value", null, null);
            Assert.Equal("key", vm.Key);
            Assert.Equal("value", vm.Value);
            Assert.Null(vm.KeyToolTip);
            Assert.Null(vm.ValueToolTip);
        }

        [Fact]
        public void Constructor_FourArguments_PartialNullToolTips()
        {
            var vm = new KeyValueViewModel("key", "value", "keyTip", null);
            Assert.Equal("keyTip", vm.KeyToolTip);
            Assert.Null(vm.ValueToolTip);
        }
    }
}

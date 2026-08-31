// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.LogicalSchema
{
    public class FieldFormattableStringViewModelTest
    {
        [Fact]
        public void Constructor_SetsFormatString()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            Assert.Equal(model.FormatString, vm.FormatString);
        }

        [Fact]
        public void FormatString_IsNonEmpty()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            Assert.False(string.IsNullOrEmpty(vm.FormatString));
        }

        [Fact]
        public void Table_ContainsContainerPathEntry()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            var containerPathEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Container path");
            Assert.NotNull(containerPathEntry);
            Assert.Equal(model.Container.Path, containerPathEntry!.Value);
        }

        [Fact]
        public void Table_ContainsFormatStringEntry()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            var formatStringEntry = vm.Table.FirstOrDefault(kv => kv.Key == "Format string");
            Assert.NotNull(formatStringEntry);
            Assert.Equal(model.FormatString, formatStringEntry!.Value);
        }

        [Fact]
        public void Table_ContainsFieldPathEntries()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            for (int i = 0; i < model.FormatPaths.Count; i++)
            {
                var entry = vm.Table.FirstOrDefault(kv => kv.Key == $"Field path {i}");
                Assert.NotNull(entry);
                Assert.Equal(model.FormatPaths[i], entry!.Value);
            }
        }

        [Fact]
        public void Table_HasExpectedCount()
        {
            var model = FindFieldFormattableString();
            var vm = new FieldFormattableStringViewModel(model);
            // 2 base entries (Container path, Format string) + one per format path.
            Assert.Equal(2 + model.FormatPaths.Count, vm.Table.Count);
        }

        internal static FieldFormattableString FindFieldFormattableString()
        {
            var root = TestData.LoadTD27Schema().LogicalRoot;
            return root.DescendantsAndSelf().First().Format;
        }
    }
}

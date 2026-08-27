// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class FileFiltersTest
    {
        [Fact]
        public void JsonFiles_IsNonEmptyAndContainsJsonExtension()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.JsonFiles));
            Assert.Contains(".json", FileFilters.JsonFiles);
        }

        [Fact]
        public void KitFiles_IsNonEmptyAndContainsVkitExtension()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.KitFiles));
            Assert.Contains(".vkit", FileFilters.KitFiles);
        }

        [Fact]
        public void ModuleFiles_IsNonEmptyAndContainsVdrumExtension()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.ModuleFiles));
            Assert.Contains(".vdrum", FileFilters.ModuleFiles);
        }

        [Fact]
        public void InstrumentAudioFiles_IsNonEmptyAndContainsVaudioExtension()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.InstrumentAudioFiles));
            Assert.Contains(".vaudio", FileFilters.InstrumentAudioFiles);
        }

        [Fact]
        public void LogFiles_IsNonEmptyAndContainsJsonExtension()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.LogFiles));
            Assert.Contains(".json", FileFilters.LogFiles);
        }

        [Fact]
        public void AllExplorerFiles_ContainsAllExplorerExtensions()
        {
            Assert.False(string.IsNullOrEmpty(FileFilters.AllExplorerFiles));
            Assert.Contains(".vdrum", FileFilters.AllExplorerFiles);
            Assert.Contains(".vkit", FileFilters.AllExplorerFiles);
            Assert.Contains(".vaudio", FileFilters.AllExplorerFiles);
        }

        [Fact]
        public void AllExplorerFiles_ContainsPipeSeparatorForMultipleFilters()
        {
            // The AllExplorerFiles filter contains multiple sub-filters separated by '|'
            Assert.Contains("|", FileFilters.AllExplorerFiles);
        }

        [Theory]
        [InlineData(FileFilters.JsonFiles, "*.json")]
        [InlineData(FileFilters.KitFiles, "*.vkit")]
        [InlineData(FileFilters.ModuleFiles, "*.vdrum")]
        [InlineData(FileFilters.InstrumentAudioFiles, "*.vaudio")]
        [InlineData(FileFilters.LogFiles, "*.json")]
        public void Filter_ContainsWildcardPattern(string filter, string pattern)
        {
            Assert.Contains(pattern, filter);
        }
    }
}

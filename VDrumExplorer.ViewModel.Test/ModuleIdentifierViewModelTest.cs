// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Home;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class ModuleIdentifierViewModelTest
    {
        [Fact]
        public void DisplayName_WithZeroSoftwareRevision_ReturnsJustName()
        {
            var identifier = ModuleIdentifier.TD27; // SoftwareRevision is 0
            var vm = new ModuleIdentifierViewModel(identifier, false);
            Assert.Equal(identifier.Name, vm.DisplayName);
        }

        [Fact]
        public void DisplayName_WithNonZeroSoftwareRevision_IncludesRevision()
        {
            var identifier = ModuleIdentifier.TD17.WithSoftwareRevision(0x01);
            var vm = new ModuleIdentifierViewModel(identifier, true);
            Assert.Equal("TD-17 (rev 0x1)", vm.DisplayName);
        }

        [Fact]
        public void DisplayName_WithNonZeroSoftwareRevision_FormatsAsHex()
        {
            var identifier = ModuleIdentifier.TD17.WithSoftwareRevision(0x0a);
            var vm = new ModuleIdentifierViewModel(identifier, true);
            Assert.Contains("0xa", vm.DisplayName);
        }

        [Fact]
        public void Identifier_ReturnsModel()
        {
            var identifier = ModuleIdentifier.TD27;
            var vm = new ModuleIdentifierViewModel(identifier, false);
            Assert.Same(identifier, vm.Identifier);
        }

        [Fact]
        public void GetIdentifiersForKnownSchemas_ReturnsNonEmptyList()
        {
            var identifiers = ModuleIdentifierViewModel.GetIdentifiersForKnownSchemas();
            Assert.NotEmpty(identifiers);
        }

        [Fact]
        public void GetIdentifiersForKnownSchemas_ReturnsSortedListByName()
        {
            var identifiers = ModuleIdentifierViewModel.GetIdentifiersForKnownSchemas();
            var names = identifiers.Select(vm => vm.Identifier.Name).ToList();
            var sortedNames = names.OrderBy(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }

        [Fact]
        public void GetIdentifiersForKnownSchemas_AllHaveNonNullIdentifier()
        {
            var identifiers = ModuleIdentifierViewModel.GetIdentifiersForKnownSchemas();
            Assert.All(identifiers, vm => Assert.NotNull(vm.Identifier));
        }

        [Fact]
        public void GetIdentifiersForKnownSchemas_ReturnsViewModelsWithDisplayNames()
        {
            var identifiers = ModuleIdentifierViewModel.GetIdentifiersForKnownSchemas();
            Assert.All(identifiers, vm => Assert.False(string.IsNullOrEmpty(vm.DisplayName)));
        }
    }
}

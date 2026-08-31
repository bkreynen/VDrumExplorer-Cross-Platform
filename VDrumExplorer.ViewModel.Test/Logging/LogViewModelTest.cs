// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VDrumExplorer.ViewModel.Logging;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Logging
{
    public class LogViewModelTest
    {
        private static readonly Instant FixedInstant = Instant.FromUtc(2023, 6, 15, 10, 30, 0);

        [Fact]
        public void Constructor_CreatesNonNullLogger()
        {
            var vm = new LogViewModel();
            Assert.NotNull(vm.Logger);
        }

        [Fact]
        public void LogEntries_StartsEmpty()
        {
            var vm = new LogViewModel();
            Assert.Empty(vm.LogEntries);
        }

        [Fact]
        public void Logger_LogInformation_AddsEntryToLogEntries()
        {
            var vm = new LogViewModel();
            vm.Logger.LogInformation("test message");
            Assert.Single(vm.LogEntries);
            Assert.Equal("test message", vm.LogEntries[0].Text);
            Assert.Equal(LogLevel.Information, vm.LogEntries[0].Level);
        }

        [Fact]
        public void Logger_LogWarning_AddsEntryWithWarningLevel()
        {
            var vm = new LogViewModel();
            vm.Logger.LogWarning("warning message");
            Assert.Single(vm.LogEntries);
            Assert.Equal(LogLevel.Warning, vm.LogEntries[0].Level);
        }

        [Fact]
        public void Logger_LogDebug_AddsEntryEvenWhenFilterIsInformation()
        {
            // Debug is below the default filter level (Information), but the entry
            // is still added to the allLogEntries list (just not shown). After
            // lowering the filter, it should appear.
            var vm = new LogViewModel();
            vm.Logger.LogDebug("debug message");
            Assert.Empty(vm.LogEntries);
            vm.FilterLevel = LogLevel.Debug;
            Assert.Single(vm.LogEntries);
        }

        [Fact]
        public void FilterLevel_SetToWarning_HidesInformationEntries()
        {
            var vm = new LogViewModel();
            vm.Logger.LogInformation("info");
            vm.Logger.LogWarning("warn");
            Assert.Equal(2, vm.LogEntries.Count);
            vm.FilterLevel = LogLevel.Warning;
            Assert.Single(vm.LogEntries);
            Assert.Equal("warn", vm.LogEntries[0].Text);
        }

        [Fact]
        public void FilterLevel_SetToWarning_FiresPropertyChangedForFilterLevelAndLogEntries()
        {
            var vm = new LogViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.FilterLevel = LogLevel.Warning;
            Assert.Contains(nameof(vm.FilterLevel), changedProperties);
            Assert.Contains(nameof(vm.LogEntries), changedProperties);
        }

        [Fact]
        public void FilterLevel_SetToSameValue_DoesNotFirePropertyChanged()
        {
            var vm = new LogViewModel();
            var changedProperties = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);
            vm.FilterLevel = vm.FilterLevel; // Default is Information
            Assert.Empty(changedProperties);
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var vm = new LogViewModel();
            vm.Logger.LogInformation("one");
            vm.Logger.LogInformation("two");
            Assert.Equal(2, vm.LogEntries.Count);
            vm.Clear();
            Assert.Empty(vm.LogEntries);
        }

        [Fact]
        public void AllFilterLevels_ContainsAllLogLevelValues()
        {
            var vm = new LogViewModel();
            var expected = Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().ToList();
            Assert.Equal(expected, vm.AllFilterLevels);
        }

        [Fact]
        public void AllFilterLevels_IsReadOnlyCollection()
        {
            var vm = new LogViewModel();
            // The list is created via .AsReadOnly(), which returns a ReadOnlyCollection<T>.
            Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<LogLevel>>(vm.AllFilterLevels);
        }

        [Fact]
        public void LogVersion_WithAssemblyHavingVersionAttribute_LogsVersionMessage()
        {
            var vm = new LogViewModel(new FakeClock(FixedInstant));
            vm.LogVersion(typeof(LogViewModelTest));
            Assert.Single(vm.LogEntries);
            Assert.Contains("V-Drum Explorer version", vm.LogEntries[0].Text);
        }

        [Fact]
        public void LogVersion_WithTypeWithoutVersionAttribute_LogsNotFoundMessage()
        {
            // Create a type in a dynamic assembly that has no AssemblyInformationalVersionAttribute.
            var assemblyName = new AssemblyName("DynamicTestAssembly");
            var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(assemblyName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");
            var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);
            var dynamicType = typeBuilder.CreateType();

            var vm = new LogViewModel(new FakeClock(FixedInstant));
            vm.LogVersion(dynamicType);
            Assert.Single(vm.LogEntries);
            Assert.Equal("Version attribute not found.", vm.LogEntries[0].Text);
        }

        [Fact]
        public void Logger_WithException_AddsEntryContainingExceptionInfo()
        {
            var vm = new LogViewModel();
            var exception = new InvalidOperationException("boom");
            vm.Logger.LogError(exception, "failed");
            Assert.Single(vm.LogEntries);
            Assert.Contains("failed", vm.LogEntries[0].Text);
            Assert.Contains("InvalidOperationException", vm.LogEntries[0].Text);
        }

        [Fact]
        public void Save_WritesJsonFileWithEntries()
        {
            var vm = new LogViewModel(new FakeClock(FixedInstant));
            vm.Logger.LogInformation("hello");
            var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"logtest-{Guid.NewGuid()}.json");
            try
            {
                vm.Save(file);
                Assert.True(System.IO.File.Exists(file));
                var content = System.IO.File.ReadAllText(file);
                Assert.Contains("hello", content);
                Assert.Contains("Information", content);
            }
            finally
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }
            }
        }
    }
}

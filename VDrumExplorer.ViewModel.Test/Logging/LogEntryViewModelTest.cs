// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using VDrumExplorer.ViewModel.Logging;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Logging
{
    public class LogEntryViewModelTest
    {
        [Fact]
        public void Constructor_SetsEntry()
        {
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "msg", LogLevel.Information, null);
            var vm = new LogEntryViewModel(entry);
            Assert.Same(entry, vm.Entry);
        }

        [Fact]
        public void Level_ReturnsEntryLevel()
        {
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "msg", LogLevel.Error, null);
            var vm = new LogEntryViewModel(entry);
            Assert.Equal(LogLevel.Error, vm.Level);
        }

        [Fact]
        public void Text_WithoutException_ReturnsMessage()
        {
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "hello world", LogLevel.Information, null);
            var vm = new LogEntryViewModel(entry);
            Assert.Equal("hello world", vm.Text);
        }

        [Fact]
        public void Text_WithException_IncludesExceptionTypeAndMessage()
        {
            var exception = new InvalidOperationException("boom");
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "failed", LogLevel.Error, exception);
            var vm = new LogEntryViewModel(entry);
            Assert.Contains("failed", vm.Text);
            Assert.Contains("InvalidOperationException", vm.Text);
            Assert.Contains("boom", vm.Text);
        }

        [Fact]
        public void ToolTip_WithoutException_IsNull()
        {
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "msg", LogLevel.Information, null);
            var vm = new LogEntryViewModel(entry);
            Assert.Null(vm.ToolTip);
        }

        [Fact]
        public void ToolTip_WithException_ReturnsStackTrace()
        {
            // The exception must be thrown and caught so that its StackTrace is populated;
            // an exception that is only constructed (never thrown) has a null StackTrace.
            InvalidOperationException exception;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException ex) { exception = ex; }
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 0), "msg", LogLevel.Error, exception);
            var vm = new LogEntryViewModel(entry);
            Assert.NotNull(vm.ToolTip);
            // Exception.StackTrace contains stack frames ("   at ...") but not the
            // exception type name; verify the trace was populated by checking for the
            // stack frame marker and the test method name within it.
            Assert.Contains("   at ", vm.ToolTip);
            Assert.Contains(nameof(ToolTip_WithException_ReturnsStackTrace), vm.ToolTip);
        }

        [Fact]
        public void Timestamp_FormattedCorrectly()
        {
            // Use a fixed UTC instant; the formatting uses the system timezone, so we just
            // verify the format pattern (HH:mm:ss.fff) is present.
            var entry = new LogEntry(Instant.FromUtc(2023, 1, 15, 10, 30, 45) + Duration.FromMilliseconds(123), "msg", LogLevel.Information, null);
            var vm = new LogEntryViewModel(entry);
            // The timestamp should match the pattern HH:mm:ss.fff in the local timezone.
            // We verify it has the right length (12 chars: HH:mm:ss.fff).
            Assert.Equal(12, vm.Timestamp.Length);
            Assert.Equal(':', vm.Timestamp[2]);
            Assert.Equal(':', vm.Timestamp[5]);
            Assert.Equal('.', vm.Timestamp[8]);
        }
    }
}

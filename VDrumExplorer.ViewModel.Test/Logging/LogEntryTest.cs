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
    public class LogEntryTest
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var timestamp = Instant.FromUtc(2023, 1, 15, 10, 30, 0);
            var exception = new InvalidOperationException("test error");
            var entry = new LogEntry(timestamp, "test message", LogLevel.Warning, exception);

            Assert.Equal(timestamp, entry.Timestamp);
            Assert.Equal("test message", entry.Message);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Same(exception, entry.Exception);
        }

        [Fact]
        public void Constructor_WithNullException_AcceptsNull()
        {
            var timestamp = Instant.FromUtc(2023, 1, 15, 10, 30, 0);
            var entry = new LogEntry(timestamp, "no error", LogLevel.Information, null);

            Assert.Equal(timestamp, entry.Timestamp);
            Assert.Equal("no error", entry.Message);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Null(entry.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void Constructor_PreservesLogLevel(LogLevel level)
        {
            var entry = new LogEntry(Instant.MinValue, "msg", level, null);
            Assert.Equal(level, entry.Level);
        }
    }
}

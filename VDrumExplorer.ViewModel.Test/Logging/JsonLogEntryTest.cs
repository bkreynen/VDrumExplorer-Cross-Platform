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
    public class JsonLogEntryTest
    {
        private static readonly Instant Timestamp = Instant.FromUtc(2023, 6, 15, 10, 30, 0);

        [Fact]
        public void Constructor_WithoutException_SetsTimestampLevelMessage()
        {
            var entry = new LogEntry(Timestamp, "test message", LogLevel.Warning, null);
            var json = new JsonLogEntry(entry);
            Assert.Equal(Timestamp, json.Timestamp);
            Assert.Equal(LogLevel.Warning, json.Level);
            Assert.Equal("test message", json.Message);
            Assert.Null(json.Exception);
        }

        [Fact]
        public void Constructor_WithException_SetsExceptionProperties()
        {
            var exception = new InvalidOperationException("outer error");
            var entry = new LogEntry(Timestamp, "msg", LogLevel.Error, exception);
            var json = new JsonLogEntry(entry);
            Assert.NotNull(json.Exception);
            Assert.Equal("InvalidOperationException", json.Exception!.Type);
            Assert.Equal("outer error", json.Exception.Message);
            Assert.Null(json.Exception.InnerException);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsInnerExceptionRecursively()
        {
            var inner = new ArgumentException("inner error");
            var outer = new InvalidOperationException("outer error", inner);
            var entry = new LogEntry(Timestamp, "msg", LogLevel.Error, outer);
            var json = new JsonLogEntry(entry);
            Assert.NotNull(json.Exception);
            Assert.Equal("InvalidOperationException", json.Exception!.Type);
            Assert.Equal("outer error", json.Exception.Message);
            Assert.NotNull(json.Exception.InnerException);
            Assert.Equal("ArgumentException", json.Exception.InnerException!.Type);
            Assert.Equal("inner error", json.Exception.InnerException.Message);
            Assert.Null(json.Exception.InnerException.InnerException);
        }

        [Fact]
        public void Constructor_WithThrownException_SetsStackTrace()
        {
            InvalidOperationException exception;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException ex) { exception = ex; }
            var entry = new LogEntry(Timestamp, "msg", LogLevel.Error, exception);
            var json = new JsonLogEntry(entry);
            Assert.NotNull(json.Exception);
            Assert.NotNull(json.Exception!.StackTrace);
            Assert.Contains(nameof(Constructor_WithThrownException_SetsStackTrace), json.Exception.StackTrace);
        }

        [Fact]
        public void Constructor_WithNullException_ExceptionPropertyIsNull()
        {
            var entry = new LogEntry(Timestamp, "no error", LogLevel.Information, null);
            var json = new JsonLogEntry(entry);
            Assert.Null(json.Exception);
        }

        [Fact]
        public void JsonException_Type_ReturnsExceptionTypeNameWithoutNamespace()
        {
            var exception = new System.IO.IOException("disk error");
            var entry = new LogEntry(Timestamp, "msg", LogLevel.Error, exception);
            var json = new JsonLogEntry(entry);
            Assert.Equal("IOException", json.Exception!.Type);
        }
    }
}

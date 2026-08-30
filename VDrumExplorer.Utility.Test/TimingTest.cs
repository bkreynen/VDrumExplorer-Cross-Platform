// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using VDrumExplorer.Utility;

namespace VDrumExplorer.Utility.Test
{
    public class TimingTest
    {
        [Test]
        public void LogTiming_ExecutesAction()
        {
            var executed = false;
            Timing.LogTiming(NullLogger.Instance, "test", () => executed = true);
            Assert.IsTrue(executed);
        }

        [Test]
        public void LogTiming_DoesNotThrowForNoOpLogger()
        {
            // NullLogger.Instance is a non-null no-op ILogger, not a null reference.
            // This test verifies the no-op logger does not throw, distinct from a null logger.
            Assert.DoesNotThrow(() => Timing.LogTiming(NullLogger.Instance, "test", () => { }));
        }

        [Test]
        public void LogTiming_NullLogger_ThrowsArgumentNullException()
        {
            // Timing.LogTiming does not guard against null; passing null throws via LoggerExtensions.Log.
            // In .NET 8+ LoggerExtensions does ThrowIfNull(logger) -> ArgumentNullException(paramName: "logger").
            var ex = Assert.Throws<ArgumentNullException>(() => Timing.LogTiming(null!, "test", () => { }));
            Assert.That(ex!.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void LogTiming_Generic_NullLogger_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => Timing.LogTiming<int>(null!, "test", () => 42));
            Assert.That(ex!.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void LogTiming_ActionThrows_PropagatesException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Timing.LogTiming(NullLogger.Instance, "test", () => throw new InvalidOperationException("boom")));
        }

        [Test]
        public void LogTiming_Generic_ReturnsFunctionResult()
        {
            var result = Timing.LogTiming(NullLogger.Instance, "test", () => 42);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void LogTiming_Generic_ReturnsReferenceTypeResult()
        {
            var result = Timing.LogTiming(NullLogger.Instance, "test", () => "hello");
            Assert.AreEqual("hello", result);
        }

        [Test]
        public void LogTiming_Generic_FunctionThrows_PropagatesException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Timing.LogTiming<int>(NullLogger.Instance, "test", () => throw new InvalidOperationException("boom")));
        }

        [Test]
        public void LogTiming_ActionOverload_LogsDebugMessageWithDescriptionAndElapsedTime()
        {
            var logger = new RecordingLogger();
            var executed = false;

            Timing.LogTiming(logger, "test", () => executed = true);

            Assert.IsTrue(executed);
            Assert.AreEqual(1, logger.Entries.Count);
            var entry = logger.Entries[0];
            Assert.AreEqual(LogLevel.Debug, entry.Level);
            Assert.That(entry.Message, Does.Contain("test"));
            // Timing.cs logs as $"{description} in {elapsed}ms" — verify elapsed-time part.
            Assert.That(entry.Message, Does.Contain("ms"));
            Assert.That(entry.Message, Does.Contain("in"));
        }

        [Test]
        public void LogTiming_GenericOverload_LogsDebugMessageWithDescriptionAndElapsedTime()
        {
            var logger = new RecordingLogger();

            var result = Timing.LogTiming(logger, "test", () => 42);

            Assert.AreEqual(42, result);
            Assert.AreEqual(1, logger.Entries.Count);
            var entry = logger.Entries[0];
            Assert.AreEqual(LogLevel.Debug, entry.Level);
            Assert.That(entry.Message, Does.Contain("test"));
            Assert.That(entry.Message, Does.Contain("ms"));
            Assert.That(entry.Message, Does.Contain("in"));
        }

        [Test]
        public void DebugConsoleLogTiming_ExecutesAction()
        {
            var executed = false;
            Timing.DebugConsoleLogTiming("test", () => executed = true);
            Assert.IsTrue(executed);
        }

        [Test]
        public void DebugConsoleLogTiming_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Timing.DebugConsoleLogTiming("test", () => { }));
        }

        [Test]
        public void DebugConsoleLogTiming_ActionThrows_PropagatesException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Timing.DebugConsoleLogTiming("test", () => throw new InvalidOperationException("boom")));
        }

        [Test]
        public void DebugConsoleLogTiming_Generic_ReturnsFunctionResult()
        {
            var result = Timing.DebugConsoleLogTiming("test", () => 42);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void DebugConsoleLogTiming_Generic_ReturnsReferenceTypeResult()
        {
            var result = Timing.DebugConsoleLogTiming("test", () => "hello");
            Assert.AreEqual("hello", result);
        }

        [Test]
        public void DebugConsoleLogTiming_Generic_FunctionThrows_PropagatesException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Timing.DebugConsoleLogTiming<int>("test", () => throw new InvalidOperationException("boom")));
        }

        /// <summary>
        /// Minimal <see cref="ILogger"/> that captures <see cref="ILogger.Log{TState}"/> calls
        /// for assertion. Avoids taking dependency on Microsoft.Extensions.Logging.Testing.
        /// </summary>
        private sealed class RecordingLogger : ILogger
        {
            public List<LogEntry> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                Entries.Add(new LogEntry(logLevel, message));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }

        private sealed record LogEntry(LogLevel Level, string Message);
    }
}

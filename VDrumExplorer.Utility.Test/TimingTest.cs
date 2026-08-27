// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
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
        public void LogTiming_DoesNotThrowForNullLoggerInstance()
        {
            Assert.DoesNotThrow(() => Timing.LogTiming(NullLogger.Instance, "test", () => { }));
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
    }
}

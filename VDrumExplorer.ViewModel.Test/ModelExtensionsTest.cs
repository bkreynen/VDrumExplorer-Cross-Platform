// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using VDrumExplorer.Model;
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class ModelExtensionsTest
    {
        private static ModuleSchema TD27Schema => ModuleSchema.KnownSchemas[ModuleIdentifier.TD27].Value;

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void ValidateKitNumber_ValidNumber_ReturnsValue(int kitNumber)
        {
            var schema = TD27Schema;
            Assert.Equal(kitNumber, schema.ValidateKitNumber(kitNumber));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void ValidateKitNumber_TooLow_Throws(int kitNumber)
        {
            var schema = TD27Schema;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateKitNumber(kitNumber));
        }

        [Theory]
        [InlineData(int.MaxValue)]
        public void ValidateKitNumber_TooHigh_Throws(int kitNumber)
        {
            var schema = TD27Schema;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateKitNumber(kitNumber));
        }

        [Fact]
        public void ValidateKitNumber_JustAboveMax_Throws()
        {
            var schema = TD27Schema;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateKitNumber(schema.Kits + 1));
        }

        [Fact]
        public void ValidateKitNumber_AtMax_ReturnsValue()
        {
            var schema = TD27Schema;
            Assert.Equal(schema.Kits, schema.ValidateKitNumber(schema.Kits));
        }

        [Fact]
        public void ValidateKitNumber_NullSchema_Throws()
        {
            ModuleSchema? schema = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateKitNumber(1));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ValidateUserSampleNumber_ValidNumber_ReturnsValue(int sampleNumber)
        {
            var schema = TD27Schema;
            // Only test if the schema actually has user samples; otherwise this will throw.
            if (schema.UserSamples > 0)
            {
                Assert.Equal(sampleNumber, schema.ValidateUserSampleNumber(sampleNumber));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateUserSampleNumber_TooLow_Throws(int sampleNumber)
        {
            var schema = TD27Schema;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateUserSampleNumber(sampleNumber));
        }

        [Fact]
        public void ValidateUserSampleNumber_AboveMax_Throws()
        {
            var schema = TD27Schema;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateUserSampleNumber(schema.UserSamples + 1));
        }

        [Fact]
        public void ValidateUserSampleNumber_AtMax_ReturnsValue()
        {
            var schema = TD27Schema;
            if (schema.UserSamples > 0)
            {
                Assert.Equal(schema.UserSamples, schema.ValidateUserSampleNumber(schema.UserSamples));
            }
        }

        [Fact]
        public void ValidateUserSampleNumber_NullSchema_Throws()
        {
            ModuleSchema? schema = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => schema.ValidateUserSampleNumber(1));
        }
    }
}

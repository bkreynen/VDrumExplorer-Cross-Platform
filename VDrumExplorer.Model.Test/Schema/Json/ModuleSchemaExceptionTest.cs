// Copyright 2024 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System;
using VDrumExplorer.Model.Schema.Json;

namespace VDrumExplorer.Model.Test.Schema.Json
{
    public class ModuleSchemaExceptionTest
    {
        [Test]
        public void Constructor_WithMessage_SetsMessageProperty()
        {
            var ex = new ModuleSchemaException("Something went wrong");
            Assert.AreEqual("Something went wrong", ex.Message);
        }

        [Test]
        public void Constructor_WithEmptyMessage_SetsEmptyMessage()
        {
            var ex = new ModuleSchemaException("");
            Assert.AreEqual("", ex.Message);
        }

        [Test]
        public void ExtendsException()
        {
            var ex = new ModuleSchemaException("test");
            Assert.IsInstanceOf<Exception>(ex);
        }

        [Test]
        public void CanBeThrownAndCaught()
        {
            Assert.Throws<ModuleSchemaException>(() =>
            {
                throw new ModuleSchemaException("test error");
            });
        }

        [Test]
        public void Message_IsAccessibleViaBaseClass()
        {
            var ex = new ModuleSchemaException("base message test");
            Exception baseEx = ex;
            Assert.AreEqual("base message test", baseEx.Message);
        }
    }
}

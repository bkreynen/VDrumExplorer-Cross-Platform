// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Collections.Generic;

namespace VDrumExplorer.Model.Test
{
    public class SchemaVariablesTest
    {
        [Test]
        public void Empty_ReplaceReturnsOriginalText()
        {
            var result = SchemaVariables.Empty.Replace("hello world");
            Assert.AreEqual("hello world", result);
        }

        [Test]
        public void Empty_ReplaceReturnsOriginalTextEvenWithBraces()
        {
            // Empty has no parent, so Replace returns the text as-is even if it contains braces.
            var result = SchemaVariables.Empty.Replace("hello {world}");
            Assert.AreEqual("hello {world}", result);
        }

        [Test]
        public void WithVariable_NullKey_ReturnsSameInstance()
        {
            var vars = SchemaVariables.Empty.WithVariable(null, "value", "{key}");
            Assert.AreSame(SchemaVariables.Empty, vars);
        }

        [Test]
        public void WithVariable_ReplacesTemplateInText()
        {
            var vars = SchemaVariables.Empty.WithVariable("kit", "1", "{kit}");
            var result = vars.Replace("Kit[{kit}]");
            Assert.AreEqual("Kit[1]", result);
        }

        [Test]
        public void WithVariable_DoesNotReplaceOtherTemplates()
        {
            var vars = SchemaVariables.Empty.WithVariable("kit", "1", "{kit}");
            var result = vars.Replace("Kit[{kit}] Trigger[{trigger}]");
            Assert.AreEqual("Kit[1] Trigger[{trigger}]", result);
        }

        [Test]
        public void WithVariables_NullDictionary_ReturnsSameInstance()
        {
            var vars = SchemaVariables.Empty.WithVariables(null);
            Assert.AreSame(SchemaVariables.Empty, vars);
        }

        [Test]
        public void WithVariables_EmptyDictionary_ReturnsSameInstance()
        {
            var vars = SchemaVariables.Empty.WithVariables(new Dictionary<string, string>());
            Assert.AreSame(SchemaVariables.Empty, vars);
        }

        [Test]
        public void WithVariables_ReplacesAllVariables()
        {
            var dict = new Dictionary<string, string>
            {
                { "kit", "5" },
                { "trigger", "3" }
            };
            var vars = SchemaVariables.Empty.WithVariables(dict);
            var result = vars.Replace("Kit[{kit}]/Trigger[{trigger}]");
            Assert.AreEqual("Kit[5]/Trigger[3]", result);
        }

        [Test]
        public void WithVariables_ChainedWithWithVariable()
        {
            var vars = SchemaVariables.Empty
                .WithVariable("kit", "1", "{kit}")
                .WithVariable("trigger", "2", "{trigger}");
            var result = vars.Replace("Kit[{kit}]/Trigger[{trigger}]");
            Assert.AreEqual("Kit[1]/Trigger[2]", result);
        }

        [Test]
        public void Replace_NoBraces_ReturnsOriginalText()
        {
            var vars = SchemaVariables.Empty.WithVariable("kit", "1", "{kit}");
            var result = vars.Replace("no variables here");
            Assert.AreEqual("no variables here", result);
        }

        [Test]
        public void Replace_MultipleOccurrencesOfSameVariable()
        {
            var vars = SchemaVariables.Empty.WithVariable("kit", "5", "{kit}");
            var result = vars.Replace("Kit[{kit}] and Kit[{kit}] again");
            Assert.AreEqual("Kit[5] and Kit[5] again", result);
        }

        [Test]
        public void Replace_WithOverlappingVariables()
        {
            // Variables are replaced from child to parent. The most recently added
            // variable is replaced first.
            var vars = SchemaVariables.Empty
                .WithVariable("kit", "1", "{kit}")
                .WithVariable("trigger", "2", "{trigger}");
            var result = vars.Replace("{kit}/{trigger}");
            Assert.AreEqual("1/2", result);
        }

        [Test]
        public void Replace_TextWithoutAnyMatchingVariables_ReturnsOriginalText()
        {
            var vars = SchemaVariables.Empty.WithVariable("kit", "1", "{kit}");
            var result = vars.Replace("Trigger[{trigger}]");
            Assert.AreEqual("Trigger[{trigger}]", result);
        }
    }
}

// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NUnit.Framework;
using System.Linq;

namespace VDrumExplorer.Model.Test
{
    public class InstrumentAndGroupTest
    {
        private Module module;

        [SetUp]
        public void Setup()
        {
            module = TestData.LoadTD27();
        }

        [Test]
        public void PresetInstruments_IsNonEmpty()
        {
            Assert.IsTrue(module.Schema.PresetInstruments.Count > 0);
        }

        [Test]
        public void PresetInstruments_HaveSequentialIdsStartingFromZero()
        {
            var instruments = module.Schema.PresetInstruments;
            for (int i = 0; i < instruments.Count; i++)
            {
                Assert.AreEqual(i, instruments[i].Id, $"Preset instrument at index {i} has ID {instruments[i].Id}");
            }
        }

        [Test]
        public void PresetInstruments_HaveNonEmptyNames()
        {
            foreach (var instrument in module.Schema.PresetInstruments)
            {
                Assert.IsFalse(string.IsNullOrEmpty(instrument.Name), $"Instrument {instrument.Id} has empty name");
            }
        }

        [Test]
        public void PresetInstruments_FirstInstrumentIsOff()
        {
            // The first instrument group in TD-27 is "Off" with a single instrument with ID 0.
            var first = module.Schema.PresetInstruments[0];
            Assert.AreEqual(0, first.Id);
            Assert.AreEqual("Off", first.Name);
        }

        [Test]
        public void UserSampleInstruments_IsNonEmpty()
        {
            Assert.IsTrue(module.Schema.UserSampleInstruments.Count > 0);
        }

        [Test]
        public void UserSampleInstruments_HaveCorrectCount()
        {
            // TD-27 has 500 user samples.
            Assert.AreEqual(500, module.Schema.UserSampleInstruments.Count);
        }

        [Test]
        public void UserSampleInstruments_HaveSequentialIdsStartingFromZero()
        {
            var instruments = module.Schema.UserSampleInstruments;
            for (int i = 0; i < instruments.Count; i++)
            {
                Assert.AreEqual(i, instruments[i].Id, $"User sample instrument at index {i} has ID {instruments[i].Id}");
            }
        }

        [Test]
        public void UserSampleInstruments_HaveUserSampleNames()
        {
            var first = module.Schema.UserSampleInstruments[0];
            Assert.AreEqual("User sample 1", first.Name);
            var second = module.Schema.UserSampleInstruments[1];
            Assert.AreEqual("User sample 2", second.Name);
        }

        [Test]
        public void InstrumentGroups_AreNonEmpty()
        {
            Assert.IsTrue(module.Schema.InstrumentGroups.Count > 0);
        }

        [Test]
        public void InstrumentGroups_EachHasDescription()
        {
            foreach (var group in module.Schema.InstrumentGroups)
            {
                Assert.IsFalse(string.IsNullOrEmpty(group.Description), $"Group at index {group.Index} has empty description");
            }
        }

        [Test]
        public void InstrumentGroups_EachHasCorrectIndex()
        {
            var groups = module.Schema.InstrumentGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                Assert.AreEqual(i, groups[i].Index, $"Group at position {i} has index {groups[i].Index}");
            }
        }

        [Test]
        public void InstrumentGroups_PresetGroupsHavePresetBank()
        {
            var presetGroups = module.Schema.InstrumentGroups.Where(g => g.Preset);
            foreach (var group in presetGroups)
            {
                Assert.AreEqual(InstrumentBank.Preset, group.Bank, $"Preset group '{group.Description}' has wrong bank");
            }
        }

        [Test]
        public void InstrumentGroups_UserSampleGroupHasUserSamplesBank()
        {
            var userSampleGroups = module.Schema.InstrumentGroups.Where(g => !g.Preset);
            foreach (var group in userSampleGroups)
            {
                Assert.AreEqual(InstrumentBank.UserSamples, group.Bank, $"User sample group '{group.Description}' has wrong bank");
            }
        }

        [Test]
        public void InstrumentGroups_EachHasVEditCategory()
        {
            foreach (var group in module.Schema.InstrumentGroups)
            {
                Assert.IsFalse(string.IsNullOrEmpty(group.VEditCategory), $"Group '{group.Description}' has empty VEditCategory");
            }
        }

        [Test]
        public void Instrument_ToString_ReturnsName()
        {
            var instrument = module.Schema.PresetInstruments[0];
            Assert.AreEqual(instrument.Name, instrument.ToString());
        }

        [Test]
        public void Instrument_Group_MatchesItsGroup()
        {
            foreach (var group in module.Schema.InstrumentGroups)
            {
                foreach (var instrument in group.Instruments)
                {
                    Assert.AreSame(group, instrument.Group, $"Instrument {instrument.Id} does not reference its group");
                }
            }
        }

        [Test]
        public void Instrument_Bank_IsCorrectForPresetInstruments()
        {
            foreach (var instrument in module.Schema.PresetInstruments)
            {
                Assert.AreEqual(InstrumentBank.Preset, instrument.Bank, $"Preset instrument {instrument.Id} has wrong bank");
            }
        }

        [Test]
        public void Instrument_Bank_IsCorrectForUserSampleInstruments()
        {
            foreach (var instrument in module.Schema.UserSampleInstruments)
            {
                Assert.AreEqual(InstrumentBank.UserSamples, instrument.Bank, $"User sample instrument {instrument.Id} has wrong bank");
            }
        }

        [Test]
        public void InstrumentBank_Preset_IsZero()
        {
            Assert.AreEqual(0, (int)InstrumentBank.Preset);
        }

        [Test]
        public void InstrumentBank_UserSamples_IsOne()
        {
            Assert.AreEqual(1, (int)InstrumentBank.UserSamples);
        }

        [Test]
        public void InstrumentGroup_ToString_ReturnsDescription()
        {
            var group = module.Schema.InstrumentGroups[0];
            Assert.AreEqual(group.Description, group.ToString());
        }

        [Test]
        public void InstrumentGroups_LastGroupIsUserSamples()
        {
            // The last group should be the user samples group (since TD-27 has user samples).
            var lastGroup = module.Schema.InstrumentGroups.Last();
            Assert.IsFalse(lastGroup.Preset);
            Assert.AreEqual("User samples", lastGroup.Description);
        }
    }
}

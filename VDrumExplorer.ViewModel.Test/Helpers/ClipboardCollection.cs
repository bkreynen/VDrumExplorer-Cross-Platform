using Xunit;

namespace VDrumExplorer.ViewModel.Test.Helpers
{
    [CollectionDefinition("Clipboard", DisableParallelization = true)]
    public class ClipboardCollectionDefinition { }

    [CollectionDefinition("MidiDevices", DisableParallelization = true)]
    public class MidiDevicesCollectionDefinition { }
}

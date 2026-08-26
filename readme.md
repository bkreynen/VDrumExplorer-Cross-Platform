# Drums (VDrumExplorer for Linux)

This is a fork of [Jon Skeet's](https://github.com/jskeet) demo code
repository, focused on the **VDrumExplorer** project — a tool for
exploring and editing Roland V-Drum settings.

The original repository contained many unrelated demo projects from
talks and blog posts. This fork removes everything except VDrumExplorer
and ports the GUI from WPF to [Avalonia UI](https://avaloniaui.net/)
so it runs cross-platform (Linux, macOS, and Windows).

## ⚠️ Status

**This code should be considered an alpha.** It is not guaranteed to
work correctly. It has only been tested on Linux (Pop!_OS) with a
Roland TD-17 (version 2). Other modules, operating systems, or
configurations may not work.

## Key changes from the upstream

- **Avalonia UI** — the legacy WPF GUI (`VDrumExplorer.Gui`) has been
  removed. The app now uses the Avalonia project
  (`VDrumExplorer.Gui.Avalonia`) targeting `net10.0`.
- **Cross-platform MIDI** — uses the managed-midi backend
  (`VDrumExplorer.Midi.ManagedMidi`) with ALSA on Linux.
- **UX improvements** — hotkey copy/paste for kits and instruments,
  undo/redo support, multi-kit copy dialog, and kit switching via MIDI
  Program Change when playing notes.

## Building

See [`run-avalonia.sh`](run-avalonia.sh) for a build-and-run script.

All code is released under the Apache License 2.0, as found in the
[LICENSE.txt](LICENSE.txt) file.

## Third-party licenses

The [managed-midi](https://github.com/atsushieno/managed-midi) library
is licensed under the MIT License. See
[LICENSE.ManagedMidi.txt](LICENSE.ManagedMidi.txt) for details.

The [Google.Protobuf](https://github.com/protocolbuffers/protobuf)
library is licensed under the BSD 2.0 license. See
[LICENSE.Protobuf.txt](LICENSE.Protobuf.txt) for details.

The [byte-base64](https://github.com/euo/byte-base64) library (used in
the Blazor app) is licensed under the MIT License. See
[LICENCE.byte-base64.txt](LICENCE.byte-base64.txt) for details.

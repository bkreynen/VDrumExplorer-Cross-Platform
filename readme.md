# Drums (VDrumExplorer for Linux)

This is a fork of [Jon Skeet's](https://github.com/jskeet) demo code
repository, focused on the **VDrumExplorer** project — a tool for
exploring and editing Roland V-Drum settings. The overwhelming
majority of this codebase — the data model, schema system, MIDI
communication layer, console application, Blazor web app, and the
original WPF GUI — was written by Jon Skeet. This fork adds an
[Avalonia UI](https://avaloniaui.net/) port and several UX
improvements on top of that foundation.

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

- **Avalonia UI port** — the WPF GUI (`VDrumExplorer.Gui`) has been
  ported to a new Avalonia project (`VDrumExplorer.Gui.Avalonia`)
  targeting `net10.0`.
- **Cross-platform MIDI** — uses the managed-midi backend
  (`VDrumExplorer.Midi.ManagedMidi`) with ALSA on Linux.
- **UX improvements** — hotkey copy/paste for kits and instruments,
  undo/redo support, multi-kit copy dialog, and kit switching via MIDI
  Program Change when playing notes.

## Building

See [`run-avalonia.sh`](run-avalonia.sh) for a build-and-run script.

All code is released under the Apache License 2.0, as found in the
[LICENSE](LICENSE) file.

## Third-party licenses

The [managed-midi](https://github.com/atsushieno/managed-midi) library
is licensed under the MIT License. See
[LICENSE.ManagedMidi.txt](LICENSE.ManagedMidi.txt) for details.

The [Google.Protobuf](https://github.com/protocolbuffers/protobuf)
library is licensed under the BSD 2.0 license. See
[LICENSE.Protobuf.txt](LICENSE.Protobuf.txt) for details.

The [NAudio](https://github.com/naudio/NAudio) library is licensed
under the Microsoft Public License (Ms-PL). See
[LICENSE.NAudio.txt](LICENSE.NAudio.txt) for details.

The [byte-base64](https://github.com/euo/byte-base64) library (used in
the Blazor app) is licensed under the MIT License. See
[LICENCE.byte-base64.txt](LICENCE.byte-base64.txt) for details.

## Credits & Attribution

The overwhelming majority of this codebase was authored by
[Jon Skeet](https://github.com/jskeet), including the data model,
schema system, MIDI communication layer, console application, Blazor
web app, and the original WPF GUI. This fork builds on that foundation
and is grateful for the original work.

- **Original repository:** [jskeet/DemoCode (Drums)](https://github.com/jskeet/DemoCode/tree/master/Drums)
- **Jon Skeet's V-Drum blog posts:** [codeblog.jonskeet.uk/category/v-drums](https://codeblog.jonskeet.uk/category/v-drums/)
- **Original documentation:** [jskeet.github.io/DemoCode/Drums](https://jskeet.github.io/DemoCode/Drums/)

This project is licensed under the Apache License, Version 2.0 — the
same license as the original repository. Files modified from the
upstream carry prominent change notices per Section 4(b) of the
license. See the [NOTICE](NOTICE) file for full attribution details.

### What this fork changed/added

- **Avalonia UI port** — the WPF GUI (`VDrumExplorer.Gui`) was ported
  to a new cross-platform Avalonia project
  (`VDrumExplorer.Gui.Avalonia`).
- **Cross-platform MIDI fixes** — the managed-midi backend
  (`VDrumExplorer.Midi.ManagedMidi`) was updated for cross-platform
  support (ALSA on Linux).
- **Undo/redo** — edit actions in the GUI can be undone and redone.
- **Multi-kit copy** — a dialog for copying settings across multiple
  kits at once.
- **Clipboard copy/paste** — hotkey copy/paste for kits and
  instruments.
- **Kit-switching via MIDI** — kit switching through MIDI Program
  Change messages when playing notes.

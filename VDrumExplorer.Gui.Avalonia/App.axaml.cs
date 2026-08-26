// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using VDrumExplorer.Gui.Avalonia.Audio;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.Gui.Avalonia.ViewServices;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Logging;

namespace VDrumExplorer.Gui.Avalonia;

public class App : Application
{
    private DeviceViewModel? deviceViewModel;
    private LogViewModel? logViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Set up the MIDI manager (cross-platform managed-midi implementation).
        MidiDevices.Manager = new Midi.ManagedMidi.MidiManager();

        // Create the stub audio device manager (NAudio is Windows-only).
        var audioDeviceManager = new StubAudioDeviceManager();

        deviceViewModel = new DeviceViewModel();
        logViewModel = new LogViewModel();

        // Set up unhandled exception logging.
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                logViewModel.Logger.LogError(ex, "Unhandled exception");
            }
        };

        var viewServices = new AvaloniaViewServices();

        var viewModel = new ExplorerHomeViewModel(viewServices, logViewModel, deviceViewModel, audioDeviceManager);
        var mainWindow = new ExplorerHome { DataContext = viewModel };
        viewServices.MainWindow = mainWindow;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;
        }

        mainWindow.Show();
        logViewModel.LogVersion(GetType());

        // Detect the module asynchronously (fire and forget, like the WPF version).
        ThreadPool.QueueUserWorkItem(async _ =>
        {
            try
            {
                await deviceViewModel.DetectModule(logViewModel.Logger);
            }
            catch (Exception ex)
            {
                logViewModel.Logger.LogError(ex, "Error detecting module");
            }
        });

        base.OnFrameworkInitializationCompleted();
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
    }
}

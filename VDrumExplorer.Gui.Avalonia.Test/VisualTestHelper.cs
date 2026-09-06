// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Shared helpers for the visual acceptance tests: renders windows headlessly, checks
/// that a render actually produced visible content, and compares renders against
/// committed baseline screenshots.
/// </summary>
public static class VisualTestHelper
{
    /// <summary>Per-channel tolerance applied before a pixel is considered mismatched,
    /// to absorb minor anti-aliasing differences.</summary>
    public const int PerChannelTolerance = 15;

    /// <summary>Maximum percentage of mismatched pixels allowed for the baseline check.</summary>
    public const double MaxMismatchPercentage = 15.0;

    /// <summary>The rendered window must be at least this fraction of opaque pixels.</summary>
    public const double MinimumOpaqueRatio = 0.95;

    /// <summary>At most this fraction of pixels may share the single most common color,
    /// otherwise the render is considered a blank/uniform surface.</summary>
    public const double MaximumBackgroundRatio = 0.995;

    /// <summary>
    /// Creates, shows and renders a window to a bitmap. The window is shown, laid out,
    /// rendered, then closed. The caller owns the returned bitmap.
    /// </summary>
    public static RenderTargetBitmap RenderWindow(Window window)
    {
        try
        {
            window.Show();

            // Force a layout pass so that Bounds and child visuals are up to date.
            window.UpdateLayout();

            var pixelSize = new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height);
            Assert.True(pixelSize.Width > 0 && pixelSize.Height > 0, "Window has non-zero size.");

            var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            bitmap.Render(window);
            return bitmap;
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Asserts that the rendered bitmap actually contains visible window content:
    /// it must be non-empty, mostly opaque and not a uniform surface.
    /// </summary>
    public static void AssertProducesVisualResult(RenderTargetBitmap bitmap, string screenName)
    {
        var pixelSize = bitmap.PixelSize;
        Assert.True(pixelSize.Width > 0 && pixelSize.Height > 0, $"{screenName}: Rendered bitmap has no pixels.");

        byte[] pixels = GetPixelBytes(bitmap);

        // Fail fast on an empty PNG before looking at pixel content.
        Assert.True(pixels.Length > 0, $"{screenName}: Rendered bitmap has no pixel data.");

        bool hasVisualContent = HasVisualContent(pixels, out string details);

        // The current render is saved to a temp file so failures can be inspected manually.
        string debugPath = SaveDebugCopy(bitmap, screenName);
        Assert.True(
            hasVisualContent,
            $"{screenName}: Rendered window has no visible content (blank/transparent). {details} " +
            $"Render saved for debugging to: {debugPath}");
    }

    /// <summary>
    /// Compares the rendered bitmap against a committed baseline screenshot. If no baseline
    /// exists yet (first run), the current render is saved as the new baseline and the test
    /// passes (bootstrap mode). The comparison only runs on Linux, as the committed baselines
    /// were generated there and font shaping/rendering may differ on other operating systems.
    /// </summary>
    public static void AssertMatchesBaseline(RenderTargetBitmap bitmap, string baselineFileName)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        string baselinePath = Path.Combine(GetBaselineDirectory(), baselineFileName);

        if (!File.Exists(baselinePath))
        {
            // Bootstrap mode: no committed baseline yet, so store this render as the
            // baseline and pass. Commit the generated PNG to the repository afterwards.
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            using (var stream = File.Create(baselinePath))
            {
                bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            }
            return;
        }

        using var baseline = new Bitmap(baselinePath);
        Assert.Equal(
            new PixelSize(baseline.PixelSize.Width, baseline.PixelSize.Height),
            new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height));

        byte[] currentPixels = GetPixelBytes(bitmap);
        byte[] baselinePixels = GetPixelBytes(baseline);
        Assert.Equal(baselinePixels.Length, currentPixels.Length);

        double mismatchPercentage = CalculateMismatchPercentage(
            currentPixels, baselinePixels, PerChannelTolerance);

        string debugPath = SaveDebugCopy(bitmap, baselineFileName);
        Assert.True(
            mismatchPercentage <= MaxMismatchPercentage,
            $"Rendered screen does not match baseline '{baselinePath}': " +
            $"{mismatchPercentage:F2}% mismatched pixels (tolerance {PerChannelTolerance} per channel, " +
            $"maximum {MaxMismatchPercentage:F0}%). " +
            $"Current render saved for debugging to: {debugPath}");
    }

    /// <summary>
    /// Returns the raw BGRA8888 (premultiplied) pixel data of the given bitmap. The pixels are
    /// copied into a scratch <see cref="WriteableBitmap"/> framebuffer of a known format via
    /// <see cref="Bitmap.CopyPixels(ILockedFramebuffer)"/>, which transcodes the pixel/alpha
    /// format if needed - so both sides of a comparison (a freshly rendered target bitmap and
    /// a decoded baseline PNG) produce bytes in exactly the same layout.
    /// </summary>
    private static byte[] GetPixelBytes(Bitmap bitmap)
    {
        using var scratch = new WriteableBitmap(
            bitmap.PixelSize, bitmap.Dpi, PixelFormats.Bgra8888, AlphaFormat.Premul);
        using (var framebuffer = scratch.Lock())
        {
            bitmap.CopyPixels(framebuffer);
            var pixels = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
            Marshal.Copy(framebuffer.Address, pixels, 0, pixels.Length);
            return pixels;
        }
    }

    /// <summary>
    /// Determines whether the pixel data represents visible window content. A rendering
    /// that failed to produce output is either fully transparent (alpha channel empty)
    /// or a uniform solid surface; real window content is opaque and has pixel variety.
    /// </summary>
    /// <param name="pixels">Raw BGRA8888 pixel data.</param>
    /// <param name="details">A human-readable summary of the analyzed pixels.</param>
    /// <returns>True if the data looks like a genuinely rendered window.</returns>
    private static bool HasVisualContent(byte[] pixels, out string details)
    {
        int total = pixels.Length / 4;
        if (total == 0)
        {
            details = "No pixels present.";
            return false;
        }

        int opaque = 0;
        var colorCounts = new Dictionary<long, int>();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] > 0)
            {
                opaque++;
            }

            long key = pixels[i] | (pixels[i + 1] << 8) | (pixels[i + 2] << 16) | (pixels[i + 3] << 24);
            colorCounts[key] = colorCounts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        int mostCommonCount = colorCounts.Values.Max();
        double opaqueRatio = (double)opaque / total;
        double backgroundRatio = (double)mostCommonCount / total;
        details = $"Analysis of {total} pixels: opaque={opaqueRatio:P1} " +
                  $"(minimum {MinimumOpaqueRatio:P1}), " +
                  $"most-common-color coverage={backgroundRatio:P2} (maximum {MaximumBackgroundRatio:P2}), " +
                  $"distinctColors={colorCounts.Count}.";

        return opaqueRatio >= MinimumOpaqueRatio && backgroundRatio <= MaximumBackgroundRatio;
    }

    /// <summary>
    /// Computes the percentage of pixels where any RGBA channel differs from the baseline
    /// by more than the given tolerance. (Channel order is irrelevant as both arrays use
    /// the same format.)
    /// </summary>
    private static double CalculateMismatchPercentage(byte[] currentPixels, byte[] baselinePixels, int tolerance)
    {
        int total = currentPixels.Length / 4;
        int mismatched = 0;
        for (int i = 0; i < currentPixels.Length; i += 4)
        {
            int da = Math.Abs(currentPixels[i + 3] - baselinePixels[i + 3]);
            if (da > tolerance)
            {
                mismatched++;
                continue;
            }
            // Compare color channels while both pixels are opaque; fully transparent pixels
            // have undefined color values on either side, so only alpha matters for them.
            if (currentPixels[i + 3] > 0 && baselinePixels[i + 3] > 0)
            {
                int dr = Math.Abs(currentPixels[i] - baselinePixels[i]);
                int dg = Math.Abs(currentPixels[i + 1] - baselinePixels[i + 1]);
                int db = Math.Abs(currentPixels[i + 2] - baselinePixels[i + 2]);
                if (dr > tolerance || dg > tolerance || db > tolerance)
                {
                    mismatched++;
                }
            }
        }
        return total == 0 ? 100.0 : (double)mismatched / total * 100.0;
    }

    /// <summary>Saves a copy of the rendered bitmap to a temp file for failure diagnosis.</summary>
    /// <param name="bitmap">The rendered bitmap to save.</param>
    /// <param name="screenName">A name identifying the screen, used for the debug file name.</param>
    /// <returns>The path of the saved file.</returns>
    private static string SaveDebugCopy(RenderTargetBitmap bitmap, string screenName)
    {
        string directory = Path.Combine(Path.GetTempPath(), "VDrumExplorer.Gui.Avalonia.Test");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, screenName);
        using var stream = File.Create(path);
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return path;
    }

    /// <summary>
    /// Locates the baseline directory. The source project directory (found by walking up
    /// from the test output folder) is preferred, so bootstrap mode writes the baseline
    /// into the location that gets committed to the repository. When the source project
    /// directory cannot be found, the output-directory copy is used instead.
    /// </summary>
    private static string GetBaselineDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VDrumExplorer.Gui.Avalonia.Test.csproj")))
            {
                return Path.Combine(directory.FullName, "Baselines");
            }
            directory = directory.Parent!;
        }
        return Path.Combine(AppContext.BaseDirectory, "Baselines");
    }
}

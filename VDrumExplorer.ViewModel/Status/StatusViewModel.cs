// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

namespace VDrumExplorer.ViewModel.Status
{
    /// <summary>
    /// View model backing the live-region status line of a window, following the
    /// REAPER+OSARA announcement model (see docs/accessibility.md §5 and §6):
    /// every successful operation produces a polite status update, every failure
    /// produces an assertive error announcement. One instance per window feeds
    /// both the visible status bar and the screen-reader live regions.
    /// </summary>
    /// <remarks>
    /// Deliberately dependency-free (no Avalonia, no device types) so any window's
    /// view model can hold one, e.g. <c>ExplorerHomeViewModel</c> and
    /// <c>DataExplorerViewModel</c>.
    /// </remarks>
    public class StatusViewModel : ViewModelBase
    {
        private string message = "";
        private string errorMessage = "";

        /// <summary>
        /// The polite live-region text: the outcome of the most recent successful
        /// operation (e.g. "Switched to kit 12, Jazz"). Also shown in the visible
        /// status bar. Empty before the first announcement.
        /// </summary>
        public string Message
        {
            get => message;
            private set
            {
                // Always raise PropertyChanged, even when the text is unchanged: screen
                // readers may not re-announce identical text otherwise, and a repeated
                // identical operation outcome should still be announced.
                message = value;
                RaisePropertyChanged(nameof(Message));
            }
        }

        /// <summary>
        /// The assertive live-region text: the reason for the most recent failure
        /// (e.g. "Could not load kit: timeout"). Only populated on error, per the
        /// live-region policy in docs/accessibility.md §6. Empty outside failures.
        /// </summary>
        public string ErrorMessage
        {
            get => errorMessage;
            private set
            {
                // As with Message: re-announce even when the text is unchanged.
                errorMessage = value;
                RaisePropertyChanged(nameof(ErrorMessage));
            }
        }

        /// <summary>
        /// Announces a successful operation outcome (polite live-region update).
        /// </summary>
        /// <param name="message">Concise, identifier-first announcement text including the new value where applicable.</param>
        public void SetMessage(string message) =>
            Message = message ?? "";

        /// <summary>
        /// Announces a failure (assertive live-region update) with the reason it occurred.
        /// </summary>
        /// <param name="error">Reason for the failure, e.g. "Could not switch kit: device not connected".</param>
        public void SetError(string error) =>
            ErrorMessage = error ?? "";
    }
}

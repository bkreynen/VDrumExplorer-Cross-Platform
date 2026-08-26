// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using VDrumExplorer.Model;

namespace VDrumExplorer.ViewModel.Dialogs
{
    /// <summary>
    /// ViewModel for the "Copy multiple kits" dialog.
    /// Copies a range of kits (sourceFrom to sourceTo) to a destination range
    /// starting at destinationFrom.
    /// </summary>
    public class CopyKitsViewModel : ViewModelBase
    {
        private readonly Module module;
        public int KitCount { get; }

        private int sourceFrom = 1;
        public int SourceFrom
        {
            get => sourceFrom;
            set
            {
                if (SetProperty(ref sourceFrom, module.Schema.ValidateKitNumber(value)))
                {
                    ValidateRanges();
                }
            }
        }

        private int sourceTo = 1;
        public int SourceTo
        {
            get => sourceTo;
            set
            {
                if (SetProperty(ref sourceTo, module.Schema.ValidateKitNumber(value)))
                {
                    ValidateRanges();
                }
            }
        }

        private int destinationFrom = 1;
        public int DestinationFrom
        {
            get => destinationFrom;
            set
            {
                if (SetProperty(ref destinationFrom, module.Schema.ValidateKitNumber(value)))
                {
                    ValidateRanges();
                }
            }
        }

        /// <summary>
        /// The number of kits that will be copied, or 0 if the source range is invalid.
        /// </summary>
        public int CopyCount => sourceTo >= sourceFrom ? sourceTo - sourceFrom + 1 : 0;

        /// <summary>
        /// Whether the Copy button should be enabled.
        /// The source range must be valid (sourceTo >= sourceFrom), the destination
        /// range must fit within the module (destinationFrom + count - 1 &lt;= KitCount),
        /// and the source and destination ranges must not be identical (no-op).
        /// </summary>
        public bool CopyEnabled
        {
            get
            {
                if (sourceTo < sourceFrom)
                {
                    return false;
                }
                int count = CopyCount;
                int destEnd = destinationFrom + count - 1;
                if (destEnd > KitCount)
                {
                    return false;
                }
                // Don't allow copying to the same range (no-op).
                if (sourceFrom == destinationFrom)
                {
                    return false;
                }
                return true;
            }
        }

        public CopyKitsViewModel(Module module)
        {
            this.module = module;
            KitCount = module.Schema.Kits;
            sourceTo = KitCount;
        }

        private void ValidateRanges()
        {
            RaisePropertyChanged(nameof(CopyCount));
            RaisePropertyChanged(nameof(CopyEnabled));
        }
    }
}

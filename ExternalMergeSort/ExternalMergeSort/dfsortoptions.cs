using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExternalMergeSort
{
    public class dfsortoptions
    {
        /// <summary>
        /// file to sort
        /// </summary>
        public string InputFileName {get;set;}
        /// <summary>
        /// destingation file to save sorted result
        /// </summary>
        public string DestinationFileName { get; set; }
        /// <summary>
        /// working dir for computing
        /// </summary>
        public string WorkingDirectory { get; set; }
        /// <summary>
        /// Report progress after each lines
        /// </summary>
        public long ProgressAfter { get; set; }
        /// <summary>
        /// chunk files using this max size for part
        /// </summary>
        public long FileSizeDivider { get; set; }
        /// <summary>
        /// egzample "split{0:d5}.dat"; - it will be format with parts
        /// </summary>
        public string SplitFilePattern { get; set; }
        /// <summary>
        /// egzample "split*.dat" - search for all parts
        /// </summary>
        public string SplitFilePatternSearch { get; set; }
        /// <summary>
        /// "split", "sorted"
        /// </summary>
        public string SortedFilePattern { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExternalMergeSort
{
    public class sorter : ISorter
    {
        private dfsortoptions _opts;
        public sorter(dfsortoptions options)
        {
            _opts = options;
        }

        public void SortChunks()
        {
            foreach (string path in Directory.GetFiles(_opts.WorkingDirectory, _opts.SplitFilePatternSearch))
            {
                Console.Write("{0}     \r", path);

                // Read all lines into an array
                string[] contents = File.ReadAllLines(path);
                // Sort the in-memory array
                Array.Sort(contents);
                // Create the 'sorted' filename
                string newpath = path.Replace("split", "sorted");
                // Write it
                File.WriteAllLines(newpath, contents);
                // Delete the unsorted chunk
                File.Delete(path);
                // Free the in-memory sorted array
                contents = null;
            }
        }
    }
}

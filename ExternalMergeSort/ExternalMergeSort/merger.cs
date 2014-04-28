using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExternalMergeSort
{
    public class merger : IMerger
    {
        private dfsortoptions _opts;
        public merger(dfsortoptions options)
        {
            _opts = options;
        }

        void LoadQueue(Queue<string> queue, StreamReader file, int records)
        {
            for (int i = 0; i < records; i++)
            {
                if (file.Peek() < 0) break;
                queue.Enqueue(file.ReadLine());
            }
        }

        public void MergeChunks()
        {
            string[] paths = Directory.GetFiles(_opts.WorkingDirectory, _opts.SplitFilePatternSearch);
            int chunks = paths.Length; // Number of chunks
            int recordsize = 100; // estimated record size
            int records = 10000000; // estimated total # records
            int maxusage = 500000000; // max memory usage
            int buffersize = maxusage / chunks; // size in bytes of each buffer
            double recordoverhead = 7.5; // The overhead of using Queue<>
            int bufferlen = (int)(buffersize / recordsize / recordoverhead); // number of records in each buffer

            // Open the files
            StreamReader[] readers = new StreamReader[chunks];
            for (int i = 0; i < chunks; i++)
                readers[i] = new StreamReader(paths[i]);

            // Make the queues
            Queue<string>[] queues = new Queue<string>[chunks];
            for (int i = 0; i < chunks; i++)
                queues[i] = new Queue<string>(bufferlen);

            // Load the queues
            //W("Priming the queues");
            for (int i = 0; i < chunks; i++)
                LoadQueue(queues[i], readers[i], bufferlen);
            //W("Priming the queues complete");

            // Merge!
            StreamWriter sw = new StreamWriter(_opts.DestinationFileName);
            bool done = false;
            int lowest_index, j, progress = 0;
            string lowest_value;
            while (!done)
            {
                // Report the progress
                //if (++progress % rowDivider == 0)
                //    Console.Write("{0:f2}%   \r",
                //      100.0 * progress / records);

                // Find the chunk with the lowest value
                lowest_index = -1;
                lowest_value = String.Empty;
                for (j = 0; j < chunks; j++)
                {
                    if (queues[j] != null)
                    {
                        if (lowest_index < 0 || String.CompareOrdinal(queues[j].Peek(), lowest_value) < 0)
                        {
                            lowest_index = j;
                            lowest_value = queues[j].Peek();
                        }
                    }
                }

                // Was nothing found in any queue? We must be done then.
                if (lowest_index == -1) { done = true; break; }

                // Output it
                sw.WriteLine(lowest_value);

                // Remove from queue
                queues[lowest_index].Dequeue();
                // Have we emptied the queue? Top it up
                if (queues[lowest_index].Count == 0)
                {
                    LoadQueue(queues[lowest_index], readers[lowest_index], bufferlen);
                    // Was there nothing left to read?
                    if (queues[lowest_index].Count == 0)
                    {
                        queues[lowest_index] = null;
                    }
                }
            }
            sw.Close();

            // Close and delete the files
            for (int i = 0; i < chunks; i++)
            {
                readers[i].Close();
                File.Delete(paths[i]);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExternalMergeSort
{
    public class splitter : ISplitter
    {
        private dfsortoptions _opts;
        public splitter(dfsortoptions options)
        {
            _opts = options;
        }

        public void Split()
        {
            int split_num = 1;
            string destFilePart = Path.Combine(_opts.WorkingDirectory, string.Format(_opts.SplitFilePattern, split_num));
            StreamWriter sw = new StreamWriter(destFilePart);
            long read_line = 0;
            using (StreamReader sr = new StreamReader(_opts.InputFileName))
            {
                while (sr.Peek() >= 0)
                {

                    // Progress reporting
                    //if (++read_line % rowDivider == 0)
                    //    Console.Write("{0:f2}%   \r",
                    //      100.0 * sr.BaseStream.Position / sr.BaseStream.Length);

                    // Copy a line
                    sw.WriteLine(sr.ReadLine());

                    // If the file is big, then make a new split,
                    // however if this was the last line then don't bother
                    if (read_line / split_num > _opts.FileSizeDivider && sr.Peek() >= 0)
                    {
                        sw.Close();
                        destFilePart = Path.Combine(_opts.WorkingDirectory, string.Format(_opts.SplitFilePattern, split_num++));
                        sw = new StreamWriter(destFilePart);
                    }
                }
            }
            sw.Close();
        }
    }
}

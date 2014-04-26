using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Ideafixxxer.HugeFileSort
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new string[3] { "c:\\BigFile.txt", "c:\\BigFile_out.txt", "10485760" };
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: HugeFileSort <input> <output> <maxsize>");
                return;
            }

            long size;
            if (!long.TryParse(args[2], out size))
            {
                Console.WriteLine("Third parameter must be a number");
                return;
            }

            

            var hfs = new HugeFileSort { MaxFileSize = size, Comparer = StringComparer.CurrentCultureIgnoreCase };
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                hfs.Sort(args[0], args[1]);
                Console.WriteLine("Operation completed in {0} msec", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }
    }
}

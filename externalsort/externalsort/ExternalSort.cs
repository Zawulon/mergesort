using System;
//using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace ExternalSort
{
    //extend .Net Bit Converter to work on arrays
    static class ByteArray
    {
        public static unsafe void FromInt(int[] input, int inputLen, byte[] output)
        {
            if (output.Length < 4 * inputLen || inputLen > input.Length)
                throw new Exception("ByteArray.FromInt index error");

            fixed (byte* fixedOutput = output)
            {
                fixed (int* fixedInput = input)
                {
                    int* outputIntPtr = (int*)fixedOutput;
                    int* inputPtr = fixedInput;
                    for (int i = 0; i < inputLen; i++)
                    {
                        *outputIntPtr++ = *inputPtr++;
                    }
                }
            }
        }

        public static unsafe void ToInt(byte[] input, int[] output, int outputLen)
        {
            if (input.Length < 4 * outputLen || outputLen > output.Length)
                throw new Exception("ByteArray.ToInt Index error");

            fixed (int* fixedOutput = output)
            {
                fixed (byte* fixedInput = input)
                {
                    int* outputPtr = fixedOutput;
                    int* inputIntPtr = (int*)fixedInput;
                    for (int i = 0; i < outputLen; i++)
                    {
                        *outputPtr++ = *inputIntPtr++;
                    }
                }
            }
        }
    }

    public static class ExternalMergeSort
    {
        public static int sizeFromMB(int mb)
        {
            long lmb = (long)mb;
            long bytes = lmb * 1024 * 1024;
            return (int)(bytes / sizeof(int));
        }

        public class Options
        {
            public readonly int MaxSortSize;
            public readonly int MergeFileSize;
            public readonly int OutputBufferSize;
            public bool SkipInitialSort = false;

            public Options(int maxSortSize, int mergeFileSize, int outputBufferSize)
            {
                this.MaxSortSize = maxSortSize;
                this.MergeFileSize = mergeFileSize;
                this.OutputBufferSize = outputBufferSize;
            }

        }

        private class OrderedSeqFromFile
        {
            FileStream fs;

            public class Param
            {
                public FileStream fs;
                public long fileLen;
                public long remaining;
                public int bufLen;
                public int ioSize;
                public byte[] byteBuffer;
                public int[] intBuffer;
                public ManualResetEvent mre;
                public int id;
                public int sz;
                public Param(FileStream fs, long fileLen, long remaining, int ioSize, ManualResetEvent mre, int id)
                {
                    this.fs = fs;
                    this.fileLen = fileLen;
                    this.remaining = remaining;
                    this.ioSize = ioSize;
                    bufLen = sizeof(int) * ioSize;
                    byteBuffer = new byte[bufLen];
                    intBuffer = new int[ioSize];
                    this.mre = mre;
                    this.id = id;
                    sz = 0;
                }
            }

            ManualResetEvent mre;
            Param[] parameters;
            int currentParam = 0;

            public OrderedSeqFromFile(string fileName, int ioSize)
            {
                this.fs = File.Open(fileName, FileMode.Open);
                long fileLen = fs.Length;
                long remaining = fileLen / sizeof(int);
                mre = new ManualResetEvent(true);
                parameters = new Param[2];
                parameters[0] = new Param(fs, fileLen, remaining, ioSize, mre, 0);
                parameters[1] = new Param(fs, fileLen, remaining, ioSize, mre, 1);
                currentParam = 0;
                ReadAhead(parameters[currentParam]);
            }

            static void ReadOnParam(Param param)
            {
                if (param.remaining > 0)
                {
                    int sz = (int)Math.Min(param.remaining, (long)param.ioSize);
                    param.remaining -= sz;
                    param.fs.Read(param.byteBuffer, 0, sizeof(int) * sz);
                    ByteArray.ToInt(param.byteBuffer, param.intBuffer, sz);
                    param.sz = sz;
                }
                param.mre.Set();
            }

            static void ThreadProc(Object state)
            {
                Param param = (Param)state;
                ReadOnParam(param);
            }



            public void ReadAhead(Param param)
            {
                mre.Reset();
                ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), param);
            }

            public bool GetRest(OrderedSeq rest)
            {
                mre.WaitOne();//wait for previous read to complete
                Param param = parameters[currentParam];//one should always be available
                if (param.remaining + param.sz > 0)
                {
                    rest.Init(param.intBuffer, 0, param.sz, this, null, null);
                    //swap param
                    currentParam = (currentParam + 1) % 2;
                    Param newParam = parameters[currentParam];
                    newParam.remaining = param.remaining;
                    newParam.sz = 0;
                    ReadAhead(newParam);
                    return true;
                }
                mre.Dispose();
                fs.Close();
                fs.Dispose();
                return false;
            }

        }

        private class OrderedSeq
        {
            public int[] data;
            public int fromPos;
            public int toPos;
            public OrderedSeq set1;
            public OrderedSeq set2;
            public OrderedSeqFromFile fromFile; //if <> null it's a file stream

            public void Init(int[] data, int fromPos, int toPos, OrderedSeqFromFile fromFile, OrderedSeq set1, OrderedSeq set2)
            {
                this.data = data;
                this.fromPos = fromPos;
                this.toPos = toPos;
                this.set1 = set1;
                this.set2 = set2;
                this.fromFile = fromFile;
            }

            public void CopyFrom(OrderedSeq other)
            {
                data = other.data;
                fromPos = other.fromPos;
                toPos = other.toPos;
                set1 = other.set1;
                set2 = other.set2;
                fromFile = other.fromFile;
            }

            public bool ReplaceWithSubseq(int newFrom, int newTo)
            {
                this.fromPos = newFrom;
                this.toPos = newTo;
                if (this.fromPos < this.toPos)
                {
                    return false;
                }
                return ReplaceWithRest();
            }

            public bool ReplaceWithRest()
            {
                if (fromFile != null)
                {
                    return fromFile.GetRest(this);
                }
                else if (set1 == null)
                {
                    if (set2 == null)
                        return false;
                    //oldSet2 = set2
                    CopyFrom(set2);
                    //oldSet2.Destroy();
                    return true;
                }
                else if (set2 == null)
                {
                    //oldSet1 = set1
                    CopyFrom(set1);
                    //oldSet1.Destroy();
                    return true;
                }
                return Merge2Seq(set1, set2, this);
            }
        }

        private static bool FromFile(string fileName, int ioSize, OrderedSeq rest)
        {
            OrderedSeqFromFile fromFile = new OrderedSeqFromFile(fileName, ioSize);
            return fromFile.GetRest(rest);
        }

        private static bool Merge2Seq(OrderedSeq set1, OrderedSeq set2, OrderedSeq rest)
        {
            int i1 = set1.fromPos;
            int i2 = set2.fromPos;
            int e1 = set1.toPos;
            int e2 = set2.toPos;
            int[] arr1 = set1.data;
            int[] arr2 = set2.data;
            int[] temp = rest.data;
            Debug.Assert(set1 == rest.set1);
            Debug.Assert(set2 == rest.set2);
            Debug.Assert(temp != null);
            Debug.Assert(set1 != set2);
            Debug.Assert(set1 != rest);
            Debug.Assert(set2 != rest);
            int j = 0;
        loop:
            if (i1 >= e1) { goto copy_2; }
            if (i2 >= e2) { goto copy_1; }
            int v1 = arr1[i1]; int v2 = arr2[i2];
            if (v1 < v2) { temp[j++] = v1; i1++; }
            else { temp[j++] = v2; i2++; }
            goto loop;
        copy_1:
            rest.fromPos = 0;
            rest.toPos = j;
            set1.ReplaceWithSubseq(i1, e1);
            if (!set2.ReplaceWithRest())
                rest.set2 = null;
            return true;
        copy_2:
            rest.fromPos = 0;
            rest.toPos = j;
            if (!set1.ReplaceWithRest())
                rest.set1 = null;
            set2.ReplaceWithSubseq(i2, e2);
            return true;
        }


        class WriteToFileParam
        {
            public int[] buffer;
            public char[] charBuffer;
            public int id;
            public ManualResetEvent mre;
            public int len;
            public StreamWriter w;

            public WriteToFileParam(int len, StreamWriter w, ManualResetEvent mre, int id)
            {
                buffer = new int[len];
                charBuffer = new char[1024];
                this.len = len;
                this.w = w;
                this.mre = mre;
                this.id = id;
            }
        }

        public static bool StringToInt(string s, out int res)
        {
            int len = s.Length;
            res = 0;

            if (len >= 1)
            {
                int zero = (int)'0';
                int i = 0;
                if (s[0] == '-')
                    i++;
                int num = 0;
                while (i < len)
                {
                    char digit = s[i++];
                    if (digit < '0' || digit > '9')
                        return false;
                    num = 10 * num + ((int)(digit) - zero);
                }
                if (s[0] == '-') num = -num;
                res = num;
                return true;
            }
            return false;
        }

        public static int IntToCharBuffer(int num, char[] buffer)
        {
            int i = 0;
            int off = 1;
            if (num < 0)
            {
                buffer[i++] = '-';
                num = -num;
                off = 0;
            }
            int start = i;

            int zero = (int)'0';
            do
            {
                int nextNum = num / 10;
                char d = (char)(zero + num - 10 * nextNum);
                num = nextNum;
                buffer[i++] = d;
            } while (num > 0);
            int mid = start + (i - start) / 2;
            while (start < mid)
            {
                char tmp = buffer[start];
                int end_ = i - start - off;
                buffer[start] = buffer[end_];
                buffer[end_] = tmp;
                start++;
            }
            return i;
        }

        private static void IntsToFile(WriteToFileParam param)
        {
            StreamWriter w = param.w;
            int[] data = param.buffer;
            char[] charBuffer = param.charBuffer;
            int len = param.len;
            for (int i = 0; i < len; i++)
            {
                int j = IntToCharBuffer(data[i], charBuffer);
                charBuffer[j++] = '\r';
                charBuffer[j++] = '\n';
                for (int k = 0; k < j; k++)
                    w.Write(charBuffer[k]);

            }
        }

        static void ThreadProcIntsToFile(object state)
        {
            WriteToFileParam param = (WriteToFileParam)state;
            IntsToFile(param);
            param.mre.Set();//waiting thread can proceed
        }

        private static void WriteOnThread(WriteToFileParam param)
        {
            param.mre.WaitOne();//wait for previous thread to complete
            param.mre.Reset();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProcIntsToFile), param);
        }

        private static void ToTextFile(OrderedSeq s, string fileName, int outputLen)
        {
            using (StreamWriter w = new StreamWriter(fileName))
            {
                using (ManualResetEvent mre = new ManualResetEvent(true))
                {
                    bool flag = true;

                    WriteToFileParam[] parameters = new WriteToFileParam[2];
                    parameters[0] = new WriteToFileParam(outputLen, w, mre, 0);
                    parameters[1] = new WriteToFileParam(outputLen, w, mre, 1);
                    int currentParam = 0;
                    int[] buffer = parameters[currentParam].buffer;
                    int j = 0;
                    while (flag)
                    {
                        int fromPos = s.fromPos;
                        int finalToPos = s.toPos;
                        while (fromPos < finalToPos)
                        {
                            int sz = Math.Min(outputLen - j, finalToPos - fromPos);
                            int intermToPos = fromPos + sz;
                            for (int i = fromPos; i < intermToPos; i++)
                            {
                                buffer[j++] = s.data[i];
                            }
                            fromPos += sz;
                            if (j >= outputLen)
                            {
                                //write to file
                                WriteOnThread(parameters[currentParam]);
                                currentParam = (currentParam + 1) % 2;
                                buffer = parameters[currentParam].buffer;
                                j = 0;
                            }
                        }
                        flag = s.ReplaceWithRest();
                    }
                    if (j > 0)
                    {
                        //write to file
                        parameters[currentParam].len = j;
                        WriteOnThread(parameters[currentParam]);
                    }
                    mre.WaitOne();
                    parameters[0].w = null; parameters[0].mre = null;
                    parameters[1].w = null; parameters[1].mre = null;

                }
                w.Close();
            }
        }


        private static void ArrayToFile(int[] data, byte[] byteOutput, int len, string fileName)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Create)) //to do: change to .CreateNew
            {
                ByteArray.FromInt(data, len, byteOutput);
                fs.Write(byteOutput, 0, sizeof(int) * len);
                fs.Close();
            }
        }

        private static void Error(string desc)
        {
            throw new Exception(desc);
        }

        private static bool MergeBy2(string[] inputFiles, int fromIdx, int toIdx, int ioSize, OrderedSeq rest)
        {
            int len = toIdx - fromIdx;
            if (len == 0)
            {
                Error("internal error");
                return false;
            }
            else if (len == 1)
            {
                return FromFile(inputFiles[fromIdx], ioSize, rest);
            }
            else if (len == 2)
            {
                OrderedSeq s1 = new OrderedSeq();
                OrderedSeq s2 = new OrderedSeq();
                FromFile(inputFiles[fromIdx], ioSize, s1);
                FromFile(inputFiles[fromIdx + 1], ioSize, s2);
                rest.data = new int[s1.data.Length + s2.data.Length];
                rest.set1 = s1; rest.set2 = s2;
                return Merge2Seq(s1, s2, rest);
            }
            else
            {
                int mid = fromIdx + len / 2;
                OrderedSeq s1 = new OrderedSeq();
                MergeBy2(inputFiles, fromIdx, mid, ioSize, s1);
                OrderedSeq s2 = new OrderedSeq();
                MergeBy2(inputFiles, mid, toIdx, ioSize, s2);
                rest.data = new int[s1.data.Length + s2.data.Length];
                rest.set1 = s1; rest.set2 = s2;
                return Merge2Seq(s1, s2, rest);
            }
        }

        private static void MergeMany(string[] inputFiles, string outputFile, int ioSize, int outputLen)
        {
            int len = inputFiles.Length;

            if (len == 0)
            {
                Error("Number of input files has to be larger than 0");
            }
            else if (len == 1)
            {
                OrderedSeq rest = new OrderedSeq();
                FromFile(inputFiles[0], ioSize, rest);
                ToTextFile(rest, outputFile, outputLen);
            }
            else
            {
                OrderedSeq rest = new OrderedSeq();
                MergeBy2(inputFiles, 0, len, ioSize, rest);
                ToTextFile(rest, outputFile, outputLen);
            }
        }

        private static void Warn(string line)
        {
            Console.WriteLine(line);
        }

        private class NameGen
        {
            string dir;
            int cntr = 0;
            public NameGen(string dir)
            {
                this.dir = dir;
            }

            public string NewName()
            {
                cntr++;
                return System.IO.Path.Combine(dir, "tmp_sort_file_" + cntr.ToString());
            }
        }

        class SortParam
        {
            public int[] buffer;
            public byte[] byteBuffer;
            public int len;
            //public int[] aux;
            public string name;
            public ManualResetEvent mre;
            public int id;

            public SortParam(int sz, ManualResetEvent mre, int id)
            {
                buffer = new int[sz];
                //aux = new int[sz]; //used for merge sort in old version
                byteBuffer = new byte[sizeof(int) * sz];
                this.mre = mre;
                this.id = id;
            }

            public void Reset(int len, string name)
            {
                this.len = len;
                this.name = name;
            }
        }

        static void SortFromParam(SortParam param)
        {
            //merge sort vs. dot net sort
            Array.Sort(param.buffer, 0, param.len);
            ArrayToFile(param.buffer, param.byteBuffer, param.len, param.name);
        }

        static void ThreadProc(Object state)
        {
            SortParam param = (SortParam)state;
            SortFromParam(param);
            param.mre.Set();//waiting thread can proceed
        }

        private static void SortOnThread(SortParam param)
        {
            param.mre.WaitOne();//wait for previous thread to complete
            param.mre.Reset();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), param);
        }

        private static string[] InitialSort(string inputFile, string tempDir, Options opt)
        {
            List<string> tempFiles = new List<string>();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //read a chunk, sort it, then write it to file
            using (StreamReader reader = new StreamReader(inputFile))
            {
                using (ManualResetEvent mre = new ManualResetEvent(true))
                {
                    NameGen nameGen = new NameGen(tempDir);
                    string line;
                    int lineNo = 0;
                    int maxSortSize = opt.MaxSortSize;
                    SortParam[] parameters = new SortParam[2];
                    parameters[0] = new SortParam(maxSortSize, mre, 0);
                    parameters[1] = new SortParam(maxSortSize, mre, 1);
                    int currentParam = 0;

                    int[] buffer = parameters[currentParam].buffer;
                    int j = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNo++;
                        int data;
                        //if (Int32.TryParse(line, out data)) //slow
                        if (StringToInt(line, out data))
                        {
                            buffer[j++] = data;
                            if (j >= maxSortSize)
                            {
                                string name = nameGen.NewName();
                                tempFiles.Add(name);
                                parameters[currentParam].Reset(j, name);
                                SortOnThread(parameters[currentParam]);
                                //swap
                                currentParam = (currentParam + 1) % 2;
                                buffer = parameters[currentParam].buffer;
                                j = 0;
                            }
                        }
                        else
                            Warn("Skipping line " + lineNo.ToString());
                    }
                    if (j > 0)
                    {
                        string name = nameGen.NewName();
                        tempFiles.Add(name);
                        parameters[currentParam].Reset(j, name);
                        SortOnThread(parameters[currentParam]);
                    }
                    mre.WaitOne();
                    parameters[0].mre = null; parameters[1].mre = null;
                }
                reader.Close();
            }
            sw.Stop();
            Console.WriteLine("Time to sort intermediate files: {0}", sw.Elapsed.ToString());

            return tempFiles.ToArray();
        }

        public static void Sort(string inputFile, string outputFile, string tempDir, Options opt)
        {
            string[] tempFiles = null;
            if (opt.SkipInitialSort)
            {
                tempFiles = Directory.EnumerateFiles(tempDir).ToArray();

            }
            else
                tempFiles = InitialSort(inputFile, tempDir, opt);
            //merge the resulting small files
            Console.WriteLine("Number of files to merge: {0}", tempFiles.Length);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            MergeMany(tempFiles, outputFile, opt.MergeFileSize, opt.OutputBufferSize);
            sw.Stop();
            Console.WriteLine("Time to merge intermediate files: {0}", sw.Elapsed.ToString());
        }
    }

    static class Test
    {
        public static void GenFile(long smallLen, long len, int range, System.Random rnd, string filename)
        {
            double gb = (double)smallLen * (double)len * 4.0 / (1024 * 1024 * 1024);
            Console.WriteLine("Expected file length {0} GB", gb);
            using (StreamWriter w = new StreamWriter(filename, false))
            {
                for (long k = 0; k < smallLen; k++)
                {
                    for (long i = 0; i < len; i++)
                    {
                        int el = 1 + rnd.Next(range);
                        w.WriteLine(el.ToString());
                    }
                }
                w.Close();
            }
        }

        public static int[] ReadIntFile(string fileName)
        {
            System.Collections.Generic.List<int> lst = new System.Collections.Generic.List<int>();
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    lst.Add(Int32.Parse(line));
                }
            }
            return lst.ToArray();
        }

        public static void TestResult(string input, string output)
        {
            int[] inputData = ReadIntFile(input);
            Array.Sort(inputData);
            int[] outputData = ReadIntFile(output);
            if (inputData.Length != outputData.Length)
                throw new Exception("input.Length != output.Length");
            for (int i = 0; i < inputData.Length; i++)
            {
                if (inputData[i] != outputData[i])
                {
                    throw new Exception("input[i] != output[i]");
                }
            }
            Console.WriteLine("Test is OK");
        }
    }

    class Program
    {
        static void LargeTest()
        {
            string input = @"c:\tmpSort\BigFile.txt";
            string tempDir = @"c:\tmpSort\tmp\";
            string output = @"c:\tmpSort\output1.txt";
            
            Console.WriteLine("Begin Data Gen");
            var swGen = new System.Diagnostics.Stopwatch();
            swGen.Start();
            Test.GenFile(50, 100000000, 100000, new System.Random(51), input);
            swGen.Stop();
            Console.WriteLine("Data Gen Time: {0}", swGen.Elapsed.ToString());
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var opt = new ExternalMergeSort.Options(ExternalMergeSort.sizeFromMB(200), ExternalMergeSort.sizeFromMB(10 /*0*/ /*10*/) / 10, 1024 * 10);
            opt.SkipInitialSort = false;
            ExternalMergeSort.Sort(input, output, tempDir, opt);
            sw.Stop();
            Console.WriteLine("Total Sort Time: {0}", sw.Elapsed.ToString());
            //Test.TestResult(input, output);//cannot test for large files with this code
        }

        static void SmallTest()
        {
            string input = @"C:\tmpSort\BigFile.txt";
            string output = @"C:\tmpSort\output.txt";
            Console.WriteLine("Begin Data Gen");
            var swGen = new System.Diagnostics.Stopwatch();
            swGen.Start();
            Test.GenFile(1, 100000, 1000*100, new System.Random(51), input);
            swGen.Stop();
            Console.WriteLine("Data Gen Time: {0}", swGen.Elapsed.ToString());
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var opt = new ExternalMergeSort.Options(100, 10, 5); //ExternalMergeSort.sizeFromMB(1), ExternalMergeSort.sizeFromMB(1) / 10);
            //var opt = new ExternalMergeSort.Options(100000, 10000); //ExternalMergeSort.sizeFromMB(1), ExternalMergeSort.sizeFromMB(1) / 10);
            ExternalMergeSort.Sort(input, output, @"C:\tmpSort\tmp\", opt);
            sw.Stop();
            Console.WriteLine("Total Sort Time: {0}", sw.Elapsed.ToString());
            Test.TestResult(input, output);
        }

        static void Main(string[] args){
            SmallTest();
        }
    }
}

//Results:
//Time to generate file is around 20 min.

//File is 30 GB text file of integers
//Time to sort intermediate files: 00:28:34.9422320
//Number of files to merge: 96
//Time to merge intermediate files: 00:24:50.6632475 (00:27:43.6421148 when '\n' for end of line is replaced by '\r\n')
//Total Sort Time: 00:53:25.9329689
//theoretical using 30MB per second transfer rate and assuming 25GB of data being written twice and read twice
//25.0*4.0*1024.0/(30.0*60.0) = 56.88888889

//Result of unix sort command
//$ time sort -n --buffer-size=1000M --temporary-directory=/cygdrive/e/tmpSort/tmp/ --output=unixsort.output input.txt
//real    422m35.296s
//user    346m14.309s
//sys     2m56.857s

//to do:
//eliminate large byte buffer in first soring pass
//Result of unix wc commnand on output file
//$ time wc -l output.txt
//5000000000 output.txt
//real    14m22.307s
//user    1m40.386s
//sys     0m27.424s




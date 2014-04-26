using NSort;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nsort_test
{
    class Program
    {
        static void Main(string[] args)
        {
            SorterTest s = new SorterTest();
            s.Sorter = new QuickSorter();
            s.SortTest();
        }
    }

    public class SorterTest
    {
        private ISorter sorter = null;

        public ISorter Sorter
        {
            get
            {
                return this.sorter;
            }
            set
            {
                this.sorter = value;
            }
        }

        // a bug in MUTE doesn't pick up base class tests!
        public void SortTest()
        {
            Random rnd = new Random();
            int[] list = new int[1000];
            int i;
            for (i = 0; i < list.Length; ++i)
                list[i] = rnd.Next();

            // create sorted list
            SortedList sl = new SortedList();
            foreach (int key in list)
                sl.Add(key, null);

            // sort table
            Sorter.Sort(list);

            i = 0;
            foreach (int val in sl.Keys)
            {
                //Assertion.Assert(val == list[i], "Sorter failed.");
                ++i;
            }
        }
    }
}

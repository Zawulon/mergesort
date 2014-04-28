using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExternalMergeSort
{
    public class dfsorf
    {
        private dfsortoptions _opts;
        public dfsorf(dfsortoptions options)
        {
            _opts = options;
        }

        public void Execute()
        {
            splitter sp = new splitter(_opts);
            sp.Split();
            sorter so = new sorter(_opts);
            so.SortChunks();
        }
    }
}

using System;
using System.Collections;

namespace NSort
{
	public class InsertionSort : SwapSorter
	{
		public InsertionSort() : base() {}

		public InsertionSort(IComparer comparer, ISwap swapper)
			: base(comparer,swapper)
		{}
		
		public override void Sort(IList list) 
		{
			int i;
			int j;
			object b;

			for (i=1; i<list.Count ;i++)
			{
				j=i;
				b = list[i];
				while ((j > 0) && (Comparer.Compare(list[j-1], b)>0))
				{
					Swapper.Set(list, j, list[j-1]);
					--j;
				}
				Swapper.Set(list, j, b);
			}						 
		}
	}
}

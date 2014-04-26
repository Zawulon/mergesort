using System;
using System.Collections;

namespace NSort
{
	public class ShakerSort : SwapSorter
	{
		public ShakerSort() : base() {}

		public ShakerSort(IComparer comparer, ISwap swapper)
			:base(comparer,swapper)
		{
		}

		public override void Sort(IList list) 
		{
			int i;
			int j;
			int k;
			int min;
			int max;

			i = 0;
			k = list.Count-1;
			while (i < k) 
			{
				min = i;
				max = i;
				for (j=i+1;j<=k;j++) 
				{
					if (Comparer.Compare(list[j], list[min])<0) 
					{
						min = j;
					}
					if (Comparer.Compare(list[j], list[max])>0) 
					{
						max = j;
					}
				}
				Swapper.Swap(list, min, i);
				if (max == i) 
				{
					Swapper.Swap(list, min,k);
				} 
				else 
				{
					Swapper.Swap(list, max, k);
				}
				i++;
				k--;
			}
		}
	}
}

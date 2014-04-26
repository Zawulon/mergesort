using System;
using System.Collections;

namespace NSort
{
	public class HeapSort : SwapSorter
	{
		public HeapSort() : base() {}

		public HeapSort(IComparer comparer, ISwap swapper)
			:base(comparer,swapper)
		{
		}

		public override void Sort(IList list) 
		{
			int n;
			int i;

			n = list.Count;
			for (i = n / 2;i>0;i--) 
			{
				DownHeap(list, i, n);
			}
			do 
			{
				Swapper.Swap(list, 0, n-1);
				n = n - 1;
				DownHeap(list, 1, n);
			} while (n>1);
		}

		private void DownHeap(IList list, int k, int n)
		{
			int j;
			bool loop=true;

			while ( (k <= n / 2) && loop) 
			{
				j = k + k;
				if (j < n) 
				{
					if (Comparer.Compare(list[j-1], list[j]) < 0) 
					{					
						j++;
					}
				}	
				if (Comparer.Compare(list[k-1], list[j-1]) >= 0) 
				{
					loop=false;
				} 
				else 
				{
					Swapper.Swap(list, k-1, j-1);
					k = j;
				}
			}
		}
	}
}

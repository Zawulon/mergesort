using System;

using NSort;

using NUnit.Framework;

namespace QuickGraphNUnit.Collections
{
	/// <summary>
	/// Summary description for SwapSorterTest.
	/// </summary>
	[TestFixture]
	public class SwapSorterTest
	{
		public SwapSorter Sorter
		{
			get
			{
				return new BubbleSorter();
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullSwapper()
		{
			SwapSorter sorter = Sorter;
			sorter.Swapper = null;
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullComparer()
		{
			SwapSorter sorter = Sorter;
			sorter.Comparer = null;
		}
	}
}

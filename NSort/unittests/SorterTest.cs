using System;
using System.Collections;


using NSort;
using NUnit.Framework;

namespace QuickGraphNUnit.Collections
{
	/// <summary>
	/// Summary description for SorterTest.
	/// </summary>
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
			for(i = 0;i<list.Length;++i)
				list[i] = rnd.Next();

			// create sorted list
			SortedList sl =new SortedList();
			foreach(int key in list)
				sl.Add(key,null);

			// sort table
			Sorter.Sort(list);

			i = 0;
			foreach(int val in sl.Keys)
			{
				Assertion.Assert(val==list[i], "Sorter failed.");
				++i;
			}
		}
	}

	[TestFixture]
	public class QuickSorterTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new QuickSorter();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}


	[TestFixture]
	public class BubbleSorterTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new BubbleSorter();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}


	[TestFixture]
	public class BiDirectionalBubbleSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new BiDirectionalBubbleSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}


	[TestFixture]
	public class ComboSort11Test : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new ComboSort11();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class HeapSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new HeapSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class ShearSorterTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new ShearSorter();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}


	[TestFixture]
	public class OddEvenTransportSorterTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new OddEvenTransportSorter();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class FastQuickSorterTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new FastQuickSorter();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class SelectionSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new SelectionSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class ShakerTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new ShakerSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class DoubleStorageMergeSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new DoubleStorageMergeSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class InPlaceMergeSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new InPlaceMergeSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class InsertionSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new InsertionSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class ShellSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new ShellSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}

	[TestFixture]
	public class QuickSortWithBubbleSortTest : SorterTest
	{
		[TestSetUp]
		public void SetUp()
		{
			this.Sorter = new QuickSortWithBubbleSort();
		}

		[Test]
		public void Test()
		{
			SortTest();
		}
	}
}

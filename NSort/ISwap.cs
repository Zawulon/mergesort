using System;
using System.Collections;

namespace NSort
{
	/// <summary>
	/// Object swapper interface
	/// </summary>
	public interface ISwap
	{
		void Swap(IList array, int left, int right);
		void Set(IList array, int left, int right);
		void Set(IList array, int left, object obj);
	}
}

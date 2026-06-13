using System;
using System.Collections;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// Gets an element from a collection at a specified index.
	/// Works with any List&lt;T&gt; or IList collection.
	/// </summary>
	[StateInfo(
		"Gets element at index",
		"Reads an element from a collection (List or array) at the specified index. " +
		"Outputs the element and the collection count. " +
		"Index is clamped to valid range; outputs null if collection is empty."
	)]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.Grid)]
	public class GetCollectionElementState : State
	{
		[DataIn("Collection")]
		public IList Collection;

		[DataIn("Index")]
		public int Index;

		[DataOut("Element")]
		public object Element;

		[DataOut("Count")]
		public int Count;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Count = Collection?.Count ?? 0;

			if (Collection != null && Count > 0 && Index >= 0 && Index < Count)
			{
				Element = Collection[Index];
			}
			else
			{
				Element = null;
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Increments an index value, wrapping around at a specified count.
	/// Useful for cycling through collection elements.
	/// </summary>
	[StateInfo(
		"Cycles an index",
		"Increments the current index by one and wraps it to zero when it reaches the count. " +
		"Use with GetCollectionElementState to cycle through list elements."
	)]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.Restart)]
	public class CycleIndexState : State
	{
		[DataIn("Current")]
		public int Current;

		[DataIn("Count")]
		public int Count;

		[DataOut("Next")]
		public int Next;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Next = Count > 0 ? (Current + 1) % Count : 0;
			OnComplete?.Invoke();
		}
	}
}

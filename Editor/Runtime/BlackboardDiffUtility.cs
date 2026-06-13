using System.Collections.Generic;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Computes the difference between two blackboard snapshots.
	/// </summary>
	public static class BlackboardDiffUtility
	{
		public static BlackboardDiff ComputeDiff(
			Dictionary<string, string> snapshotA,
			Dictionary<string, string> snapshotB)
		{
			var diff = new BlackboardDiff();

			snapshotA ??= new Dictionary<string, string>();
			snapshotB ??= new Dictionary<string, string>();

			foreach (var kvp in snapshotB)
			{
				if (!snapshotA.ContainsKey(kvp.Key))
				{
					diff.Added.Add(new BlackboardDiffEntry
					{
						Key = kvp.Key,
						NewValue = kvp.Value
					});
				}
				else if (snapshotA[kvp.Key] != kvp.Value)
				{
					diff.Changed.Add(new BlackboardDiffEntry
					{
						Key = kvp.Key,
						OldValue = snapshotA[kvp.Key],
						NewValue = kvp.Value
					});
				}
				else
				{
					diff.Unchanged.Add(new BlackboardDiffEntry
					{
						Key = kvp.Key,
						OldValue = kvp.Value,
						NewValue = kvp.Value
					});
				}
			}

			foreach (var kvp in snapshotA)
			{
				if (!snapshotB.ContainsKey(kvp.Key))
				{
					diff.Removed.Add(new BlackboardDiffEntry
					{
						Key = kvp.Key,
						OldValue = kvp.Value
					});
				}
			}

			return diff;
		}
	}

	public class BlackboardDiff
	{
		public List<BlackboardDiffEntry> Added { get; } = new List<BlackboardDiffEntry>();
		public List<BlackboardDiffEntry> Removed { get; } = new List<BlackboardDiffEntry>();
		public List<BlackboardDiffEntry> Changed { get; } = new List<BlackboardDiffEntry>();
		public List<BlackboardDiffEntry> Unchanged { get; } = new List<BlackboardDiffEntry>();

		public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
	}

	public class BlackboardDiffEntry
	{
		public string Key { get; set; }
		public string OldValue { get; set; }
		public string NewValue { get; set; }
	}
}

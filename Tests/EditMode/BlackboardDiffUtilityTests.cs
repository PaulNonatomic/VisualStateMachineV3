using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Runtime;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardDiffUtilityTests
	{
		[Test]
		public void ComputeDiff_BothNull_ReturnsEmptyDiff()
		{
			var diff = BlackboardDiffUtility.ComputeDiff(null, null);

			Assert.IsNotNull(diff);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
			Assert.IsFalse(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_BothEmpty_ReturnsEmptyDiff()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string>();

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
			Assert.IsFalse(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_NullA_TreatsAsEmpty()
		{
			var b = new Dictionary<string, string> { { "key", "value" } };

			var diff = BlackboardDiffUtility.ComputeDiff(null, b);

			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual("key", diff.Added[0].Key);
			Assert.AreEqual("value", diff.Added[0].NewValue);
		}

		[Test]
		public void ComputeDiff_NullB_TreatsAsEmpty()
		{
			var a = new Dictionary<string, string> { { "key", "value" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, null);

			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual("key", diff.Removed[0].Key);
			Assert.AreEqual("value", diff.Removed[0].OldValue);
		}

		[Test]
		public void ComputeDiff_NewKeyInB_DetectedAsAdded()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string> { { "health", "100" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual("health", diff.Added[0].Key);
			Assert.AreEqual("100", diff.Added[0].NewValue);
			Assert.IsNull(diff.Added[0].OldValue);
		}

		[Test]
		public void ComputeDiff_MultipleNewKeys_AllDetectedAsAdded()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "mana", "50" },
				{ "name", "Player" }
			};

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(3, diff.Added.Count);
			var addedKeys = diff.Added.Select(e => e.Key).ToList();
			Assert.Contains("health", addedKeys);
			Assert.Contains("mana", addedKeys);
			Assert.Contains("name", addedKeys);
		}

		[Test]
		public void ComputeDiff_Added_HasChanges_ReturnsTrue()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string> { { "key", "value" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_KeyMissingFromB_DetectedAsRemoved()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string>();

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual("health", diff.Removed[0].Key);
			Assert.AreEqual("100", diff.Removed[0].OldValue);
			Assert.IsNull(diff.Removed[0].NewValue);
		}

		[Test]
		public void ComputeDiff_MultipleKeysRemoved_AllDetected()
		{
			var a = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "mana", "50" }
			};
			var b = new Dictionary<string, string>();

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(2, diff.Removed.Count);
		}

		[Test]
		public void ComputeDiff_Removed_HasChanges_ReturnsTrue()
		{
			var a = new Dictionary<string, string> { { "key", "value" } };
			var b = new Dictionary<string, string>();

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_ValueChanged_DetectedAsChanged()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string> { { "health", "75" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Changed.Count);
			Assert.AreEqual("health", diff.Changed[0].Key);
			Assert.AreEqual("100", diff.Changed[0].OldValue);
			Assert.AreEqual("75", diff.Changed[0].NewValue);
		}

		[Test]
		public void ComputeDiff_MultipleValuesChanged_AllDetected()
		{
			var a = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "state", "Idle" }
			};
			var b = new Dictionary<string, string>
			{
				{ "health", "50" },
				{ "state", "Combat" }
			};

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(2, diff.Changed.Count);
		}

		[Test]
		public void ComputeDiff_Changed_HasChanges_ReturnsTrue()
		{
			var a = new Dictionary<string, string> { { "key", "old" } };
			var b = new Dictionary<string, string> { { "key", "new" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_SameValue_DetectedAsUnchanged()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string> { { "health", "100" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Unchanged.Count);
			Assert.AreEqual("health", diff.Unchanged[0].Key);
			Assert.AreEqual("100", diff.Unchanged[0].OldValue);
			Assert.AreEqual("100", diff.Unchanged[0].NewValue);
		}

		[Test]
		public void ComputeDiff_OnlyUnchanged_HasChanges_ReturnsFalse()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string> { { "health", "100" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsFalse(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_MixedChanges_CorrectlyCategorized()
		{
			var a = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "mana", "50" },
				{ "removed_var", "old" }
			};
			var b = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "mana", "75" },
				{ "new_var", "new" }
			};

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Unchanged.Count);
			Assert.AreEqual("health", diff.Unchanged[0].Key);

			Assert.AreEqual(1, diff.Changed.Count);
			Assert.AreEqual("mana", diff.Changed[0].Key);

			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual("new_var", diff.Added[0].Key);

			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual("removed_var", diff.Removed[0].Key);

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_IdenticalSnapshots_NoChanges()
		{
			var snapshot = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "mana", "50" },
				{ "name", "Player" }
			};
			var a = new Dictionary<string, string>(snapshot);
			var b = new Dictionary<string, string>(snapshot);

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(3, diff.Unchanged.Count);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.IsFalse(diff.HasChanges);
		}

		[Test]
		public void ComputeDiff_CompletelyDifferentSnapshots_AllAddedAndRemoved()
		{
			var a = new Dictionary<string, string>
			{
				{ "old1", "v1" },
				{ "old2", "v2" }
			};
			var b = new Dictionary<string, string>
			{
				{ "new1", "v3" },
				{ "new2", "v4" }
			};

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(2, diff.Added.Count);
			Assert.AreEqual(2, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
		}

		[Test]
		public void ComputeDiff_EmptyStringValue_TreatedAsValue()
		{
			var a = new Dictionary<string, string> { { "key", "" } };
			var b = new Dictionary<string, string> { { "key", "value" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Changed.Count);
			Assert.AreEqual("", diff.Changed[0].OldValue);
			Assert.AreEqual("value", diff.Changed[0].NewValue);
		}

		[Test]
		public void ComputeDiff_EmptyStringToEmpty_IsUnchanged()
		{
			var a = new Dictionary<string, string> { { "key", "" } };
			var b = new Dictionary<string, string> { { "key", "" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(1, diff.Unchanged.Count);
		}

		[Test]
		public void ComputeDiff_LargeSnapshot_PerformsCorrectly()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string>();
			for (var i = 0; i < 100; i++)
			{
				a[$"key_{i}"] = $"value_{i}";
				b[$"key_{i}"] = i < 50 ? $"value_{i}" : $"changed_{i}";
			}

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.AreEqual(50, diff.Unchanged.Count);
			Assert.AreEqual(50, diff.Changed.Count);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
		}

		[Test]
		public void BlackboardDiff_NewInstance_HasEmptyLists()
		{
			var diff = new BlackboardDiff();

			Assert.IsNotNull(diff.Added);
			Assert.IsNotNull(diff.Removed);
			Assert.IsNotNull(diff.Changed);
			Assert.IsNotNull(diff.Unchanged);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
		}

		[Test]
		public void BlackboardDiff_HasChanges_TrueWhenAdded()
		{
			var diff = new BlackboardDiff();
			diff.Added.Add(new BlackboardDiffEntry { Key = "k", NewValue = "v" });

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void BlackboardDiff_HasChanges_TrueWhenRemoved()
		{
			var diff = new BlackboardDiff();
			diff.Removed.Add(new BlackboardDiffEntry { Key = "k", OldValue = "v" });

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void BlackboardDiff_HasChanges_TrueWhenChanged()
		{
			var diff = new BlackboardDiff();
			diff.Changed.Add(new BlackboardDiffEntry { Key = "k", OldValue = "a", NewValue = "b" });

			Assert.IsTrue(diff.HasChanges);
		}

		[Test]
		public void BlackboardDiff_HasChanges_FalseWhenOnlyUnchanged()
		{
			var diff = new BlackboardDiff();
			diff.Unchanged.Add(new BlackboardDiffEntry { Key = "k", OldValue = "v", NewValue = "v" });

			Assert.IsFalse(diff.HasChanges);
		}

		[Test]
		public void BlackboardDiffEntry_Key_SetAndGet()
		{
			var entry = new BlackboardDiffEntry { Key = "health" };
			Assert.AreEqual("health", entry.Key);
		}

		[Test]
		public void BlackboardDiffEntry_OldValue_SetAndGet()
		{
			var entry = new BlackboardDiffEntry { OldValue = "100" };
			Assert.AreEqual("100", entry.OldValue);
		}

		[Test]
		public void BlackboardDiffEntry_NewValue_SetAndGet()
		{
			var entry = new BlackboardDiffEntry { NewValue = "75" };
			Assert.AreEqual("75", entry.NewValue);
		}

	}
}

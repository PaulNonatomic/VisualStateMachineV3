using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Runtime;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class TransitionTimelineTests
	{
		[Test]
		public void BlackboardDiff_IdenticalSnapshots_NoChanges()
		{
			var a = new Dictionary<string, string> { { "health", "100" }, { "speed", "5" } };
			var b = new Dictionary<string, string> { { "health", "100" }, { "speed", "5" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsFalse(diff.HasChanges);
			Assert.AreEqual(2, diff.Unchanged.Count);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
		}

		[Test]
		public void BlackboardDiff_ValueChanged_DetectedCorrectly()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string> { { "health", "50" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(1, diff.Changed.Count);
			Assert.AreEqual("health", diff.Changed[0].Key);
			Assert.AreEqual("100", diff.Changed[0].OldValue);
			Assert.AreEqual("50", diff.Changed[0].NewValue);
		}

		[Test]
		public void BlackboardDiff_VariableAdded_DetectedCorrectly()
		{
			var a = new Dictionary<string, string> { { "health", "100" } };
			var b = new Dictionary<string, string> { { "health", "100" }, { "armor", "25" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual("armor", diff.Added[0].Key);
			Assert.AreEqual("25", diff.Added[0].NewValue);
		}

		[Test]
		public void BlackboardDiff_VariableRemoved_DetectedCorrectly()
		{
			var a = new Dictionary<string, string> { { "health", "100" }, { "temp", "1" } };
			var b = new Dictionary<string, string> { { "health", "100" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual("temp", diff.Removed[0].Key);
			Assert.AreEqual("1", diff.Removed[0].OldValue);
		}

		[Test]
		public void BlackboardDiff_NullSnapshots_HandlesGracefully()
		{
			var diff = BlackboardDiffUtility.ComputeDiff(null, null);

			Assert.IsFalse(diff.HasChanges);
			Assert.AreEqual(0, diff.Added.Count);
			Assert.AreEqual(0, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
		}

		[Test]
		public void BlackboardDiff_EmptyToPopulated_AllAdded()
		{
			var a = new Dictionary<string, string>();
			var b = new Dictionary<string, string> { { "x", "1" }, { "y", "2" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(2, diff.Added.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
		}

		[Test]
		public void BlackboardDiff_PopulatedToEmpty_AllRemoved()
		{
			var a = new Dictionary<string, string> { { "x", "1" }, { "y", "2" } };
			var b = new Dictionary<string, string>();

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(2, diff.Removed.Count);
			Assert.AreEqual(0, diff.Added.Count);
		}

		[Test]
		public void BlackboardDiff_MixedChanges_AllCategorized()
		{
			var a = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "speed", "5" },
				{ "temp", "abc" }
			};
			var b = new Dictionary<string, string>
			{
				{ "health", "50" },
				{ "speed", "5" },
				{ "armor", "10" }
			};

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(1, diff.Changed.Count);
			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual(1, diff.Unchanged.Count);
		}

		[Test]
		public void StateTransitionRecord_BlackboardSnapshot_IsStored()
		{
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromNodeId = "a",
				FromStateName = "StateA",
				ToNodeId = "b",
				ToStateName = "StateB",
				TriggerPortName = "onComplete",
				BlackboardSnapshot = new Dictionary<string, string>
				{
					{ "health", "100" },
					{ "name", "Player" }
				},
				BlackboardTypes = new Dictionary<string, string>
				{
					{ "health", "Int32" },
					{ "name", "String" }
				}
			};

			Assert.IsNotNull(record.BlackboardSnapshot);
			Assert.AreEqual(2, record.BlackboardSnapshot.Count);
			Assert.AreEqual("100", record.BlackboardSnapshot["health"]);
			Assert.AreEqual("onComplete", record.TriggerPortName);
			Assert.AreEqual("Int32", record.BlackboardTypes["health"]);
		}

		[Test]
		public void StateTransitionRecord_GetDuration_CalculatesCorrectly()
		{
			var earlier = new StateTransitionRecord
			{
				Timestamp = new DateTime(2026, 1, 1, 12, 0, 0)
			};
			var later = new StateTransitionRecord
			{
				Timestamp = new DateTime(2026, 1, 1, 12, 0, 5)
			};

			var duration = later.GetDuration(earlier);

			Assert.AreEqual(TimeSpan.FromSeconds(5), duration);
		}

		[Test]
		public void StateTransitionRecord_GetDuration_NullPrevious_ReturnsZero()
		{
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now
			};

			Assert.AreEqual(TimeSpan.Zero, record.GetDuration(null));
		}

		[Test]
		public void StateTransitionRecord_TriggerPortName_NullByDefault()
		{
			var record = new StateTransitionRecord();

			Assert.IsNull(record.TriggerPortName);
		}

		[Test]
		public void StateTransitionRecord_BlackboardSnapshot_NullByDefault()
		{
			var record = new StateTransitionRecord();

			Assert.IsNull(record.BlackboardSnapshot);
			Assert.IsNull(record.BlackboardTypes);
		}

		[Test]
		public void BlackboardDiff_SymmetricChanges_DetectedCorrectly()
		{
			var a = new Dictionary<string, string> { { "x", "1" } };
			var b = new Dictionary<string, string> { { "y", "2" } };

			var diff = BlackboardDiffUtility.ComputeDiff(a, b);

			Assert.IsTrue(diff.HasChanges);
			Assert.AreEqual(1, diff.Added.Count);
			Assert.AreEqual(1, diff.Removed.Count);
			Assert.AreEqual(0, diff.Changed.Count);
			Assert.AreEqual(0, diff.Unchanged.Count);
			Assert.AreEqual("y", diff.Added[0].Key);
			Assert.AreEqual("x", diff.Removed[0].Key);
		}

		[Test]
		public void DebugTimelinePanel_InheritsFloatingPanel()
		{
			Assert.IsTrue(typeof(Nonatomic.VSM.Editor.Graph.FloatingPanel)
				.IsAssignableFrom(typeof(Nonatomic.VSM.Editor.Graph.DebugTimelinePanel)));
		}
	}
}

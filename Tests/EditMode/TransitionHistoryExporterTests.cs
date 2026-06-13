using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Runtime;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class TransitionHistoryExporterTests
	{
		[Test]
		public void ExportToJson_NullInstance_ReturnsEmptyObject()
		{
			var json = TransitionHistoryExporter.ExportToJson(null);

			Assert.AreEqual("{}", json);
		}

		[Test]
		public void ExportToJson_EmptyHistory_ReturnsEmptyTransitionsArray()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"transitions\": ["));
			Assert.IsTrue(json.Contains("]"));
			Assert.IsFalse(json.Contains("\"timestamp\""));
		}

		[Test]
		public void ExportToJson_IncludesAllTransitions()
		{
			var instance = CreateInstanceWithHistory(3);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			// Should contain all 3 state names
			Assert.IsTrue(json.Contains("\"stateName\": \"State0\""));
			Assert.IsTrue(json.Contains("\"stateName\": \"State1\""));
			Assert.IsTrue(json.Contains("\"stateName\": \"State2\""));
		}

		[Test]
		public void ExportToJson_ProducesValidStructure()
		{
			var instance = CreateInstanceWithHistory(2);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"instanceId\""));
			Assert.IsTrue(json.Contains("\"graphName\""));
			Assert.IsTrue(json.Contains("\"ownerName\""));
			Assert.IsTrue(json.Contains("\"exportedAt\""));
			Assert.IsTrue(json.Contains("\"transitions\""));
			Assert.IsTrue(json.Contains("\"from\""));
			Assert.IsTrue(json.Contains("\"to\""));
			Assert.IsTrue(json.Contains("\"triggerPort\""));
			Assert.IsTrue(json.Contains("\"blackboard\""));
		}

		[Test]
		public void ExportToJson_IncludesBlackboardSnapshot()
		{
			var instance = CreateInstanceWithHistory(1);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"Health\""));
			Assert.IsTrue(json.Contains("\"100\""));
		}

		[Test]
		public void ExportToJson_IncludesTriggerPortName()
		{
			var instance = CreateInstanceWithHistory(1);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"triggerPort\": \"Done\""));
		}

		[Test]
		public void ExportToJson_IncludesDurationFromPrevious()
		{
			var instance = CreateInstanceWithHistory(2);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"durationFromPrevious\""));
		}

		[Test]
		public void ExportToJson_EscapesSpecialCharacters()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);
			var history = (List<StateTransitionRecord>)historyField.GetValue(instance);

			history.Add(new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromStateName = "State \"A\"",
				ToStateName = "State\\B",
				ToNodeId = "node0",
				BlackboardSnapshot = new Dictionary<string, string>()
			});

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("State \\\"A\\\""));
			Assert.IsTrue(json.Contains("State\\\\B"));
		}

		[Test]
		public void ExportToJson_NullBlackboardSnapshot_ProducesEmptyBlackboard()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);
			var history = (List<StateTransitionRecord>)historyField.GetValue(instance);

			history.Add(new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				ToNodeId = "node0",
				ToStateName = "StateA",
				BlackboardSnapshot = null
			});

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"blackboard\": {"));
			Assert.IsTrue(json.Contains("}"));
		}

		[Test]
		public void ExportToJson_NullFromNodeId_UsesEmptyString()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);
			var history = (List<StateTransitionRecord>)historyField.GetValue(instance);

			history.Add(new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromNodeId = null,
				FromStateName = null,
				ToNodeId = "node0",
				ToStateName = "StateA",
				BlackboardSnapshot = new Dictionary<string, string>()
			});

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"nodeId\": \"\""));
			Assert.IsTrue(json.Contains("\"stateName\": \"Start\""));
		}

		[Test]
		public void ExportToJson_FirstTransition_DurationIsNull()
		{
			var instance = CreateInstanceWithHistory(1);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"durationFromPrevious\": null"));
		}

		[Test]
		public void ExportToJson_MultipleTransitions_LastHasNonNullDuration()
		{
			var instance = CreateInstanceWithHistory(2);

			var json = TransitionHistoryExporter.ExportToJson(instance);

			// Second transition should have a duration string
			Assert.IsTrue(json.Contains("\"durationFromPrevious\": \""));
		}

		[Test]
		public void ExportToJson_EscapesNewlinesAndTabs()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);
			var history = (List<StateTransitionRecord>)historyField.GetValue(instance);

			history.Add(new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromStateName = "State\nWith\tSpecial",
				ToNodeId = "node0",
				ToStateName = "Normal",
				BlackboardSnapshot = new Dictionary<string, string>()
			});

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("State\\nWith\\tSpecial"));
		}

		[Test]
		public void ExportToJson_NullGraphName_UsesUnknown()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"graphName\": \"Unknown\""));
		}

		[Test]
		public void ExportToJson_NullOwnerName_UsesUnknown()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"ownerName\": \"Unknown\""));
		}

		[Test]
		public void ExportToJson_ContainsExportedAtTimestamp()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"exportedAt\":"));
		}

		[Test]
		public void ExportToJson_ContainsInstanceId()
		{
			var instance = CreateEmptyRuntimeInstanceInfo();

			var json = TransitionHistoryExporter.ExportToJson(instance);

			Assert.IsTrue(json.Contains("\"instanceId\":"));
		}

		private static RuntimeInstanceInfo CreateEmptyRuntimeInstanceInfo()
		{
			var ctor = typeof(RuntimeInstanceInfo).GetConstructor(
				BindingFlags.Public | BindingFlags.Instance,
				null,
				new[] { typeof(Nonatomic.VSM.Core.StateMachineInstance), typeof(Nonatomic.VSM.Graph.StateMachineGraph), typeof(UnityEngine.GameObject) },
				null);

			return (RuntimeInstanceInfo)ctor.Invoke(new object[] { null, null, null });
		}

		private static RuntimeInstanceInfo CreateInstanceWithHistory(int count)
		{
			var info = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);

			var history = (List<StateTransitionRecord>)historyField.GetValue(info);

			for (var i = 0; i < count; i++)
			{
				history.Add(new StateTransitionRecord
				{
					Timestamp = DateTime.Now.AddSeconds(i),
					FromNodeId = i > 0 ? $"node{i - 1}" : null,
					FromStateName = i > 0 ? $"State{i - 1}" : "Start",
					ToNodeId = $"node{i}",
					ToStateName = $"State{i}",
					TriggerPortName = "Done",
					BlackboardSnapshot = new Dictionary<string, string>
					{
						{ "Health", (100 - i * 10).ToString() }
					},
					BlackboardTypes = new Dictionary<string, string>
					{
						{ "Health", "float" }
					}
				});
			}

			return info;
		}
	}
}

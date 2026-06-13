using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Analysis;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class TransitionLoopDetectorTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_graph != null)
			{
				Object.DestroyImmediate(_graph);
			}
		}


		private StateNode AddState(string id, string displayName = null)
		{
			var state = new StateNode
			{
				Id = id,
				DisplayName = displayName ?? id
			};
			_graph.AddNode(state);
			return state;
		}

		private EntryNode AddEntry(string id = "entry")
		{
			var entry = new EntryNode { Id = id };
			_graph.AddNode(entry);
			_graph.EntryNodeId = id;
			return entry;
		}

		private ExitNode AddExit(string id = "exit")
		{
			var exit = new ExitNode { Id = id };
			_graph.AddNode(exit);
			return exit;
		}

		private void AddTransition(string sourceId, string targetId, float transitionTime = 0f)
		{
			_graph.AddConnection(new Connection
			{
				Id = $"conn_{sourceId}_{targetId}",
				SourceNodeId = sourceId,
				TargetNodeId = targetId,
				ConnectionType = ConnectionType.Transition,
				TransitionTime = transitionTime
			});
		}



		[Test]
		public void Analyze_NullGraph_ReturnsEmptyResult()
		{
			var result = TransitionLoopDetector.Analyze(null);

			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.DetectedLoops.Count);
			Assert.AreEqual(0, result.InstantTransitionChains.Count);
		}

		[Test]
		public void Analyze_EmptyGraph_ReturnsEmptyResult()
		{
			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count);
			Assert.AreEqual(0, result.InstantTransitionChains.Count);
		}

		[Test]
		public void Analyze_SimpleLoop_AtoB_BtoA_DetectsLoop()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B");
			AddTransition("B", "A");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0, "Should detect A -> B -> A loop");
			var loop = result.DetectedLoops.First();
			Assert.IsTrue(loop.NodeIds.Contains("A"));
			Assert.IsTrue(loop.NodeIds.Contains("B"));
		}

		[Test]
		public void Analyze_SelfLoop_AtoA_DetectsLoop()
		{
			AddState("A");
			AddTransition("A", "A");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0, "Should detect self-loop A -> A");
		}

		[Test]
		public void Analyze_ComplexCycle_AtoBtoCtoDtoA_DetectsLoop()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			AddState("D");
			AddTransition("A", "B");
			AddTransition("B", "C");
			AddTransition("C", "D");
			AddTransition("D", "A");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0, "Should detect A -> B -> C -> D -> A cycle");
		}

		[Test]
		public void Analyze_LinearPath_NoLoop()
		{
			AddEntry();
			AddState("A");
			AddState("B");
			AddState("C");
			AddExit();
			AddTransition("entry", "A");
			AddTransition("A", "B");
			AddTransition("B", "C");
			AddTransition("C", "exit");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count, "Linear path should have no loops");
		}

		[Test]
		public void Analyze_BranchingPath_NoLoop()
		{
			AddEntry();
			AddState("A");
			AddState("B1");
			AddState("B2");
			AddState("C");
			AddExit();
			AddTransition("entry", "A");
			AddTransition("A", "B1");
			AddTransition("A", "B2");
			AddTransition("B1", "C");
			AddTransition("B2", "C");
			AddTransition("C", "exit");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count, "Branching path without cycles should have no loops");
		}



		[Test]
		public void Analyze_InstantTransitionLoop_MarkedAsCritical()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 0f); // Instant
			AddTransition("B", "A", 0f); // Instant

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0);
			var loop = result.DetectedLoops.First();
			Assert.IsTrue(loop.IsAllInstant, "Loop with all instant transitions should be marked as instant");
			Assert.AreEqual(LoopSeverity.Critical, loop.Severity);
		}

		[Test]
		public void Analyze_DelayedTransitionLoop_MarkedAsWarning()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 1.0f); // Delayed
			AddTransition("B", "A", 1.0f); // Delayed

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0);
			var loop = result.DetectedLoops.First();
			Assert.IsFalse(loop.IsAllInstant, "Loop with delayed transitions should not be marked as instant");
			Assert.AreEqual(LoopSeverity.Warning, loop.Severity);
		}

		[Test]
		public void Analyze_MixedInstantDelayedLoop_MarkedAsWarning()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 0f);   // Instant
			AddTransition("B", "A", 0.5f); // Delayed

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0);
			var loop = result.DetectedLoops.First();
			Assert.IsFalse(loop.IsAllInstant, "Mixed loop should not be marked as all instant");
			Assert.AreEqual(LoopSeverity.Warning, loop.Severity);
		}



		[Test]
		public void Analyze_LongInstantChain_DetectsChain()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			AddState("D");
			AddState("E");
			AddTransition("A", "B", 0f);
			AddTransition("B", "C", 0f);
			AddTransition("C", "D", 0f);
			AddTransition("D", "E", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			// Chain of 5 states should be detected (length > 2)
			Assert.IsTrue(result.InstantTransitionChains.Any(c => c.Length >= 4),
				"Should detect long instant transition chain");
		}

		[Test]
		public void Analyze_InstantChainFormingCycle_DetectedAsCycle()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			AddTransition("A", "B", 0f);
			AddTransition("B", "C", 0f);
			AddTransition("C", "A", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.InstantTransitionChains.Any(c => c.FormsCycle),
				"Instant chain forming a cycle should be detected");
			Assert.IsTrue(result.HasCriticalIssues, "Cycle should be marked as critical");
		}

		[Test]
		public void Analyze_ShortInstantChain_NotReported()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			// Chains with length <= 2 should not be reported
			Assert.IsFalse(result.InstantTransitionChains.Any(c => c.Length <= 2 && !c.FormsCycle),
				"Short instant chains (length 2) should not be reported unless they form cycles");
		}



		[Test]
		public void Analyze_LoopWithEntryNode_DetectsLoop()
		{
			AddEntry();
			AddState("A");
			AddState("B");
			AddTransition("entry", "A");
			AddTransition("A", "B");
			AddTransition("B", "A"); // Loop between A and B

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Count > 0, "Should detect loop even with entry node in graph");
		}

		[Test]
		public void Analyze_GraphWithExitNodes_NoFalsePositives()
		{
			AddEntry();
			AddState("A");
			AddState("B");
			AddExit("exit1");
			AddExit("exit2");
			AddTransition("entry", "A");
			AddTransition("A", "B");
			AddTransition("B", "exit1");
			AddTransition("A", "exit2");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count, "Graph with multiple exit paths should have no loops");
		}



		[Test]
		public void IsPathAtRisk_NullPath_ReturnsFalse()
		{
			var result = TransitionLoopDetector.IsPathAtRisk(_graph, null);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsPathAtRisk_EmptyPath_ReturnsFalse()
		{
			var result = TransitionLoopDetector.IsPathAtRisk(_graph, new List<string>());

			Assert.IsFalse(result);
		}

		[Test]
		public void IsPathAtRisk_SingleNodePath_ReturnsFalse()
		{
			var result = TransitionLoopDetector.IsPathAtRisk(_graph, new List<string> { "A" });

			Assert.IsFalse(result);
		}

		[Test]
		public void IsPathAtRisk_PathWithCycle_ReturnsTrue()
		{
			var path = new List<string> { "A", "B", "C", "A" };

			var result = TransitionLoopDetector.IsPathAtRisk(_graph, path);

			Assert.IsTrue(result, "Path with repeated node should be at risk");
		}

		[Test]
		public void IsPathAtRisk_PathWithoutCycle_ReturnsFalse()
		{
			var path = new List<string> { "A", "B", "C", "D" };

			var result = TransitionLoopDetector.IsPathAtRisk(_graph, path);

			Assert.IsFalse(result, "Path without repeated nodes should not be at risk");
		}



		[Test]
		public void GetValidationIssues_CriticalLoop_ReturnsErrorSeverity()
		{
			AddState("A", "StateA");
			AddState("B", "StateB");
			AddTransition("A", "B", 0f);
			AddTransition("B", "A", 0f);

			var analysisResult = TransitionLoopDetector.Analyze(_graph);
			var issues = TransitionLoopDetector.GetValidationIssues(analysisResult, _graph);

			Assert.IsTrue(issues.Any(i => i.Severity == GraphValidationSeverity.Error),
				"Critical loop should produce error-level issue");
		}

		[Test]
		public void GetValidationIssues_WarningLoop_ReturnsWarningSeverity()
		{
			AddState("A", "StateA");
			AddState("B", "StateB");
			AddTransition("A", "B", 1.0f);
			AddTransition("B", "A", 1.0f);

			var analysisResult = TransitionLoopDetector.Analyze(_graph);
			var issues = TransitionLoopDetector.GetValidationIssues(analysisResult, _graph);

			Assert.IsTrue(issues.Any(i => i.Severity == GraphValidationSeverity.Warning),
				"Warning loop should produce warning-level issue");
		}

		[Test]
		public void GetValidationIssues_InstantCycle_ReturnsErrorSeverity()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			AddTransition("A", "B", 0f);
			AddTransition("B", "C", 0f);
			AddTransition("C", "A", 0f);

			var analysisResult = TransitionLoopDetector.Analyze(_graph);
			var issues = TransitionLoopDetector.GetValidationIssues(analysisResult, _graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("cycle")),
				"Instant cycle should produce error with 'cycle' in message");
		}

		[Test]
		public void GetValidationIssues_LongInstantChain_ReturnsWarning()
		{
			// Create chain of 7 states (> 5)
			AddState("S1");
			AddState("S2");
			AddState("S3");
			AddState("S4");
			AddState("S5");
			AddState("S6");
			AddState("S7");
			AddTransition("S1", "S2", 0f);
			AddTransition("S2", "S3", 0f);
			AddTransition("S3", "S4", 0f);
			AddTransition("S4", "S5", 0f);
			AddTransition("S5", "S6", 0f);
			AddTransition("S6", "S7", 0f);

			var analysisResult = TransitionLoopDetector.Analyze(_graph);
			var issues = TransitionLoopDetector.GetValidationIssues(analysisResult, _graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("Long instant transition chain")),
				"Long instant chain should produce warning about performance");
		}

		[Test]
		public void GetValidationIssues_IncludesNodeDisplayNames()
		{
			AddState("node1", "MyFirstState");
			AddState("node2", "MySecondState");
			AddTransition("node1", "node2", 0f);
			AddTransition("node2", "node1", 0f);

			var analysisResult = TransitionLoopDetector.Analyze(_graph);
			var issues = TransitionLoopDetector.GetValidationIssues(analysisResult, _graph);

			var message = issues.FirstOrDefault()?.Message ?? "";
			Assert.IsTrue(message.Contains("MyFirstState") || message.Contains("MySecondState"),
				"Validation message should include node display names");
		}



		[Test]
		public void LoopAnalysisResult_HasCriticalIssues_TrueForCriticalLoop()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 0f);
			AddTransition("B", "A", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.HasCriticalIssues);
		}

		[Test]
		public void LoopAnalysisResult_HasCriticalIssues_FalseForWarningOnly()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 1.0f);
			AddTransition("B", "A", 1.0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsFalse(result.HasCriticalIssues,
				"Result with only warning-level loops should not have critical issues");
		}

		[Test]
		public void LoopAnalysisResult_HasCriticalIssues_TrueForCyclingChain()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			AddTransition("A", "B", 0f);
			AddTransition("B", "C", 0f);
			AddTransition("C", "A", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.HasCriticalIssues,
				"Cycling instant chain should be marked as critical");
		}



		[Test]
		public void Analyze_MultipleLoops_DetectsAll()
		{
			// Create two separate loops
			AddState("A1");
			AddState("B1");
			AddTransition("A1", "B1");
			AddTransition("B1", "A1");

			AddState("A2");
			AddState("B2");
			AddTransition("A2", "B2");
			AddTransition("B2", "A2");

			var result = TransitionLoopDetector.Analyze(_graph);

			// Should detect loops from both pairs
			Assert.IsTrue(result.DetectedLoops.Count >= 2, "Should detect multiple independent loops");
		}

		[Test]
		public void Analyze_NestedLoops_DetectsInnerLoop()
		{
			// Outer: A -> B -> C -> A
			// Inner: B -> D -> B
			AddState("A");
			AddState("B");
			AddState("C");
			AddState("D");
			AddTransition("A", "B");
			AddTransition("B", "C");
			AddTransition("C", "A");
			AddTransition("B", "D");
			AddTransition("D", "B");

			var result = TransitionLoopDetector.Analyze(_graph);

			// Should detect both loops
			Assert.IsTrue(result.DetectedLoops.Count >= 1, "Should detect nested loops");
		}

		[Test]
		public void Analyze_DiamondPattern_NoLoop()
		{
			// Diamond: Entry -> A, A -> B, A -> C, B -> D, C -> D, D -> Exit
			AddEntry();
			AddState("A");
			AddState("B");
			AddState("C");
			AddState("D");
			AddExit();
			AddTransition("entry", "A");
			AddTransition("A", "B");
			AddTransition("A", "C");
			AddTransition("B", "D");
			AddTransition("C", "D");
			AddTransition("D", "exit");

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count,
				"Diamond pattern (converging paths) should not be detected as loop");
		}



		[Test]
		public void Analyze_DisconnectedNodes_NoFalsePositives()
		{
			AddState("A");
			AddState("B");
			AddState("C");
			// No connections between states

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count);
			Assert.AreEqual(0, result.InstantTransitionChains.Count);
		}

		[Test]
		public void Analyze_DataConnections_Ignored()
		{
			AddState("A");
			AddState("B");
			// Add data connection instead of transition
			_graph.AddConnection(new Connection
			{
				Id = "data_conn",
				SourceNodeId = "A",
				TargetNodeId = "B",
				ConnectionType = ConnectionType.Data
			});

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.AreEqual(0, result.DetectedLoops.Count,
				"Data connections should not be considered for loop detection");
		}

		[Test]
		public void Analyze_ZeroTransitionTime_TreatedAsInstant()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", 0f);
			AddTransition("B", "A", 0f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Any(l => l.IsAllInstant),
				"Zero transition time should be treated as instant");
		}

		[Test]
		public void Analyze_NegativeTransitionTime_TreatedAsInstant()
		{
			AddState("A");
			AddState("B");
			AddTransition("A", "B", -1f);
			AddTransition("B", "A", -1f);

			var result = TransitionLoopDetector.Analyze(_graph);

			Assert.IsTrue(result.DetectedLoops.Any(l => l.IsAllInstant),
				"Negative transition time should be treated as instant");
		}

	}
}

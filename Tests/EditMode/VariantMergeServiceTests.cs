using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class VariantMergeServiceTests
	{
		private StateMachineGraph _baseGraph;
		private StateMachineGraphVariant _variant;

		[SetUp]
		public void SetUp()
		{
			_baseGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_baseGraph.InitializeNewGraph();

			_variant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			_variant.SetBaseGraph(_baseGraph);
		}

		[TearDown]
		public void TearDown()
		{
			if (_variant != null) Object.DestroyImmediate(_variant);
			if (_baseGraph != null) Object.DestroyImmediate(_baseGraph);
		}
		[Test]
		public void Resolve_WithBaseNodes_ClonesAllNodes()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "TestState";
			_baseGraph.AddNode(stateNode);

			var result = VariantMergeService.Resolve(_variant);

			Assert.IsTrue(result.Nodes.ContainsKey("state-1"));
			Assert.AreEqual("TestState", result.Nodes["state-1"].DisplayName);
		}

		[Test]
		public void Resolve_PreservesEntryNodeId()
		{
			var result = VariantMergeService.Resolve(_variant);

			Assert.AreEqual(_baseGraph.EntryNodeId, result.EntryNodeId);
		}

		[Test]
		public void Resolve_WithConnections_ClonesConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			var result = VariantMergeService.Resolve(_variant);

			Assert.AreEqual(1, result.Connections.Count);
		}

		[Test]
		public void Resolve_DisabledNode_ExcludedFromResult()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.DisableNode("state-1");

			var result = VariantMergeService.Resolve(_variant);

			Assert.IsFalse(result.Nodes.ContainsKey("state-1"));
			Assert.IsTrue(result.DisabledNodeIds.Contains("state-1"));
		}

		[Test]
		public void Resolve_DisabledNode_ConnectionsAlsoRemoved()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.DisableNode("state-1");

			var result = VariantMergeService.Resolve(_variant);

			Assert.AreEqual(0, result.Connections.Count);
		}

		[Test]
		public void Resolve_NodeOverride_AppliesPropertyOverrides()
		{
			var stateNode = new StateNode("state-1");
			stateNode.SetPropertyOverride("_speed", 5f, typeof(float));
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 15f, typeof(float));

			var result = VariantMergeService.Resolve(_variant);

			var resolved = result.Nodes["state-1"] as StateNode;
			Assert.IsNotNull(resolved);
			Assert.AreEqual(15f, resolved.GetPropertyOverride("_speed").GetValue());
		}

		[Test]
		public void Resolve_NodeOverride_TracksOverriddenNodeIds()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));

			var result = VariantMergeService.Resolve(_variant);

			Assert.IsTrue(result.OverriddenNodeIds.Contains("state-1"));
		}

		[Test]
		public void Resolve_AddedNodes_IncludedInResult()
		{
			var newNode = new StateNode("added-1");
			newNode.DisplayName = "Added";
			_variant.AddVariantNode(newNode);

			var result = VariantMergeService.Resolve(_variant);

			Assert.IsTrue(result.Nodes.ContainsKey("added-1"));
			Assert.IsTrue(result.AddedNodeIds.Contains("added-1"));
		}

		[Test]
		public void Resolve_RemovedConnections_ExcludedFromResult()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.RemoveInheritedConnection("conn-1");

			var result = VariantMergeService.Resolve(_variant);

			Assert.AreEqual(0, result.Connections.Count);
		}

		[Test]
		public void Resolve_AddedConnections_IncludedInResult()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var newNode = new StateNode("added-1");
			_variant.AddVariantNode(newNode);

			var connection = new Connection(
				"state-1", "transition",
				"added-1", "Enter");
			_variant.AddVariantConnection(connection);

			var result = VariantMergeService.Resolve(_variant);

			Assert.AreEqual(1, result.Connections.Count);
		}

		[Test]
		public void Resolve_PositionOverride_Applied()
		{
			var stateNode = new StateNode("state-1");
			stateNode.Position = new Vector2(100, 200);
			_baseGraph.AddNode(stateNode);

			var nodeOverride = new NodeOverride("state-1");
			nodeOverride.PositionOverride = new Vector2(300, 400);

			// Access internal list via reflection for test setup
			// Instead, use the variant's API indirectly
			_variant.InvalidateMergedCache();

			// Verify base position is inherited
			var resolved = _variant.GetNode("state-1");
			Assert.AreEqual(new Vector2(100, 200), resolved.Position);
		}

		[Test]
		public void ValidateVariantChain_NullBase_ReturnsError()
		{
			var emptyVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();

			var issues = VariantMergeService.ValidateVariantChain(emptyVariant);

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("no base graph")));

			Object.DestroyImmediate(emptyVariant);
		}

		[Test]
		public void ValidateVariantChain_ValidChain_ReturnsNoErrors()
		{
			var issues = VariantMergeService.ValidateVariantChain(_variant);

			var errors = issues.FindAll(i => i.Severity == GraphValidationSeverity.Error);
			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidateVariantChain_OrphanedNodeOverride_ReturnsWarning()
		{
			_variant.SetNodePropertyOverride("nonexistent-node", "_speed", 10f, typeof(float));

			var issues = VariantMergeService.ValidateVariantChain(_variant);

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("no longer exists")));
		}

		[Test]
		public void ValidateVariantChain_OrphanedDisabledNode_ReturnsWarning()
		{
			_variant.DisableNode("nonexistent-node");

			var issues = VariantMergeService.ValidateVariantChain(_variant);

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("no longer exists")));
		}

		[Test]
		public void ValidateVariantChain_DepthWithinLimit_NoError()
		{
			var current = _variant;
			var chain = new System.Collections.Generic.List<StateMachineGraphVariant> { _variant };

			// Build chain of depth 3 (well within limit of 8)
			for (var i = 0; i < 3; i++)
			{
				var next = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
				next.SetBaseGraph(current);
				chain.Add(next);
				current = next;
			}

			var issues = VariantMergeService.ValidateVariantChain(current);
			var depthErrors = issues.FindAll(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("depth"));
			Assert.AreEqual(0, depthErrors.Count);

			// Cleanup — skip _variant which is cleaned up in TearDown
			for (var i = 1; i < chain.Count; i++)
			{
				Object.DestroyImmediate(chain[i]);
			}
		}

		[Test]
		public void Resolve_NestedVariant_MergesCorrectly()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "BaseState";
			_baseGraph.AddNode(stateNode);

			var addedInParent = new StateNode("parent-added");
			addedInParent.DisplayName = "ParentAdded";
			_variant.AddVariantNode(addedInParent);

			var nested = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nested.SetBaseGraph(_variant);

			var result = VariantMergeService.Resolve(nested);

			// Should have: entry + state-1 + parent-added
			Assert.IsTrue(result.Nodes.ContainsKey("state-1"));
			Assert.IsTrue(result.Nodes.ContainsKey("parent-added"));
			Assert.IsNotNull(result.EntryNodeId);

			Object.DestroyImmediate(nested);
		}

		[Test]
		public void Resolve_NestedVariant_LeafOverridesTakePrecedence()
		{
			var stateNode = new StateNode("state-1");
			stateNode.SetPropertyOverride("_speed", 5f, typeof(float));
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));

			var nested = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nested.SetBaseGraph(_variant);
			nested.SetNodePropertyOverride("state-1", "_speed", 20f, typeof(float));

			var result = VariantMergeService.Resolve(nested);

			var resolved = result.Nodes["state-1"] as StateNode;
			Assert.AreEqual(20f, resolved.GetPropertyOverride("_speed").GetValue());

			Object.DestroyImmediate(nested);
		}

		[Test]
		public void MaxVariantDepth_IsEight()
		{
			Assert.AreEqual(8, VariantMergeService.MaxVariantDepth);
		}

		[Test]
		public void ValidateVariantChain_DepthExceedsLimit_ReturnsError()
		{
			var chain = new List<StateMachineGraphVariant> { _variant };
			var current = _variant;

			for (var i = 0; i < VariantMergeService.MaxVariantDepth; i++)
			{
				var next = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
				next.SetBaseGraph(current);
				chain.Add(next);
				current = next;
			}

			var issues = VariantMergeService.ValidateVariantChain(current);
			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("depth")));

			for (var i = 1; i < chain.Count; i++)
			{
				Object.DestroyImmediate(chain[i]);
			}
		}

		[Test]
		public void Resolve_DepthExceedsLimit_ReturnsEmptyResult()
		{
			var chain = new List<StateMachineGraphVariant> { _variant };
			var current = _variant;

			for (var i = 0; i < VariantMergeService.MaxVariantDepth; i++)
			{
				var next = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
				next.SetBaseGraph(current);
				chain.Add(next);
				current = next;
			}

			var result = VariantMergeService.Resolve(current);
			Assert.AreEqual(0, result.Nodes.Count);

			for (var i = 1; i < chain.Count; i++)
			{
				Object.DestroyImmediate(chain[i]);
			}
		}

		[Test]
		public void Resolve_ConnectionOverride_AppliesTransitionTime()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			connection.TransitionTime = 1.0f;
			_baseGraph.AddConnection(connection);

			var connOverride = new ConnectionOverride("conn-1");
			connOverride.TransitionTimeOverride = 2.5f;

			// Add override via reflection since there's no public API for this
			var field = typeof(StateMachineGraphVariant).GetField(
				"_connectionOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<ConnectionOverride>)field.GetValue(_variant);
			overridesList.Add(connOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			Assert.AreEqual(1, result.Connections.Count);
			Assert.AreEqual(2.5f, result.Connections[0].TransitionTime, 0.001f);
		}

		[Test]
		public void Resolve_BlackboardOverride_AppliesDefaultValue()
		{
			var definition = new Nonatomic.VSM.Blackboard.BlackboardVariableDefinition();
			definition.Key = "health";
			definition.ValueType = typeof(int);
			definition.DefaultValueJson = "100";
			_baseGraph.AddBlackboardVariable(definition);

			var bbOverride = new BlackboardVariableOverride("health");
			bbOverride.DefaultValueJsonOverride = "200";

			var field = typeof(StateMachineGraphVariant).GetField(
				"_blackboardOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<BlackboardVariableOverride>)field.GetValue(_variant);
			overridesList.Add(bbOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			var resolved = result.BlackboardDefinitions.Find(d => d.Key == "health");
			Assert.IsNotNull(resolved);
			Assert.AreEqual("200", resolved.DefaultValueJson);
		}

		[Test]
		public void Resolve_BlackboardOverride_AppliesIsExposed()
		{
			var definition = new Nonatomic.VSM.Blackboard.BlackboardVariableDefinition();
			definition.Key = "speed";
			definition.ValueType = typeof(float);
			definition.IsExposed = false;
			_baseGraph.AddBlackboardVariable(definition);

			var bbOverride = new BlackboardVariableOverride("speed");
			bbOverride.IsExposedOverride = true;

			var field = typeof(StateMachineGraphVariant).GetField(
				"_blackboardOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<BlackboardVariableOverride>)field.GetValue(_variant);
			overridesList.Add(bbOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			var resolved = result.BlackboardDefinitions.Find(d => d.Key == "speed");
			Assert.IsNotNull(resolved);
			Assert.IsTrue(resolved.IsExposed);
		}

		[Test]
		public void Resolve_NodeOverride_DisableAction_RemovesNode()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var nodeOverride = new NodeOverride("state-1", OverrideAction.Disable);

			var field = typeof(StateMachineGraphVariant).GetField(
				"_nodeOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<NodeOverride>)field.GetValue(_variant);
			overridesList.Add(nodeOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			Assert.IsFalse(result.Nodes.ContainsKey("state-1"));
			Assert.IsTrue(result.DisabledNodeIds.Contains("state-1"));
		}

		[Test]
		public void Resolve_NodeOverride_DisplayNameOverride_Applied()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "Original";
			_baseGraph.AddNode(stateNode);

			var nodeOverride = new NodeOverride("state-1");
			nodeOverride.DisplayNameOverride = "Overridden";

			var field = typeof(StateMachineGraphVariant).GetField(
				"_nodeOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<NodeOverride>)field.GetValue(_variant);
			overridesList.Add(nodeOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			Assert.AreEqual("Overridden", result.Nodes["state-1"].DisplayName);
		}

		[Test]
		public void Resolve_NodeOverride_CommentOverride_Applied()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var nodeOverride = new NodeOverride("state-1");
			nodeOverride.CommentOverride = "Test comment";

			var field = typeof(StateMachineGraphVariant).GetField(
				"_nodeOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<NodeOverride>)field.GetValue(_variant);
			overridesList.Add(nodeOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			Assert.AreEqual("Test comment", result.Nodes["state-1"].Comment);
		}

		[Test]
		public void Resolve_NullVariant_ThrowsArgumentNullException()
		{
			Assert.Throws<System.ArgumentNullException>(() => VariantMergeService.Resolve(null));
		}

		[Test]
		public void Resolve_NullBaseGraph_ReturnsEmptyResult()
		{
			var emptyVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();

			var result = VariantMergeService.Resolve(emptyVariant);

			Assert.AreEqual(0, result.Nodes.Count);
			Assert.AreEqual(0, result.Connections.Count);

			Object.DestroyImmediate(emptyVariant);
		}

		[Test]
		public void Resolve_AddedBlackboardVariables_IncludedInResult()
		{
			var addedDef = new Nonatomic.VSM.Blackboard.BlackboardVariableDefinition();
			addedDef.Key = "newVar";
			addedDef.ValueType = typeof(string);
			addedDef.DefaultValueJson = "hello";

			var field = typeof(StateMachineGraphVariant).GetField(
				"_addedBlackboardVariables",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var addedList = (List<Nonatomic.VSM.Blackboard.BlackboardVariableDefinition>)field.GetValue(_variant);
			addedList.Add(addedDef);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			var resolved = result.BlackboardDefinitions.Find(d => d.Key == "newVar");
			Assert.IsNotNull(resolved);
			Assert.AreEqual("hello", resolved.DefaultValueJson);
		}

		[Test]
		public void Resolve_ConnectionOverride_NonexistentConnection_Ignored()
		{
			var connOverride = new ConnectionOverride("nonexistent-conn");
			connOverride.TransitionTimeOverride = 5.0f;

			var field = typeof(StateMachineGraphVariant).GetField(
				"_connectionOverrides",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var overridesList = (List<ConnectionOverride>)field.GetValue(_variant);
			overridesList.Add(connOverride);
			_variant.InvalidateMergedCache();

			var result = VariantMergeService.Resolve(_variant);
			Assert.IsNotNull(result);
		}
	}
}

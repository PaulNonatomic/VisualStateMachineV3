using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineGraphVariantTests
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
		public void IsVariant_ReturnsTrue()
		{
			Assert.IsTrue(_variant.IsVariant);
		}

		[Test]
		public void IsVariant_BaseGraph_ReturnsFalse()
		{
			Assert.IsFalse(_baseGraph.IsVariant);
		}

		[Test]
		public void BaseGraph_ReturnsAssignedBase()
		{
			Assert.AreEqual(_baseGraph, _variant.BaseGraph);
		}

		[Test]
		public void VariantDepth_SingleLevel_ReturnsZero()
		{
			Assert.AreEqual(0, _variant.VariantDepth);
		}

		[Test]
		public void VariantDepth_NestedVariant_ReturnsOne()
		{
			var nestedVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nestedVariant.SetBaseGraph(_variant);

			Assert.AreEqual(1, nestedVariant.VariantDepth);

			Object.DestroyImmediate(nestedVariant);
		}

		[Test]
		public void GetAllNodes_InheritsBaseNodes()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "Idle";
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			var count = 0;
			var foundState = false;
			foreach (var node in _variant.GetAllNodes())
			{
				count++;
				if (node.DisplayName == "Idle") foundState = true;
			}

			// Entry node + state node
			Assert.AreEqual(2, count);
			Assert.IsTrue(foundState);
		}

		[Test]
		public void GetNode_ReturnsInheritedNode()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "Idle";
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			var resolved = _variant.GetNode("state-1");
			Assert.IsNotNull(resolved);
			Assert.AreEqual("Idle", resolved.DisplayName);
		}

		[Test]
		public void HasNode_ReturnsTrueForInheritedNode()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			Assert.IsTrue(_variant.HasNode("state-1"));
		}

		[Test]
		public void NodeCount_IncludesInheritedNodes()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			// Entry node + state node
			Assert.AreEqual(2, _variant.NodeCount);
		}

		[Test]
		public void EntryNodeId_InheritsFromBase()
		{
			Assert.AreEqual(_baseGraph.EntryNodeId, _variant.EntryNodeId);
		}

		[Test]
		public void GetEntryNode_ReturnsInheritedEntryNode()
		{
			var entry = _variant.GetEntryNode();
			Assert.IsNotNull(entry);
		}

		[Test]
		public void GetNodesOfType_ReturnsInheritedStateNodes()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			var count = 0;
			foreach (var node in _variant.GetNodesOfType<StateNode>())
			{
				count++;
			}

			Assert.AreEqual(1, count);
		}

		[Test]
		public void Connections_InheritsBaseConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			Assert.AreEqual(1, _variant.ConnectionCount);
		}

		[Test]
		public void GetConnectionsFromNode_ReturnsInheritedConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			var count = 0;
			foreach (var conn in _variant.GetConnectionsFromNode(_baseGraph.EntryNodeId))
			{
				count++;
			}

			Assert.AreEqual(1, count);
		}

		[Test]
		public void GetFirstConnectionFromNode_ReturnsInheritedConnection()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			var first = _variant.GetFirstConnectionFromNode(_baseGraph.EntryNodeId);
			Assert.IsNotNull(first);
			Assert.AreEqual("state-1", first.TargetNodeId);
		}

		[Test]
		public void GetConnectionsToNode_ReturnsInheritedConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			var count = 0;
			foreach (var conn in _variant.GetConnectionsToNode("state-1"))
			{
				count++;
			}

			Assert.AreEqual(1, count);
		}

		[Test]
		public void DisableNode_RemovesNodeFromMergedGraph()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.DisableNode("state-1");

			Assert.IsFalse(_variant.HasNode("state-1"));
		}

		[Test]
		public void DisableNode_RemovesAssociatedConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.DisableNode("state-1");

			Assert.AreEqual(0, _variant.ConnectionCount);
		}

		[Test]
		public void EnableNode_RestoresNode()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.DisableNode("state-1");
			_variant.EnableNode("state-1");

			Assert.IsTrue(_variant.HasNode("state-1"));
		}

		[Test]
		public void IsNodeDisabled_ReturnsTrueForDisabledNode()
		{
			_variant.DisableNode("state-1");

			Assert.IsTrue(_variant.IsNodeDisabled("state-1"));
		}

		[Test]
		public void IsNodeDisabled_ReturnsFalseForNormalNode()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			Assert.IsFalse(_variant.IsNodeDisabled("state-1"));
		}

		[Test]
		public void SetNodePropertyOverride_CreatesOverride()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));

			Assert.IsTrue(_variant.IsNodeOverridden("state-1"));
		}

		[Test]
		public void SetNodePropertyOverride_AppliesInMergedGraph()
		{
			var stateNode = new StateNode("state-1");
			stateNode.SetPropertyOverride("_speed", 5f, typeof(float));
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 15f, typeof(float));

			var resolved = _variant.GetNode<StateNode>("state-1");
			Assert.IsNotNull(resolved);

			var speedOverride = resolved.GetPropertyOverride("_speed");
			Assert.IsNotNull(speedOverride);

			var value = speedOverride.GetValue();
			Assert.AreEqual(15f, value);
		}

		[Test]
		public void RemoveNodePropertyOverride_RevertsToBase()
		{
			var stateNode = new StateNode("state-1");
			stateNode.SetPropertyOverride("_speed", 5f, typeof(float));
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 15f, typeof(float));
			_variant.RemoveNodePropertyOverride("state-1", "_speed");

			Assert.IsFalse(_variant.IsNodeOverridden("state-1"));
		}

		[Test]
		public void RevertNodeOverride_RemovesAllOverrides()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));
			_variant.SetNodePropertyOverride("state-1", "_damage", 20f, typeof(float));

			_variant.RevertNodeOverride("state-1");

			Assert.IsFalse(_variant.IsNodeOverridden("state-1"));
		}

		[Test]
		public void IsNodeOverridden_ReturnsFalseForNonOverriddenNode()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			Assert.IsFalse(_variant.IsNodeOverridden("state-1"));
		}

		[Test]
		public void AddVariantNode_AppearsInMergedGraph()
		{
			var newNode = new StateNode("variant-state-1");
			newNode.DisplayName = "VariantOnly";

			_variant.AddVariantNode(newNode);

			Assert.IsTrue(_variant.HasNode("variant-state-1"));
			var resolved = _variant.GetNode("variant-state-1");
			Assert.AreEqual("VariantOnly", resolved.DisplayName);
		}

		[Test]
		public void AddVariantNode_IsNodeAdded_ReturnsTrue()
		{
			var newNode = new StateNode("variant-state-1");
			_variant.AddVariantNode(newNode);

			Assert.IsTrue(_variant.IsNodeAdded("variant-state-1"));
		}

		[Test]
		public void RemoveVariantNode_RemovesFromMergedGraph()
		{
			var newNode = new StateNode("variant-state-1");
			_variant.AddVariantNode(newNode);

			_variant.RemoveVariantNode("variant-state-1");

			Assert.IsFalse(_variant.HasNode("variant-state-1"));
		}

		[Test]
		public void AddVariantNode_IncreasesNodeCount()
		{
			var baseCount = _variant.NodeCount;

			var newNode = new StateNode("variant-state-1");
			_variant.AddVariantNode(newNode);

			Assert.AreEqual(baseCount + 1, _variant.NodeCount);
		}

		[Test]
		public void AddVariantConnection_AppearsInMergedGraph()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var newNode = new StateNode("variant-state-1");
			_variant.AddVariantNode(newNode);

			var connection = new Connection(
				"state-1", "transition",
				"variant-state-1", "Enter");
			_variant.AddVariantConnection(connection);

			Assert.AreEqual(1, _variant.ConnectionCount);
		}

		[Test]
		public void RemoveInheritedConnection_ExcludesFromMergedGraph()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.RemoveInheritedConnection("conn-1");

			Assert.AreEqual(0, _variant.ConnectionCount);
		}

		[Test]
		public void RestoreInheritedConnection_ReIncludesInMergedGraph()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.RemoveInheritedConnection("conn-1");
			_variant.RestoreInheritedConnection("conn-1");

			Assert.AreEqual(1, _variant.ConnectionCount);
		}

		[Test]
		public void BaseGraphChange_PropagatesAfterCacheInvalidation()
		{
			_variant.InvalidateMergedCache();
			var initialCount = _variant.NodeCount;

			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			Assert.AreEqual(initialCount + 1, _variant.NodeCount);
		}

		[Test]
		public void BaseGraphNewConnection_PropagatesAfterCacheInvalidation()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();
			var initialConnCount = _variant.ConnectionCount;

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			Assert.AreEqual(initialConnCount + 1, _variant.ConnectionCount);
		}

		[Test]
		public void NestedVariant_InheritsFromParentVariant()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "BaseState";
			_baseGraph.AddNode(stateNode);

			var nestedVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nestedVariant.SetBaseGraph(_variant);

			Assert.IsTrue(nestedVariant.HasNode("state-1"));
			var resolved = nestedVariant.GetNode("state-1");
			Assert.AreEqual("BaseState", resolved.DisplayName);

			Object.DestroyImmediate(nestedVariant);
		}

		[Test]
		public void NestedVariant_OverridesStack()
		{
			var stateNode = new StateNode("state-1");
			stateNode.SetPropertyOverride("_speed", 5f, typeof(float));
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));

			var nestedVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nestedVariant.SetBaseGraph(_variant);
			nestedVariant.SetNodePropertyOverride("state-1", "_speed", 20f, typeof(float));

			var resolved = nestedVariant.GetNode<StateNode>("state-1");
			var speedOverride = resolved.GetPropertyOverride("_speed");
			Assert.AreEqual(20f, speedOverride.GetValue());

			Object.DestroyImmediate(nestedVariant);
		}

		[Test]
		public void NestedVariant_DisabledNodesInParent_StaysDisabled()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.DisableNode("state-1");

			var nestedVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nestedVariant.SetBaseGraph(_variant);

			Assert.IsFalse(nestedVariant.HasNode("state-1"));

			Object.DestroyImmediate(nestedVariant);
		}

		[Test]
		public void NestedVariant_AddedNodesInParent_AreInherited()
		{
			var addedNode = new StateNode("variant-state-1");
			addedNode.DisplayName = "ParentAdded";
			_variant.AddVariantNode(addedNode);

			var nestedVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();
			nestedVariant.SetBaseGraph(_variant);

			Assert.IsTrue(nestedVariant.HasNode("variant-state-1"));

			Object.DestroyImmediate(nestedVariant);
		}

		[Test]
		public void InheritedNode_IsDeepClone_NotSameReference()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "Original";
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			var baseResolved = _baseGraph.GetNode("state-1");
			var variantResolved = _variant.GetNode("state-1");

			// Should be different object references (deep clone)
			Assert.AreNotSame(baseResolved, variantResolved);
			// But same logical data
			Assert.AreEqual(baseResolved.DisplayName, variantResolved.DisplayName);
		}

		[Test]
		public void ModifyingVariantNode_DoesNotAffectBase()
		{
			var stateNode = new StateNode("state-1");
			stateNode.DisplayName = "Original";
			_baseGraph.AddNode(stateNode);

			_variant.InvalidateMergedCache();

			var variantNode = _variant.GetNode("state-1");
			variantNode.DisplayName = "Modified";

			var baseNode = _baseGraph.GetNode("state-1");
			Assert.AreEqual("Original", baseNode.DisplayName);
		}

		[Test]
		public void NullBaseGraph_GetAllNodes_ReturnsEmpty()
		{
			var emptyVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();

			var count = 0;
			foreach (var node in emptyVariant.GetAllNodes())
			{
				count++;
			}

			Assert.AreEqual(0, count);

			Object.DestroyImmediate(emptyVariant);
		}

		[Test]
		public void NullBaseGraph_EntryNodeId_ReturnsNull()
		{
			var emptyVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();

			Assert.IsNull(emptyVariant.EntryNodeId);

			Object.DestroyImmediate(emptyVariant);
		}

		[Test]
		public void NullBaseGraph_NodeCount_ReturnsZero()
		{
			var emptyVariant = ScriptableObject.CreateInstance<StateMachineGraphVariant>();

			Assert.AreEqual(0, emptyVariant.NodeCount);

			Object.DestroyImmediate(emptyVariant);
		}

		[Test]
		public void GetConnectionToPort_ReturnsInheritedConnection()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			var result = _variant.GetConnectionToPort("state-1", "Enter");
			Assert.IsNotNull(result);
		}

		[Test]
		public void IsPortConnected_ReturnsTrueForInheritedConnection()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			Assert.IsTrue(_variant.IsPortConnected("state-1", "Enter", true));
		}

		[Test]
		public void GetConnectionsFromPort_ReturnsInheritedConnections()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			_variant.InvalidateMergedCache();

			var count = 0;
			foreach (var conn in _variant.GetConnectionsFromPort(_baseGraph.EntryNodeId, "transition"))
			{
				count++;
			}

			Assert.AreEqual(1, count);
		}

		[Test]
		public void IsConnectionAdded_ReturnsTrueForVariantConnection()
		{
			var connection = new Connection(
				"variant-conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_variant.AddVariantConnection(connection);

			Assert.IsTrue(_variant.IsConnectionAdded("variant-conn-1"));
		}

		[Test]
		public void IsConnectionAdded_ReturnsFalseForInheritedConnection()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			Assert.IsFalse(_variant.IsConnectionAdded("conn-1"));
		}

		[Test]
		public void IsConnectionRemoved_ReturnsTrueForRemovedConnection()
		{
			_variant.RemoveInheritedConnection("conn-1");

			Assert.IsTrue(_variant.IsConnectionRemoved("conn-1"));
		}

		[Test]
		public void IsConnectionRemoved_ReturnsFalseForActiveConnection()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			var connection = new Connection(
				"conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_baseGraph.AddConnection(connection);

			Assert.IsFalse(_variant.IsConnectionRemoved("conn-1"));
		}

		[Test]
		public void RemoveVariantConnection_RemovesAddedConnection()
		{
			var connection = new Connection(
				"variant-conn-1",
				_baseGraph.EntryNodeId, "transition",
				"state-1", "Enter");
			_variant.AddVariantConnection(connection);

			_variant.RemoveVariantConnection("variant-conn-1");

			Assert.IsFalse(_variant.IsConnectionAdded("variant-conn-1"));
		}

		[Test]
		public void RemoveVariantNode_AlsoRemovesConnections()
		{
			var newNode = new StateNode("variant-state-1");
			_variant.AddVariantNode(newNode);

			var connection = new Connection(
				"variant-conn-1",
				_baseGraph.EntryNodeId, "transition",
				"variant-state-1", "Enter");
			_variant.AddVariantConnection(connection);

			_variant.RemoveVariantNode("variant-state-1");

			Assert.IsFalse(_variant.HasNode("variant-state-1"));
			Assert.AreEqual(0, _variant.ConnectionCount);
		}

		[Test]
		public void DisableNode_Idempotent_DoesNotDuplicate()
		{
			_variant.DisableNode("state-1");
			_variant.DisableNode("state-1");

			Assert.AreEqual(1, _variant.DisabledNodeIds.Count);
		}

		[Test]
		public void RemoveInheritedConnection_Idempotent_DoesNotDuplicate()
		{
			_variant.RemoveInheritedConnection("conn-1");
			_variant.RemoveInheritedConnection("conn-1");

			Assert.AreEqual(1, _variant.RemovedConnectionIds.Count);
		}

		[Test]
		public void SetNodePropertyOverride_UpdatesExisting()
		{
			var stateNode = new StateNode("state-1");
			_baseGraph.AddNode(stateNode);

			_variant.SetNodePropertyOverride("state-1", "_speed", 10f, typeof(float));
			_variant.SetNodePropertyOverride("state-1", "_speed", 20f, typeof(float));

			var nodeOverride = _variant.FindNodeOverride("state-1");
			Assert.AreEqual(1, nodeOverride.PropertyOverrides.Count);
		}

		[Test]
		public void AddVariantNode_NullNode_IsIgnored()
		{
			var initialCount = _variant.NodeCount;

			_variant.AddVariantNode(null);

			Assert.AreEqual(initialCount, _variant.NodeCount);
		}

		[Test]
		public void AddVariantConnection_NullConnection_IsIgnored()
		{
			var initialCount = _variant.ConnectionCount;

			_variant.AddVariantConnection(null);

			Assert.AreEqual(initialCount, _variant.ConnectionCount);
		}
	}
}

using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class SubStateMachineNodeTests
	{
		private SubStateMachineNode _node;
		private StateMachineGraph _subGraph;

		[SetUp]
		public void SetUp()
		{
			_node = new SubStateMachineNode();
			_subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_subGraph != null)
			{
				Object.DestroyImmediate(_subGraph);
			}
		}


		[Test]
		public void Constructor_Default_SetsDisplayName()
		{
			var node = new SubStateMachineNode();

			Assert.AreEqual("Sub State Machine", node.DisplayName);
		}

		[Test]
		public void Constructor_Default_SetsDataModeToInherit()
		{
			var node = new SubStateMachineNode();

			Assert.AreEqual(SubStateMachineDataMode.Inherit, node.DataMode);
		}

		[Test]
		public void Constructor_Default_GeneratesId()
		{
			var node = new SubStateMachineNode();

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void Constructor_WithId_UsesProvidedId()
		{
			var node = new SubStateMachineNode("custom-id");

			Assert.AreEqual("custom-id", node.Id);
		}

		[Test]
		public void Constructor_WithId_SetsDisplayName()
		{
			var node = new SubStateMachineNode("custom-id");

			Assert.AreEqual("Sub State Machine", node.DisplayName);
		}

		[Test]
		public void Constructor_WithId_SetsDataModeToInherit()
		{
			var node = new SubStateMachineNode("custom-id");

			Assert.AreEqual(SubStateMachineDataMode.Inherit, node.DataMode);
		}

		[Test]
		public void Constructor_WithNullId_GeneratesId()
		{
			var node = new SubStateMachineNode(null);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}



		[Test]
		public void NodeType_ReturnsSubStateMachine()
		{
			Assert.AreEqual(GraphNodeType.SubStateMachine, _node.NodeType);
		}



		[Test]
		public void SubGraphAssetGuid_DefaultIsNull()
		{
			Assert.IsNull(_node.SubGraphAssetGuid);
		}

		[Test]
		public void SubGraphAssetGuid_CanBeSet()
		{
			var guid = "test-guid-12345";

			_node.SubGraphAssetGuid = guid;

			Assert.AreEqual(guid, _node.SubGraphAssetGuid);
		}

		[Test]
		public void SubGraphAssetGuid_CanBeSetToNull()
		{
			_node.SubGraphAssetGuid = "some-guid";

			_node.SubGraphAssetGuid = null;

			Assert.IsNull(_node.SubGraphAssetGuid);
		}

		[Test]
		public void SubGraphAssetGuid_CanBeSetToEmptyString()
		{
			_node.SubGraphAssetGuid = "";

			Assert.AreEqual("", _node.SubGraphAssetGuid);
		}



		[Test]
		public void SubGraph_DefaultIsNull()
		{
			Assert.IsNull(_node.SubGraph);
		}

		[Test]
		public void SubGraph_CanBeSet()
		{
			_node.SubGraph = _subGraph;

			Assert.AreSame(_subGraph, _node.SubGraph);
		}

		[Test]
		public void SubGraph_CanBeSetToNull()
		{
			_node.SubGraph = _subGraph;

			_node.SubGraph = null;

			Assert.IsNull(_node.SubGraph);
		}



		[Test]
		public void DataMode_DefaultIsInherit()
		{
			Assert.AreEqual(SubStateMachineDataMode.Inherit, _node.DataMode);
		}

		[Test]
		public void DataMode_CanBeSetToIsolated()
		{
			_node.DataMode = SubStateMachineDataMode.Isolated;

			Assert.AreEqual(SubStateMachineDataMode.Isolated, _node.DataMode);
		}

		[Test]
		public void DataMode_CanBeSetToShared()
		{
			_node.DataMode = SubStateMachineDataMode.Shared;

			Assert.AreEqual(SubStateMachineDataMode.Shared, _node.DataMode);
		}

		[Test]
		public void DataMode_CanBeSetBackToInherit()
		{
			_node.DataMode = SubStateMachineDataMode.Isolated;

			_node.DataMode = SubStateMachineDataMode.Inherit;

			Assert.AreEqual(SubStateMachineDataMode.Inherit, _node.DataMode);
		}



		[Test]
		public void InputMappings_DefaultIsEmptyList()
		{
			Assert.IsNotNull(_node.InputMappings);
			Assert.AreEqual(0, _node.InputMappings.Count);
		}

		[Test]
		public void InputMappings_CanAddMapping()
		{
			var mapping = new VariableMapping("parentKey", "childKey", VariableMappingDirection.ParentToChild);

			_node.InputMappings.Add(mapping);

			Assert.AreEqual(1, _node.InputMappings.Count);
			Assert.AreSame(mapping, _node.InputMappings[0]);
		}

		[Test]
		public void InputMappings_CanAddMultipleMappings()
		{
			_node.InputMappings.Add(new VariableMapping("key1", "child1", VariableMappingDirection.ParentToChild));
			_node.InputMappings.Add(new VariableMapping("key2", "child2", VariableMappingDirection.ParentToChild));

			Assert.AreEqual(2, _node.InputMappings.Count);
		}

		[Test]
		public void InputMappings_CanRemoveMapping()
		{
			var mapping = new VariableMapping("parentKey", "childKey", VariableMappingDirection.ParentToChild);
			_node.InputMappings.Add(mapping);

			_node.InputMappings.Remove(mapping);

			Assert.AreEqual(0, _node.InputMappings.Count);
		}

		[Test]
		public void InputMappings_CanClearAllMappings()
		{
			_node.InputMappings.Add(new VariableMapping("key1", "child1", VariableMappingDirection.ParentToChild));
			_node.InputMappings.Add(new VariableMapping("key2", "child2", VariableMappingDirection.ParentToChild));

			_node.InputMappings.Clear();

			Assert.AreEqual(0, _node.InputMappings.Count);
		}



		[Test]
		public void OutputMappings_DefaultIsEmptyList()
		{
			Assert.IsNotNull(_node.OutputMappings);
			Assert.AreEqual(0, _node.OutputMappings.Count);
		}

		[Test]
		public void OutputMappings_CanAddMapping()
		{
			var mapping = new VariableMapping("parentKey", "childKey", VariableMappingDirection.ChildToParent);

			_node.OutputMappings.Add(mapping);

			Assert.AreEqual(1, _node.OutputMappings.Count);
			Assert.AreSame(mapping, _node.OutputMappings[0]);
		}

		[Test]
		public void OutputMappings_CanAddMultipleMappings()
		{
			_node.OutputMappings.Add(new VariableMapping("key1", "child1", VariableMappingDirection.ChildToParent));
			_node.OutputMappings.Add(new VariableMapping("key2", "child2", VariableMappingDirection.ChildToParent));

			Assert.AreEqual(2, _node.OutputMappings.Count);
		}

		[Test]
		public void OutputMappings_CanRemoveMapping()
		{
			var mapping = new VariableMapping("parentKey", "childKey", VariableMappingDirection.ChildToParent);
			_node.OutputMappings.Add(mapping);

			_node.OutputMappings.Remove(mapping);

			Assert.AreEqual(0, _node.OutputMappings.Count);
		}



		[Test]
		public void ExposedInputs_DefaultIsEmptyList()
		{
			Assert.IsNotNull(_node.ExposedInputs);
			Assert.AreEqual(0, _node.ExposedInputs.Count);
		}

		[Test]
		public void ExposedInputs_CanAddVariable()
		{
			_node.ExposedInputs.Add("inputVar");

			Assert.AreEqual(1, _node.ExposedInputs.Count);
			Assert.AreEqual("inputVar", _node.ExposedInputs[0]);
		}

		[Test]
		public void ExposedInputs_CanAddMultipleVariables()
		{
			_node.ExposedInputs.Add("input1");
			_node.ExposedInputs.Add("input2");
			_node.ExposedInputs.Add("input3");

			Assert.AreEqual(3, _node.ExposedInputs.Count);
		}

		[Test]
		public void ExposedInputs_CanRemoveVariable()
		{
			_node.ExposedInputs.Add("inputVar");

			_node.ExposedInputs.Remove("inputVar");

			Assert.AreEqual(0, _node.ExposedInputs.Count);
		}

		[Test]
		public void ExposedInputs_CanContainDuplicates()
		{
			_node.ExposedInputs.Add("inputVar");
			_node.ExposedInputs.Add("inputVar");

			Assert.AreEqual(2, _node.ExposedInputs.Count);
		}

		[Test]
		public void ExposedInputs_ContainsReturnsTrue()
		{
			_node.ExposedInputs.Add("inputVar");

			Assert.IsTrue(_node.ExposedInputs.Contains("inputVar"));
		}

		[Test]
		public void ExposedInputs_ContainsReturnsFalse()
		{
			Assert.IsFalse(_node.ExposedInputs.Contains("nonExistent"));
		}



		[Test]
		public void ExposedOutputs_DefaultIsEmptyList()
		{
			Assert.IsNotNull(_node.ExposedOutputs);
			Assert.AreEqual(0, _node.ExposedOutputs.Count);
		}

		[Test]
		public void ExposedOutputs_CanAddVariable()
		{
			_node.ExposedOutputs.Add("outputVar");

			Assert.AreEqual(1, _node.ExposedOutputs.Count);
			Assert.AreEqual("outputVar", _node.ExposedOutputs[0]);
		}

		[Test]
		public void ExposedOutputs_CanAddMultipleVariables()
		{
			_node.ExposedOutputs.Add("output1");
			_node.ExposedOutputs.Add("output2");

			Assert.AreEqual(2, _node.ExposedOutputs.Count);
		}

		[Test]
		public void ExposedOutputs_CanRemoveVariable()
		{
			_node.ExposedOutputs.Add("outputVar");

			_node.ExposedOutputs.Remove("outputVar");

			Assert.AreEqual(0, _node.ExposedOutputs.Count);
		}



		[Test]
		public void Position_DefaultIsZero()
		{
			Assert.AreEqual(Vector2.zero, _node.Position);
		}

		[Test]
		public void Position_CanBeSet()
		{
			var position = new Vector2(100, 200);

			_node.Position = position;

			Assert.AreEqual(position, _node.Position);
		}

		[Test]
		public void DisplayName_CanBeChanged()
		{
			_node.DisplayName = "Custom Name";

			Assert.AreEqual("Custom Name", _node.DisplayName);
		}

		[Test]
		public void Comment_DefaultIsNull()
		{
			Assert.IsNull(_node.Comment);
		}

		[Test]
		public void Comment_CanBeSet()
		{
			_node.Comment = "Test comment";

			Assert.AreEqual("Test comment", _node.Comment);
		}

	}

	[TestFixture]
	public class VariableMappingTests
	{

		[Test]
		public void Constructor_Default_CreatesEmptyMapping()
		{
			var mapping = new VariableMapping();

			Assert.IsNull(mapping.ParentVariableKey);
			Assert.IsNull(mapping.ChildVariableKey);
		}

		[Test]
		public void Constructor_WithParameters_SetsAllProperties()
		{
			var mapping = new VariableMapping("parentKey", "childKey", VariableMappingDirection.ParentToChild);

			Assert.AreEqual("parentKey", mapping.ParentVariableKey);
			Assert.AreEqual("childKey", mapping.ChildVariableKey);
			Assert.AreEqual(VariableMappingDirection.ParentToChild, mapping.Direction);
		}

		[Test]
		public void Constructor_WithBidirectional_SetsDirection()
		{
			var mapping = new VariableMapping("parent", "child", VariableMappingDirection.Bidirectional);

			Assert.AreEqual(VariableMappingDirection.Bidirectional, mapping.Direction);
		}



		[Test]
		public void ParentVariableKey_CanBeSet()
		{
			var mapping = new VariableMapping();

			mapping.ParentVariableKey = "newKey";

			Assert.AreEqual("newKey", mapping.ParentVariableKey);
		}

		[Test]
		public void ParentVariableKey_CanBeSetToNull()
		{
			var mapping = new VariableMapping("key", "child", VariableMappingDirection.ParentToChild);

			mapping.ParentVariableKey = null;

			Assert.IsNull(mapping.ParentVariableKey);
		}

		[Test]
		public void ChildVariableKey_CanBeSet()
		{
			var mapping = new VariableMapping();

			mapping.ChildVariableKey = "newChild";

			Assert.AreEqual("newChild", mapping.ChildVariableKey);
		}

		[Test]
		public void ChildVariableKey_CanBeSetToNull()
		{
			var mapping = new VariableMapping("parent", "child", VariableMappingDirection.ParentToChild);

			mapping.ChildVariableKey = null;

			Assert.IsNull(mapping.ChildVariableKey);
		}

		[Test]
		public void Direction_CanBeChanged()
		{
			var mapping = new VariableMapping("parent", "child", VariableMappingDirection.ParentToChild);

			mapping.Direction = VariableMappingDirection.ChildToParent;

			Assert.AreEqual(VariableMappingDirection.ChildToParent, mapping.Direction);
		}

	}

	[TestFixture]
	public class SubStateMachineDataModeTests
	{
		[Test]
		public void Inherit_HasExpectedValue()
		{
			Assert.AreEqual(0, (int)SubStateMachineDataMode.Inherit);
		}

		[Test]
		public void Isolated_HasExpectedValue()
		{
			Assert.AreEqual(1, (int)SubStateMachineDataMode.Isolated);
		}

		[Test]
		public void Shared_HasExpectedValue()
		{
			Assert.AreEqual(2, (int)SubStateMachineDataMode.Shared);
		}
	}

	[TestFixture]
	public class VariableMappingDirectionTests
	{
		[Test]
		public void ParentToChild_HasExpectedValue()
		{
			Assert.AreEqual(0, (int)VariableMappingDirection.ParentToChild);
		}

		[Test]
		public void ChildToParent_HasExpectedValue()
		{
			Assert.AreEqual(1, (int)VariableMappingDirection.ChildToParent);
		}

		[Test]
		public void Bidirectional_HasExpectedValue()
		{
			Assert.AreEqual(2, (int)VariableMappingDirection.Bidirectional);
		}
	}
}

using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class SubStateMachineNodeViewTests
	{
		[Test]
		public void SubStateMachineNodeView_Construction_SetsNodeData()
		{
			var subNode = new SubStateMachineNode();

			var view = new SubStateMachineNodeView(subNode, null);

			Assert.AreEqual(subNode, view.NodeData);
		}

		[Test]
		public void SubStateMachineNodeView_Construction_SetsTitleToSubStateMachine()
		{
			var subNode = new SubStateMachineNode();

			var view = new SubStateMachineNodeView(subNode, null);

			Assert.AreEqual("Sub State Machine", view.title);
		}

		[Test]
		public void SubStateMachineNodeView_Construction_HasCssClass()
		{
			var subNode = new SubStateMachineNode();

			var view = new SubStateMachineNodeView(subNode, null);

			Assert.IsTrue(view.ClassListContains("sub-state-machine-node"));
		}

		[Test]
		public void SubStateMachineNodeView_GetInputPort_DefaultName_ReturnsFlowInput()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetInputPort("Enter");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void SubStateMachineNodeView_GetInputPort_SubGraphPort_ReturnsSubGraphInput()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetInputPort("SubGraph");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void SubStateMachineNodeView_FlowInputPort_AcceptsMultipleConnections()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Port.Capacity.Multi, port.capacity);
		}

		[Test]
		public void SubStateMachineNodeView_SubGraphInputPort_IsSingleCapacity()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetInputPort("SubGraph");

			Assert.AreEqual(Port.Capacity.Single, port.capacity);
		}

		[Test]
		public void SubStateMachineNodeView_GetOutputPort_ReturnsFlowOutput()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetOutputPort("Exit");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void SubStateMachineNodeView_FlowOutputPort_IsSingleCapacity()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var port = view.GetOutputPort("Exit");

			Assert.AreEqual(Port.Capacity.Single, port.capacity);
		}

		[Test]
		public void SubStateMachineNodeView_SetActiveState_DoesNotThrow()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			Assert.DoesNotThrow(() => view.SetActiveState(true));
			Assert.DoesNotThrow(() => view.SetActiveState(false));
		}

		[Test]
		public void SubStateMachineNodeView_FlashHighlight_DoesNotThrow()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			Assert.DoesNotThrow(() => view.FlashHighlight());
		}

		[Test]
		public void SubStateMachineNodeView_UpdateInlineFieldVisibility_NullGraphView_DoesNotThrow()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			Assert.DoesNotThrow(() => view.UpdateInlineFieldVisibility());
		}

		[Test]
		public void SubStateMachineNodeView_INodeDataProvider_ReturnsNodeData()
		{
			var subNode = new SubStateMachineNode();
			var view = new SubStateMachineNodeView(subNode, null);

			var provider = (INodeDataProvider)view;

			Assert.AreEqual(subNode, provider.NodeData);
		}
	}
}

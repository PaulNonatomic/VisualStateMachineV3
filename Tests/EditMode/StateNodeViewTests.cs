using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateNodeViewTests
	{
		private class SimpleState : State
		{
			[Transition("On Complete")] public Action onComplete;
		}

		private class DataPortState : State
		{
			[DataIn("Speed")] public float speed;
			[DataOut("Result")] public float result;
			[Transition("Done")] public Action onDone;
		}

		[SetUp]
		public void SetUp()
		{
			Nonatomic.VSM.Editor.Ports.PortDiscoveryService.ClearCache();
		}

		[TearDown]
		public void TearDown()
		{
			Nonatomic.VSM.Editor.Ports.PortDiscoveryService.ClearCache();
		}

		[Test]
		public void StateNodeView_Construction_SetsNodeData()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);

			var view = new StateNodeView(stateNode, null);

			Assert.AreEqual(stateNode, view.NodeData);
		}

		[Test]
		public void StateNodeView_Construction_UsesDisplayNameForTitle()
		{
			var stateNode = new StateNode { DisplayName = "Idle" };
			stateNode.StateType = typeof(SimpleState);

			var view = new StateNodeView(stateNode, null);

			Assert.AreEqual("Idle", view.title);
		}

		[Test]
		public void StateNodeView_Construction_NoDisplayName_UsesTypeName()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);

			var view = new StateNodeView(stateNode, null);

			Assert.AreEqual("SimpleState", view.title);
		}

		[Test]
		public void StateNodeView_Construction_HasStateNodeCssClass()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);

			var view = new StateNodeView(stateNode, null);

			Assert.IsTrue(view.ClassListContains("state-node"));
		}

		[Test]
		public void StateNodeView_Construction_EntryState_HasEntryStateCssClass()
		{
			var stateNode = new StateNode { IsEntryState = true };
			stateNode.StateType = typeof(SimpleState);

			var view = new StateNodeView(stateNode, null);

			Assert.IsTrue(view.ClassListContains("entry-state"));
		}

		[Test]
		public void StateNodeView_GetInputPort_Enter_ReturnsFlowPort()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetInputPort("Enter");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void StateNodeView_GetInputPort_NullOrEmpty_ReturnsFlowPort()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var portNull = view.GetInputPort(null);
			var portEmpty = view.GetInputPort("");

			Assert.IsNotNull(portNull);
			Assert.IsNotNull(portEmpty);
		}

		[Test]
		public void StateNodeView_FlowInputPort_AcceptsMultipleConnections()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Port.Capacity.Multi, port.capacity);
		}

		[Test]
		public void StateNodeView_FlowInputPort_HasFlowPortCssClass()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetInputPort("Enter");

			Assert.IsTrue(port.ClassListContains("flow-port"));
		}

		[Test]
		public void StateNodeView_WithTransitions_GetOutputPort_ReturnsTransitionPort()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetOutputPort("onComplete");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void StateNodeView_WithTransitions_TransitionPort_HasTransitionCssClass()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetOutputPort("onComplete");

			Assert.IsTrue(port.ClassListContains("transition-port"));
		}

		[Test]
		public void StateNodeView_WithDataPorts_GetInputPort_ReturnsDataPort()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(DataPortState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetInputPort("speed");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void StateNodeView_WithDataPorts_DataInputPort_HasDataInputCssClass()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(DataPortState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetInputPort("speed");

			Assert.IsTrue(port.ClassListContains("data-input-port"));
		}

		[Test]
		public void StateNodeView_WithDataPorts_GetOutputPort_ReturnsDataPort()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(DataPortState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetOutputPort("result");

			Assert.IsNotNull(port);
			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void StateNodeView_WithDataPorts_DataOutputPort_HasDataOutputCssClass()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(DataPortState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetOutputPort("result");

			Assert.IsTrue(port.ClassListContains("data-output-port"));
		}

		[Test]
		public void StateNodeView_NonExistentPort_ReturnsNull()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var port = view.GetOutputPort("nonexistent");

			Assert.IsNull(port);
		}

		[Test]
		public void StateNodeView_SetActiveState_DoesNotThrow()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			Assert.DoesNotThrow(() => view.SetActiveState(true));
			Assert.DoesNotThrow(() => view.SetActiveState(false));
		}

		[Test]
		public void StateNodeView_IsActive_InitiallyFalse()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			Assert.IsFalse(view.IsActive);
		}

		[Test]
		public void StateNodeView_FlashHighlight_DoesNotThrow()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			Assert.DoesNotThrow(() => view.FlashHighlight());
		}

		[Test]
		public void StateNodeView_UpdateInlineFieldVisibility_NullGraphView_DoesNotThrow()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(DataPortState);
			var view = new StateNodeView(stateNode, null);

			Assert.DoesNotThrow(() => view.UpdateInlineFieldVisibility());
		}

		[Test]
		public void StateNodeView_INodeDataProvider_ReturnsNodeData()
		{
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);
			var view = new StateNodeView(stateNode, null);

			var provider = (INodeDataProvider)view;

			Assert.AreEqual(stateNode, provider.NodeData);
		}
	}
}

using System;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardVariableNodeViewTests
	{
		[Test]
		public void BlackboardVariableNodeView_GetMode_Construction_SetsNodeData()
		{
			var varNode = new BlackboardVariableNode("health", typeof(float), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.AreEqual(varNode, view.NodeData);
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_TitleIsVariableKey()
		{
			var varNode = new BlackboardVariableNode("playerHealth", typeof(float), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.AreEqual("playerHealth", view.title);
		}

		[Test]
		public void BlackboardVariableNodeView_Construction_HasCssClass()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsTrue(view.ClassListContains("blackboard-variable-node"));
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_HasModeCssClass()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsTrue(view.ClassListContains("variable-mode-get"));
		}

		[Test]
		public void BlackboardVariableNodeView_SetMode_HasModeCssClass()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Set);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsTrue(view.ClassListContains("variable-mode-set"));
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_HasOutputPort()
		{
			var varNode = new BlackboardVariableNode("val", typeof(float), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsNotNull(view.GetOutputPort("val"));
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_NoInputPort()
		{
			var varNode = new BlackboardVariableNode("val", typeof(float), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsNull(view.GetInputPort("val"));
		}

		[Test]
		public void BlackboardVariableNodeView_SetMode_HasInputPort()
		{
			var varNode = new BlackboardVariableNode("val", typeof(float), BlackboardNodeMode.Set);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsNotNull(view.GetInputPort("val"));
		}

		[Test]
		public void BlackboardVariableNodeView_SetMode_NoOutputPort()
		{
			var varNode = new BlackboardVariableNode("val", typeof(float), BlackboardNodeMode.Set);

			var view = new BlackboardVariableNodeView(varNode, null);

			Assert.IsNull(view.GetOutputPort("val"));
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_OutputPort_IsOutputDirection()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);
			var port = view.GetOutputPort("val");

			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void BlackboardVariableNodeView_SetMode_InputPort_IsInputDirection()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Set);

			var view = new BlackboardVariableNodeView(varNode, null);
			var port = view.GetInputPort("val");

			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void BlackboardVariableNodeView_GetMode_OutputPort_AcceptsMultipleConnections()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Get);

			var view = new BlackboardVariableNodeView(varNode, null);
			var port = view.GetOutputPort("val");

			Assert.AreEqual(Port.Capacity.Multi, port.capacity);
		}

		[Test]
		public void BlackboardVariableNodeView_SetMode_InputPort_IsSingleCapacity()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Set);

			var view = new BlackboardVariableNodeView(varNode, null);
			var port = view.GetInputPort("val");

			Assert.AreEqual(Port.Capacity.Single, port.capacity);
		}

		[Test]
		public void BlackboardVariableNodeView_INodeDataProvider_ReturnsNodeData()
		{
			var varNode = new BlackboardVariableNode("val", typeof(int), BlackboardNodeMode.Get);
			var view = new BlackboardVariableNodeView(varNode, null);

			var provider = (INodeDataProvider)view;

			Assert.AreEqual(varNode, provider.NodeData);
		}
	}
}

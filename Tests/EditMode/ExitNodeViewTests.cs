using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ExitNodeViewTests
	{
		[Test]
		public void ExitNodeView_Construction_SetsNodeData()
		{
			var exitNode = new ExitNode { Id = "exit1" };

			var view = new ExitNodeView(exitNode);

			Assert.AreEqual(exitNode, view.NodeData);
		}

		[Test]
		public void ExitNodeView_Construction_DefaultTitle_IsExit()
		{
			var exitNode = new ExitNode();

			var view = new ExitNodeView(exitNode);

			Assert.AreEqual("Exit", view.title);
		}

		[Test]
		public void ExitNodeView_Construction_CustomExitName_UsesExitName()
		{
			var exitNode = new ExitNode { ExitName = "Success" };

			var view = new ExitNodeView(exitNode);

			Assert.AreEqual("Success", view.title);
		}

		[Test]
		public void ExitNodeView_Construction_HasExitNodeCssClass()
		{
			var exitNode = new ExitNode();

			var view = new ExitNodeView(exitNode);

			Assert.IsTrue(view.ClassListContains("exit-node"));
		}

		[Test]
		public void ExitNodeView_GetInputPort_ReturnsPort()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			var port = view.GetInputPort("Enter");

			Assert.IsNotNull(port);
		}

		[Test]
		public void ExitNodeView_InputPort_IsInputDirection()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void ExitNodeView_InputPort_HasFlowPortCssClass()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			var port = view.GetInputPort("Enter");

			Assert.IsTrue(port.ClassListContains("flow-port"));
		}

		[Test]
		public void ExitNodeView_InputPort_AcceptsMultipleConnections()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Port.Capacity.Multi, port.capacity);
		}

		[Test]
		public void ExitNodeView_GetOutputPort_ReturnsNull()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			var port = view.GetOutputPort("any");

			Assert.IsNull(port);
		}

		[Test]
		public void ExitNodeView_SetActiveState_DoesNotThrow()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			Assert.DoesNotThrow(() => view.SetActiveState(true));
			Assert.DoesNotThrow(() => view.SetActiveState(false));
		}

		[Test]
		public void ExitNodeView_ForceDeactivate_DoesNotThrow()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			view.SetActiveState(true);

			Assert.DoesNotThrow(() => view.ForceDeactivate());
		}

		[Test]
		public void ExitNodeView_FlashHighlight_DoesNotThrow()
		{
			var exitNode = new ExitNode();
			var view = new ExitNodeView(exitNode);

			Assert.DoesNotThrow(() => view.FlashHighlight());
		}
	}
}

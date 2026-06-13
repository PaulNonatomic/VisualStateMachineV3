using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalOutNodeViewTests
	{
		[Test]
		public void PortalOutNodeView_Construction_SetsNodeData()
		{
			var portalOut = new PortalOutNode(channelId: 3);

			var view = new PortalOutNodeView(portalOut);

			Assert.AreEqual(portalOut, view.NodeData);
		}

		[Test]
		public void PortalOutNodeView_Construction_SetsTitleToPortalOut()
		{
			var portalOut = new PortalOutNode();

			var view = new PortalOutNodeView(portalOut);

			Assert.AreEqual("Portal Out", view.title);
		}

		[Test]
		public void PortalOutNodeView_Construction_HasPortalOutNodeCssClass()
		{
			var portalOut = new PortalOutNode();

			var view = new PortalOutNodeView(portalOut);

			Assert.IsTrue(view.ClassListContains("portal-out-node"));
		}

		[Test]
		public void PortalOutNodeView_GetInputPort_ReturnsPort()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			var port = view.GetInputPort("Enter");

			Assert.IsNotNull(port);
		}

		[Test]
		public void PortalOutNodeView_InputPort_IsInputDirection()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Direction.Input, port.direction);
		}

		[Test]
		public void PortalOutNodeView_InputPort_HasFlowPortCssClass()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			var port = view.GetInputPort("Enter");

			Assert.IsTrue(port.ClassListContains("flow-port"));
		}

		[Test]
		public void PortalOutNodeView_InputPort_AcceptsMultipleConnections()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			var port = view.GetInputPort("Enter");

			Assert.AreEqual(Port.Capacity.Multi, port.capacity);
		}

		[Test]
		public void PortalOutNodeView_GetOutputPort_ReturnsNull()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			var port = view.GetOutputPort("any");

			Assert.IsNull(port);
		}

		[Test]
		public void PortalOutNodeView_NullGraphView_IsOrphaned()
		{
			var portalOut = new PortalOutNode(channelId: 5);
			var view = new PortalOutNodeView(portalOut);

			view.UpdateOrphanStatus();

			Assert.IsTrue(view.tooltip.Contains("No Portal In"));
		}

		[Test]
		public void PortalOutNodeView_SetActiveState_DoesNotThrow()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			Assert.DoesNotThrow(() => view.SetActiveState(true));
			Assert.DoesNotThrow(() => view.SetActiveState(false));
		}

		[Test]
		public void PortalOutNodeView_FlashHighlight_DoesNotThrow()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			Assert.DoesNotThrow(() => view.FlashHighlight());
		}

		[Test]
		public void PortalOutNodeView_IsActive_InitiallyFalse()
		{
			var portalOut = new PortalOutNode();
			var view = new PortalOutNodeView(portalOut);

			Assert.IsFalse(view.IsActive);
		}

		[Test]
		public void PortalOutNodeView_UpdateChannelDisplay_DoesNotThrow()
		{
			var portalOut = new PortalOutNode(channelId: 7);
			var view = new PortalOutNodeView(portalOut);

			Assert.DoesNotThrow(() => view.UpdateChannelDisplay());
		}

		[Test]
		public void PortalInAndOut_SameChannel_ViewsCanBeCreated()
		{
			var portalIn = new PortalInNode(channelId: 2);
			var portalOut = new PortalOutNode(channelId: 2);

			var inView = new PortalInNodeView(portalIn);
			var outView = new PortalOutNodeView(portalOut);

			Assert.AreEqual(portalIn.ChannelId, portalOut.ChannelId);
			Assert.IsNotNull(inView.GetOutputPort("Continue"));
			Assert.IsNotNull(outView.GetInputPort("Enter"));
		}
	}
}

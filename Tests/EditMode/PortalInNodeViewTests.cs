using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalInNodeViewTests
	{
		[Test]
		public void PortalInNodeView_Construction_SetsNodeData()
		{
			var portalIn = new PortalInNode(channelId: 3);

			var view = new PortalInNodeView(portalIn);

			Assert.AreEqual(portalIn, view.NodeData);
		}

		[Test]
		public void PortalInNodeView_Construction_SetsTitleToPortalIn()
		{
			var portalIn = new PortalInNode();

			var view = new PortalInNodeView(portalIn);

			Assert.AreEqual("Portal In", view.title);
		}

		[Test]
		public void PortalInNodeView_Construction_HasPortalInNodeCssClass()
		{
			var portalIn = new PortalInNode();

			var view = new PortalInNodeView(portalIn);

			Assert.IsTrue(view.ClassListContains("portal-in-node"));
		}

		[Test]
		public void PortalInNodeView_GetInputPort_ReturnsNull()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			var port = view.GetInputPort("any");

			Assert.IsNull(port);
		}

		[Test]
		public void PortalInNodeView_GetOutputPort_ReturnsPort()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			var port = view.GetOutputPort("Continue");

			Assert.IsNotNull(port);
		}

		[Test]
		public void PortalInNodeView_OutputPort_IsOutputDirection()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			var port = view.GetOutputPort("Continue");

			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void PortalInNodeView_OutputPort_HasTransitionPortCssClass()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			var port = view.GetOutputPort("Continue");

			Assert.IsTrue(port.ClassListContains("transition-port"));
		}

		[Test]
		public void PortalInNodeView_OutputPort_IsSingleCapacity()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			var port = view.GetOutputPort("Continue");

			Assert.AreEqual(Port.Capacity.Single, port.capacity);
		}

		[Test]
		public void PortalInNodeView_NullGraphView_IsOrphaned()
		{
			var portalIn = new PortalInNode(channelId: 5);
			var view = new PortalInNodeView(portalIn);

			// Without a graph view, UpdateOrphanStatus can't find matching portals
			// so the node should be considered orphaned
			view.UpdateOrphanStatus();

			Assert.IsTrue(view.tooltip.Contains("No Portal Out"));
		}

		[Test]
		public void PortalInNodeView_SetActiveState_DoesNotThrow()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			Assert.DoesNotThrow(() => view.SetActiveState(true));
			Assert.DoesNotThrow(() => view.SetActiveState(false));
		}

		[Test]
		public void PortalInNodeView_FlashHighlight_DoesNotThrow()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			Assert.DoesNotThrow(() => view.FlashHighlight());
		}

		[Test]
		public void PortalInNodeView_IsActive_InitiallyFalse()
		{
			var portalIn = new PortalInNode();
			var view = new PortalInNodeView(portalIn);

			Assert.IsFalse(view.IsActive);
		}

		[Test]
		public void PortalInNodeView_UpdateChannelDisplay_DoesNotThrow()
		{
			var portalIn = new PortalInNode(channelId: 7);
			var view = new PortalInNodeView(portalIn);

			Assert.DoesNotThrow(() => view.UpdateChannelDisplay());
		}
	}
}

using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class EntryNodeViewTests
	{
		[Test]
		public void EntryNodeView_Construction_SetsNodeData()
		{
			var entryNode = new EntryNode { Id = "entry1" };

			var view = new EntryNodeView(entryNode);

			Assert.AreEqual(entryNode, view.NodeData);
		}

		[Test]
		public void EntryNodeView_Construction_SetsTitleToEntry()
		{
			var entryNode = new EntryNode();

			var view = new EntryNodeView(entryNode);

			Assert.AreEqual("Entry", view.title);
		}

		[Test]
		public void EntryNodeView_Construction_HasEntryNodeCssClass()
		{
			var entryNode = new EntryNode();

			var view = new EntryNodeView(entryNode);

			Assert.IsTrue(view.ClassListContains("entry-node"));
		}

		[Test]
		public void EntryNodeView_Construction_IsNotDeletable()
		{
			var entryNode = new EntryNode();

			var view = new EntryNodeView(entryNode);

			Assert.IsFalse(view.capabilities.HasFlag(Capabilities.Deletable));
		}

		[Test]
		public void EntryNodeView_GetInputPort_ReturnsNull()
		{
			var entryNode = new EntryNode();
			var view = new EntryNodeView(entryNode);

			var port = view.GetInputPort("any");

			Assert.IsNull(port);
		}

		[Test]
		public void EntryNodeView_GetOutputPort_ReturnsPort()
		{
			var entryNode = new EntryNode();
			var view = new EntryNodeView(entryNode);

			var port = view.GetOutputPort("Start");

			Assert.IsNotNull(port);
		}

		[Test]
		public void EntryNodeView_OutputPort_IsOutputDirection()
		{
			var entryNode = new EntryNode();
			var view = new EntryNodeView(entryNode);

			var port = view.GetOutputPort("Start");

			Assert.AreEqual(Direction.Output, port.direction);
		}

		[Test]
		public void EntryNodeView_OutputPort_HasTransitionPortCssClass()
		{
			var entryNode = new EntryNode();
			var view = new EntryNodeView(entryNode);

			var port = view.GetOutputPort("Start");

			Assert.IsTrue(port.ClassListContains("transition-port"));
		}

		[Test]
		public void EntryNodeView_INodeDataProvider_ReturnsNodeData()
		{
			var entryNode = new EntryNode { Id = "test" };
			var view = new EntryNodeView(entryNode);

			var provider = (INodeDataProvider)view;

			Assert.AreEqual(entryNode, provider.NodeData);
		}
	}
}

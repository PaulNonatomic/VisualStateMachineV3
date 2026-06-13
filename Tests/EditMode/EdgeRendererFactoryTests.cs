using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Renderers;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class EdgeRendererFactoryTests
	{
		[Test]
		public void Create_Default_ReturnsDefaultEdgeRenderer()
		{
			var renderer = EdgeRendererFactory.Create(EdgeRendererType.Default);

			Assert.IsNotNull(renderer);
			Assert.IsInstanceOf<DefaultEdgeRenderer>(renderer);
			Assert.AreEqual(EdgeRendererType.Default, renderer.Type);
		}

		[Test]
		public void Create_Wire_ReturnsWireEdgeRenderer()
		{
			var renderer = EdgeRendererFactory.Create(EdgeRendererType.Wire);

			Assert.IsNotNull(renderer);
			Assert.IsInstanceOf<WireEdgeRenderer>(renderer);
			Assert.AreEqual(EdgeRendererType.Wire, renderer.Type);
		}

		[Test]
		public void Create_ReturnsNewInstanceEachCall()
		{
			var renderer1 = EdgeRendererFactory.Create(EdgeRendererType.Wire);
			var renderer2 = EdgeRendererFactory.Create(EdgeRendererType.Wire);

			Assert.AreNotSame(renderer1, renderer2);
		}

		[Test]
		public void Create_UnknownType_FallsBackToWireEdgeRenderer()
		{
			var renderer = EdgeRendererFactory.Create((EdgeRendererType)999);

			Assert.IsNotNull(renderer);
			Assert.IsInstanceOf<WireEdgeRenderer>(renderer);
		}

		[Test]
		public void IsRegistered_Default_ReturnsTrue()
		{
			Assert.IsTrue(EdgeRendererFactory.IsRegistered(EdgeRendererType.Default));
		}

		[Test]
		public void IsRegistered_Wire_ReturnsTrue()
		{
			Assert.IsTrue(EdgeRendererFactory.IsRegistered(EdgeRendererType.Wire));
		}

		[Test]
		public void IsRegistered_UnknownType_ReturnsFalse()
		{
			Assert.IsFalse(EdgeRendererFactory.IsRegistered((EdgeRendererType)999));
		}

		[Test]
		public void GetAvailableTypes_ContainsDefaultAndWire()
		{
			var types = EdgeRendererFactory.GetAvailableTypes().ToList();

			Assert.IsTrue(types.Contains(EdgeRendererType.Default));
			Assert.IsTrue(types.Contains(EdgeRendererType.Wire));
		}

		[Test]
		public void GetAvailableTypes_HasAtLeastTwoTypes()
		{
			var types = EdgeRendererFactory.GetAvailableTypes().ToList();

			Assert.GreaterOrEqual(types.Count, 2);
		}

		[Test]
		public void Register_CustomType_BecomesAvailable()
		{
			var customType = (EdgeRendererType)42;

			EdgeRendererFactory.Register(customType, () => new WireEdgeRenderer());

			Assert.IsTrue(EdgeRendererFactory.IsRegistered(customType));
			Assert.IsNotNull(EdgeRendererFactory.Create(customType));
		}

		[Test]
		public void Register_OverwritesExisting_WithNewFactory()
		{
			// Register a custom factory for Wire that returns DefaultEdgeRenderer
			EdgeRendererFactory.Register(EdgeRendererType.Wire, () => new DefaultEdgeRenderer());

			var renderer = EdgeRendererFactory.Create(EdgeRendererType.Wire);

			Assert.IsInstanceOf<DefaultEdgeRenderer>(renderer);

			// Restore original behavior
			EdgeRendererFactory.Register(EdgeRendererType.Wire, () => new WireEdgeRenderer());
		}
	}
}

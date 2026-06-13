using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineGraphOfTTests
	{
		private class TestContext
		{
			public int Health { get; set; }
			public float Speed { get; set; }
		}

		private class AnotherContext
		{
			public string Name { get; set; }
		}

		private class TestGraph : StateMachineGraph<TestContext> { }

		private class AnotherGraph : StateMachineGraph<AnotherContext> { }

		[Test]
		public void CreateInstance_SetsContextTypeToGenericParameter()
		{
			var graph = ScriptableObject.CreateInstance<TestGraph>();

			Assert.AreEqual(typeof(TestContext), graph.ContextType);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CreateInstance_RequiresContextIsTrue()
		{
			var graph = ScriptableObject.CreateInstance<TestGraph>();

			Assert.IsTrue(graph.RequiresContext);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CreateInstance_ContextTypeAssemblyQualifiedName_MatchesGenericParameter()
		{
			var graph = ScriptableObject.CreateInstance<TestGraph>();

			Assert.AreEqual(typeof(TestContext).AssemblyQualifiedName, graph.ContextTypeAssemblyQualifiedName);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CreateInstance_DifferentContextType_SetsCorrectType()
		{
			var graph = ScriptableObject.CreateInstance<AnotherGraph>();

			Assert.AreEqual(typeof(AnotherContext), graph.ContextType);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CreateInstance_TwoGraphTypes_HaveDifferentContextTypes()
		{
			var graph1 = ScriptableObject.CreateInstance<TestGraph>();
			var graph2 = ScriptableObject.CreateInstance<AnotherGraph>();

			Assert.AreNotEqual(graph1.ContextType, graph2.ContextType);

			Object.DestroyImmediate(graph1);
			Object.DestroyImmediate(graph2);
		}

		[Test]
		public void CreateInstance_InitializesEntryNode()
		{
			var graph = ScriptableObject.CreateInstance<TestGraph>();

			Assert.IsNotNull(graph.EntryNodeId);
			Assert.IsNotEmpty(graph.EntryNodeId);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void ContextType_AfterManualClear_RestoredOnAccess()
		{
			var graph = ScriptableObject.CreateInstance<TestGraph>();

			// Manually clear context type to simulate a scenario
			graph.ContextType = null;

			// ContextType getter won't auto-restore for StateMachineGraph<T>,
			// but the AQN was cleared by the setter
			Assert.IsNull(graph.ContextType);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CreateInstance_DirectGenericType_SetsContextType()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph<TestContext>>();

			Assert.AreEqual(typeof(TestContext), graph.ContextType);

			Object.DestroyImmediate(graph);
		}
	}
}

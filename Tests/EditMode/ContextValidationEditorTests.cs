using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ContextValidationEditorTests
	{
		private class TestContext
		{
			public int Value { get; set; }
		}

		private class DerivedContext : TestContext
		{
			public string Extra { get; set; }
		}

		private class UnrelatedContext
		{
			public float Data { get; set; }
		}

		private class PlainState : State
		{
			public override void OnEnter() { }
		}

		private class ContextState : State<TestContext>
		{
			public override void OnEnter() { }
		}

		private class DerivedContextState : State<DerivedContext>
		{
			public override void OnEnter() { }
		}

		private class IndirectContextState : ContextState
		{
		}

		[Test]
		public void GetRequiredContextType_PlainState_ReturnsNull()
		{
			var result = GetRequiredContextType(typeof(PlainState));

			Assert.IsNull(result);
		}

		[Test]
		public void GetRequiredContextType_ContextState_ReturnsContextType()
		{
			var result = GetRequiredContextType(typeof(ContextState));

			Assert.AreEqual(typeof(TestContext), result);
		}

		[Test]
		public void GetRequiredContextType_DerivedContextState_ReturnsDerivedContextType()
		{
			var result = GetRequiredContextType(typeof(DerivedContextState));

			Assert.AreEqual(typeof(DerivedContext), result);
		}

		[Test]
		public void GetRequiredContextType_IndirectInheritance_ReturnsContextType()
		{
			var result = GetRequiredContextType(typeof(IndirectContextState));

			Assert.AreEqual(typeof(TestContext), result);
		}

		[Test]
		public void GraphWithContextStates_DetectedFromStateNodes()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			var stateNode = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextState).AssemblyQualifiedName
			};
			graph.AddNode(stateNode);

			var requiredTypes = new HashSet<Type>();
			foreach (var node in graph.GetNodesOfType<StateNode>())
			{
				var stateType = node.StateType;
				if (stateType == null) continue;

				var contextType = GetRequiredContextType(stateType);
				if (contextType != null)
				{
					requiredTypes.Add(contextType);
				}
			}

			Assert.AreEqual(1, requiredTypes.Count);
			Assert.IsTrue(requiredTypes.Contains(typeof(TestContext)));

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void GraphWithPlainStates_NoContextRequired()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			var stateNode = new StateNode
			{
				Id = "plainState",
				DisplayName = "Plain State",
				StateTypeAssemblyQualifiedName = typeof(PlainState).AssemblyQualifiedName
			};
			graph.AddNode(stateNode);

			var requiredTypes = new HashSet<Type>();
			foreach (var node in graph.GetNodesOfType<StateNode>())
			{
				var stateType = node.StateType;
				if (stateType == null) continue;

				var contextType = GetRequiredContextType(stateType);
				if (contextType != null)
				{
					requiredTypes.Add(contextType);
				}
			}

			Assert.AreEqual(0, requiredTypes.Count);

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void RunnerTypeDetection_GenericRunner_DetectsContextType()
		{
			var providedType = GetRunnerContextType(typeof(TestRunner));

			Assert.AreEqual(typeof(TestContext), providedType);
		}

		[Test]
		public void RunnerTypeDetection_PlainRunner_ReturnsNull()
		{
			var providedType = GetRunnerContextType(typeof(StateMachineRunner));

			Assert.IsNull(providedType);
		}

		[Test]
		public void ContextCompatibility_MatchingTypes_IsCompatible()
		{
			var required = typeof(TestContext);
			var provided = typeof(TestContext);

			Assert.IsTrue(required.IsAssignableFrom(provided));
		}

		[Test]
		public void ContextCompatibility_DerivedProvided_IsCompatible()
		{
			var required = typeof(TestContext);
			var provided = typeof(DerivedContext);

			Assert.IsTrue(required.IsAssignableFrom(provided));
		}

		[Test]
		public void ContextCompatibility_UnrelatedType_IsIncompatible()
		{
			var required = typeof(TestContext);
			var provided = typeof(UnrelatedContext);

			Assert.IsFalse(required.IsAssignableFrom(provided));
		}

		[Test]
		public void GetRequiredContextTypeFromStates_WithContextState_ReturnsType()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();
			graph.AddNode(new StateNode
			{
				Id = "s1",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextState).AssemblyQualifiedName
			});

			var result = graph.GetRequiredContextTypeFromStates();

			Assert.AreEqual(typeof(TestContext), result);

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void GetRequiredContextTypeFromStates_WithPlainStates_ReturnsNull()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();
			graph.AddNode(new StateNode
			{
				Id = "s1",
				DisplayName = "Plain State",
				StateTypeAssemblyQualifiedName = typeof(PlainState).AssemblyQualifiedName
			});

			var result = graph.GetRequiredContextTypeFromStates();

			Assert.IsNull(result);

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void GetRequiredContextTypeFromStates_EmptyGraph_ReturnsNull()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			var result = graph.GetRequiredContextTypeFromStates();

			Assert.IsNull(result);

			UnityEngine.Object.DestroyImmediate(graph);
		}

		private class TestRunner : StateMachineRunner<TestContext>
		{
			protected override TestContext CreateSharedContext()
			{
				return new TestContext();
			}
		}

		/// <summary>
		/// Mirror of the editor's GetRequiredContextType logic for testing.
		/// </summary>
		private static Type GetRequiredContextType(Type stateType)
		{
			var baseType = stateType.BaseType;

			while (baseType != null && baseType != typeof(object))
			{
				if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(State<>))
				{
					return baseType.GetGenericArguments()[0];
				}

				baseType = baseType.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Mirror of the editor's runner type detection logic for testing.
		/// </summary>
		private static Type GetRunnerContextType(Type runnerType)
		{
			var baseType = runnerType;

			while (baseType != null && baseType != typeof(object))
			{
				if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(StateMachineRunner<>))
				{
					return baseType.GetGenericArguments()[0];
				}

				baseType = baseType.BaseType;
			}

			return null;
		}
	}
}

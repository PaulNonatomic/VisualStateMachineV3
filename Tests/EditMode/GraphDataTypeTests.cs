using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ConnectionTypeTests
	{
		[Test]
		public void HasDataValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(ConnectionType), ConnectionType.Data));
		}

		[Test]
		public void HasTransitionValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(ConnectionType), ConnectionType.Transition));
		}

		[Test]
		public void HasBlackboardReadValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(ConnectionType), ConnectionType.BlackboardRead));
		}

		[Test]
		public void HasBlackboardWriteValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(ConnectionType), ConnectionType.BlackboardWrite));
		}

		[Test]
		public void HasExactlyFourValues()
		{
			var values = Enum.GetValues(typeof(ConnectionType));

			Assert.AreEqual(4, values.Length);
		}
	}

	[TestFixture]
	public class GraphNodeTypeTests
	{
		[Test]
		public void HasStateValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.State));
		}

		[Test]
		public void HasEntryValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.Entry));
		}

		[Test]
		public void HasExitValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.Exit));
		}

		[Test]
		public void HasBlackboardVariableValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.BlackboardVariable));
		}

		[Test]
		public void HasSubStateMachineValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.SubStateMachine));
		}

		[Test]
		public void HasPortalOutValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.PortalOut));
		}

		[Test]
		public void HasPortalInValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphNodeType), GraphNodeType.PortalIn));
		}

		[Test]
		public void HasExactlyEightValues()
		{
			var values = Enum.GetValues(typeof(GraphNodeType));

			Assert.AreEqual(8, values.Length);
		}
	}

	[TestFixture]
	public class GraphValidationSeverityTests
	{
		[Test]
		public void HasInfoValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphValidationSeverity), GraphValidationSeverity.Info));
		}

		[Test]
		public void HasWarningValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphValidationSeverity), GraphValidationSeverity.Warning));
		}

		[Test]
		public void HasErrorValue()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(GraphValidationSeverity), GraphValidationSeverity.Error));
		}

		[Test]
		public void HasExactlyThreeValues()
		{
			var values = Enum.GetValues(typeof(GraphValidationSeverity));

			Assert.AreEqual(3, values.Length);
		}
	}

	[TestFixture]
	public class GraphValidationIssueTests
	{
		[Test]
		public void Constructor_SetsSeverity()
		{
			var issue = new GraphValidationIssue(GraphValidationSeverity.Error, "Test", "node1");

			Assert.AreEqual(GraphValidationSeverity.Error, issue.Severity);
		}

		[Test]
		public void Constructor_SetsMessage()
		{
			var issue = new GraphValidationIssue(GraphValidationSeverity.Warning, "Missing connection", "node1");

			Assert.AreEqual("Missing connection", issue.Message);
		}

		[Test]
		public void Constructor_SetsNodeId()
		{
			var issue = new GraphValidationIssue(GraphValidationSeverity.Info, "Test", "node-abc");

			Assert.AreEqual("node-abc", issue.NodeId);
		}

		[Test]
		public void Constructor_NullNodeId_AllowsNull()
		{
			var issue = new GraphValidationIssue(GraphValidationSeverity.Error, "Graph-wide issue", null);

			Assert.IsNull(issue.NodeId);
		}

		[Test]
		public void Constructor_NullMessage_AllowsNull()
		{
			var issue = new GraphValidationIssue(GraphValidationSeverity.Info, null, null);

			Assert.IsNull(issue.Message);
		}

		[Test]
		public void Severity_IsReadOnly()
		{
			var prop = typeof(GraphValidationIssue).GetProperty("Severity");

			Assert.IsNotNull(prop);
			Assert.IsNull(prop.GetSetMethod(true));
		}

		[Test]
		public void Message_IsReadOnly()
		{
			var prop = typeof(GraphValidationIssue).GetProperty("Message");

			Assert.IsNotNull(prop);
			Assert.IsNull(prop.GetSetMethod(true));
		}

		[Test]
		public void NodeId_IsReadOnly()
		{
			var prop = typeof(GraphValidationIssue).GetProperty("NodeId");

			Assert.IsNotNull(prop);
			Assert.IsNull(prop.GetSetMethod(true));
		}
	}

	[TestFixture]
	public class PeekPreviewStateTests
	{
		[Test]
		public void IsSerializable()
		{
			var attr = typeof(PeekPreviewState).GetCustomAttribute<SerializableAttribute>();

			Assert.IsNotNull(attr);
		}

		[Test]
		public void NodeId_DefaultsToNull()
		{
			var state = new PeekPreviewState();

			Assert.IsNull(state.NodeId);
		}

		[Test]
		public void Position_DefaultsToZero()
		{
			var state = new PeekPreviewState();

			Assert.AreEqual(Vector2.zero, state.Position);
		}

		[Test]
		public void PanOffset_DefaultsToZero()
		{
			var state = new PeekPreviewState();

			Assert.AreEqual(Vector2.zero, state.PanOffset);
		}

		[Test]
		public void ZoomLevel_DefaultsToOne()
		{
			var state = new PeekPreviewState();

			Assert.AreEqual(1f, state.ZoomLevel);
		}

		[Test]
		public void NodeId_CanBeSet()
		{
			var state = new PeekPreviewState { NodeId = "sub-machine-1" };

			Assert.AreEqual("sub-machine-1", state.NodeId);
		}

		[Test]
		public void Position_CanBeSet()
		{
			var expected = new Vector2(100f, 200f);
			var state = new PeekPreviewState { Position = expected };

			Assert.AreEqual(expected, state.Position);
		}

		[Test]
		public void PanOffset_CanBeSet()
		{
			var expected = new Vector2(-50f, 30f);
			var state = new PeekPreviewState { PanOffset = expected };

			Assert.AreEqual(expected, state.PanOffset);
		}

		[Test]
		public void ZoomLevel_CanBeSet()
		{
			var state = new PeekPreviewState { ZoomLevel = 2.5f };

			Assert.AreEqual(2.5f, state.ZoomLevel);
		}

		[Test]
		public void BackingFields_HaveSerializeFieldAttribute()
		{
			var fields = typeof(PeekPreviewState).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			var serializedCount = 0;

			foreach (var field in fields)
			{
				if (field.GetCustomAttribute<SerializeField>() != null)
				{
					serializedCount++;
				}
			}

			Assert.AreEqual(4, serializedCount);
		}
	}
}

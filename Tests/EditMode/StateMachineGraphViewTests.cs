using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using UnityEditor;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineGraphViewTests
	{
		private const string AutoCenterPrefKey = "VSM.GraphView.AutoCenterOnActiveState";
		private const string UseLerpAnimationPrefKey = "VSM.GraphView.UseLerpAnimation";
		private const string LerpDurationPrefKey = "VSM.GraphView.LerpDuration";
		private const string AutoFrameEntryOnOpenPrefKey = "VSM.GraphView.AutoFrameEntryOnOpen";
		private const string DebugModePrefKey = "VSM_DebugMode";

		[SetUp]
		public void SetUp()
		{
			EditorPrefs.DeleteKey(AutoCenterPrefKey);
			EditorPrefs.DeleteKey(UseLerpAnimationPrefKey);
			EditorPrefs.DeleteKey(LerpDurationPrefKey);
			EditorPrefs.DeleteKey(AutoFrameEntryOnOpenPrefKey);
			EditorPrefs.DeleteKey(DebugModePrefKey);
		}

		[TearDown]
		public void TearDown()
		{
			EditorPrefs.DeleteKey(AutoCenterPrefKey);
			EditorPrefs.DeleteKey(UseLerpAnimationPrefKey);
			EditorPrefs.DeleteKey(LerpDurationPrefKey);
			EditorPrefs.DeleteKey(AutoFrameEntryOnOpenPrefKey);
			EditorPrefs.DeleteKey(DebugModePrefKey);
		}

		[Test]
		public void DebugMode_DefaultsToFalse()
		{
			// Force reload of debug mode from prefs
			StateMachineGraphView.DebugMode = EditorPrefs.GetBool(DebugModePrefKey, false);

			Assert.IsFalse(StateMachineGraphView.DebugMode);
		}

		[Test]
		public void DebugMode_CanBeEnabled()
		{
			StateMachineGraphView.DebugMode = true;

			Assert.IsTrue(StateMachineGraphView.DebugMode);
		}

		[Test]
		public void DebugMode_PersistsToEditorPrefs()
		{
			StateMachineGraphView.DebugMode = true;

			Assert.IsTrue(EditorPrefs.GetBool(DebugModePrefKey, false));
		}

		[Test]
		public void DebugMode_SyncsWithRuntimeStateMachineInstance()
		{
			StateMachineGraphView.DebugMode = true;

			Assert.IsTrue(Nonatomic.VSM.Core.StateMachineInstance.DebugMode);

			StateMachineGraphView.DebugMode = false;

			Assert.IsFalse(Nonatomic.VSM.Core.StateMachineInstance.DebugMode);
		}

		[Test]
		public void ClipboardData_CanBeCreated()
		{
			var clipboardData = new ClipboardData();

			Assert.IsNotNull(clipboardData);
			Assert.IsNotNull(clipboardData.Nodes);
			Assert.IsNotNull(clipboardData.Connections);
			Assert.AreEqual(0, clipboardData.Nodes.Count);
			Assert.AreEqual(0, clipboardData.Connections.Count);
		}

		[Test]
		public void ClipboardNode_CanSetProperties()
		{
			var clipNode = new ClipboardNode
			{
				OriginalId = "test-id",
				NodeType = Nonatomic.VSM.Graph.GraphNodeType.State,
				JsonData = "{}"
			};

			Assert.AreEqual("test-id", clipNode.OriginalId);
			Assert.AreEqual(Nonatomic.VSM.Graph.GraphNodeType.State, clipNode.NodeType);
			Assert.AreEqual("{}", clipNode.JsonData);
		}

		[Test]
		public void ClipboardConnection_CanSetProperties()
		{
			var clipConn = new ClipboardConnection
			{
				SourceNodeId = "source-id",
				SourcePortName = "Output",
				TargetNodeId = "target-id",
				TargetPortName = "Input",
				ConnectionType = Nonatomic.VSM.Graph.ConnectionType.Transition
			};

			Assert.AreEqual("source-id", clipConn.SourceNodeId);
			Assert.AreEqual("Output", clipConn.SourcePortName);
			Assert.AreEqual("target-id", clipConn.TargetNodeId);
			Assert.AreEqual("Input", clipConn.TargetPortName);
			Assert.AreEqual(Nonatomic.VSM.Graph.ConnectionType.Transition, clipConn.ConnectionType);
		}
	}
}

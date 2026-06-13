using System;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardVariableOverrideTests
	{
		[Test]
		public void DefaultConstructor_HasNoOverrides()
		{
			var ovr = new BlackboardVariableOverride();

			Assert.IsNull(ovr.Key);
			Assert.IsFalse(ovr.HasDefaultValueOverride);
			Assert.IsFalse(ovr.HasIsExposedOverride);
			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void KeyConstructor_SetsKey()
		{
			var ovr = new BlackboardVariableOverride("Health");

			Assert.AreEqual("Health", ovr.Key);
		}

		[Test]
		public void SetDefaultValueJson_SetsHasOverrideFlag()
		{
			var ovr = new BlackboardVariableOverride("Health");

			ovr.DefaultValueJsonOverride = "100";

			Assert.IsTrue(ovr.HasDefaultValueOverride);
			Assert.AreEqual("100", ovr.DefaultValueJsonOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void SetIsExposed_SetsHasOverrideFlag()
		{
			var ovr = new BlackboardVariableOverride("Health");

			ovr.IsExposedOverride = true;

			Assert.IsTrue(ovr.HasIsExposedOverride);
			Assert.IsTrue(ovr.IsExposedOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void ClearDefaultValueOverride_ResetsFlag()
		{
			var ovr = new BlackboardVariableOverride("Health");
			ovr.DefaultValueJsonOverride = "100";

			ovr.ClearDefaultValueOverride();

			Assert.IsFalse(ovr.HasDefaultValueOverride);
			Assert.IsNull(ovr.DefaultValueJsonOverride);
		}

		[Test]
		public void ClearIsExposedOverride_ResetsFlag()
		{
			var ovr = new BlackboardVariableOverride("Health");
			ovr.IsExposedOverride = true;

			ovr.ClearIsExposedOverride();

			Assert.IsFalse(ovr.HasIsExposedOverride);
			Assert.IsFalse(ovr.IsExposedOverride);
		}

		[Test]
		public void HasAnyOverrides_FalseAfterClearingAll()
		{
			var ovr = new BlackboardVariableOverride("Health");
			ovr.DefaultValueJsonOverride = "100";
			ovr.IsExposedOverride = true;

			ovr.ClearDefaultValueOverride();
			ovr.ClearIsExposedOverride();

			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void Key_CanBeSetViaProperty()
		{
			var ovr = new BlackboardVariableOverride();
			ovr.Key = "Speed";

			Assert.AreEqual("Speed", ovr.Key);
		}

		[Test]
		public void IsSerializable()
		{
			Assert.IsTrue(typeof(BlackboardVariableOverride).IsDefined(typeof(SerializableAttribute), false));
		}
	}

	[TestFixture]
	public class ConnectionOverrideTests
	{
		[Test]
		public void DefaultConstructor_HasNoOverrides()
		{
			var ovr = new ConnectionOverride();

			Assert.IsNull(ovr.ConnectionId);
			Assert.IsFalse(ovr.HasTransitionTimeOverride);
			Assert.IsFalse(ovr.HasEasingCurveOverride);
			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void IdConstructor_SetsConnectionId()
		{
			var ovr = new ConnectionOverride("conn-123");

			Assert.AreEqual("conn-123", ovr.ConnectionId);
		}

		[Test]
		public void SetTransitionTime_SetsHasOverrideFlag()
		{
			var ovr = new ConnectionOverride("conn-123");

			ovr.TransitionTimeOverride = 2.5f;

			Assert.IsTrue(ovr.HasTransitionTimeOverride);
			Assert.AreEqual(2.5f, ovr.TransitionTimeOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void SetEasingCurve_SetsHasOverrideFlag()
		{
			var ovr = new ConnectionOverride("conn-123");
			var curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

			ovr.EasingCurveOverride = curve;

			Assert.IsTrue(ovr.HasEasingCurveOverride);
			Assert.IsNotNull(ovr.EasingCurveOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void ClearTransitionTimeOverride_ResetsFlag()
		{
			var ovr = new ConnectionOverride("conn-123");
			ovr.TransitionTimeOverride = 2.5f;

			ovr.ClearTransitionTimeOverride();

			Assert.IsFalse(ovr.HasTransitionTimeOverride);
			Assert.AreEqual(0f, ovr.TransitionTimeOverride);
		}

		[Test]
		public void ClearEasingCurveOverride_ResetsFlag()
		{
			var ovr = new ConnectionOverride("conn-123");
			ovr.EasingCurveOverride = AnimationCurve.Linear(0, 0, 1, 1);

			ovr.ClearEasingCurveOverride();

			Assert.IsFalse(ovr.HasEasingCurveOverride);
			Assert.IsNull(ovr.EasingCurveOverride);
		}

		[Test]
		public void HasAnyOverrides_FalseAfterClearingAll()
		{
			var ovr = new ConnectionOverride("conn-123");
			ovr.TransitionTimeOverride = 1f;
			ovr.EasingCurveOverride = AnimationCurve.Linear(0, 0, 1, 1);

			ovr.ClearTransitionTimeOverride();
			ovr.ClearEasingCurveOverride();

			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void ConnectionId_CanBeSetViaProperty()
		{
			var ovr = new ConnectionOverride();
			ovr.ConnectionId = "new-id";

			Assert.AreEqual("new-id", ovr.ConnectionId);
		}

		[Test]
		public void IsSerializable()
		{
			Assert.IsTrue(typeof(ConnectionOverride).IsDefined(typeof(SerializableAttribute), false));
		}
	}

	[TestFixture]
	public class NodeOverrideTests
	{
		[Test]
		public void DefaultConstructor_HasNoOverrides()
		{
			var ovr = new NodeOverride();

			Assert.IsNull(ovr.NodeId);
			Assert.AreEqual(OverrideAction.Modify, ovr.Action);
			Assert.AreEqual(0, ovr.PropertyOverrides.Count);
			Assert.IsFalse(ovr.HasPositionOverride);
			Assert.IsFalse(ovr.HasDisplayNameOverride);
			Assert.IsFalse(ovr.HasCommentOverride);
			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void IdConstructor_SetsNodeIdAndAction()
		{
			var ovr = new NodeOverride("node-1", OverrideAction.Replace);

			Assert.AreEqual("node-1", ovr.NodeId);
			Assert.AreEqual(OverrideAction.Replace, ovr.Action);
		}

		[Test]
		public void IdConstructor_DefaultsToModify()
		{
			var ovr = new NodeOverride("node-1");

			Assert.AreEqual(OverrideAction.Modify, ovr.Action);
		}

		[Test]
		public void SetPosition_SetsHasOverrideFlag()
		{
			var ovr = new NodeOverride("node-1");
			var pos = new Vector2(100, 200);

			ovr.PositionOverride = pos;

			Assert.IsTrue(ovr.HasPositionOverride);
			Assert.AreEqual(pos, ovr.PositionOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void SetDisplayName_SetsHasOverrideFlag()
		{
			var ovr = new NodeOverride("node-1");

			ovr.DisplayNameOverride = "Custom Name";

			Assert.IsTrue(ovr.HasDisplayNameOverride);
			Assert.AreEqual("Custom Name", ovr.DisplayNameOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void SetComment_SetsHasOverrideFlag()
		{
			var ovr = new NodeOverride("node-1");

			ovr.CommentOverride = "This is a comment";

			Assert.IsTrue(ovr.HasCommentOverride);
			Assert.AreEqual("This is a comment", ovr.CommentOverride);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void ClearPositionOverride_ResetsFlag()
		{
			var ovr = new NodeOverride("node-1");
			ovr.PositionOverride = new Vector2(100, 200);

			ovr.ClearPositionOverride();

			Assert.IsFalse(ovr.HasPositionOverride);
			Assert.AreEqual(Vector2.zero, ovr.PositionOverride);
		}

		[Test]
		public void ClearDisplayNameOverride_ResetsFlag()
		{
			var ovr = new NodeOverride("node-1");
			ovr.DisplayNameOverride = "Custom Name";

			ovr.ClearDisplayNameOverride();

			Assert.IsFalse(ovr.HasDisplayNameOverride);
			Assert.IsNull(ovr.DisplayNameOverride);
		}

		[Test]
		public void ClearCommentOverride_ResetsFlag()
		{
			var ovr = new NodeOverride("node-1");
			ovr.CommentOverride = "A comment";

			ovr.ClearCommentOverride();

			Assert.IsFalse(ovr.HasCommentOverride);
			Assert.IsNull(ovr.CommentOverride);
		}

		[Test]
		public void SetPropertyOverride_AddsNew()
		{
			var ovr = new NodeOverride("node-1");

			ovr.SetPropertyOverride("speed", 5f, typeof(float));

			Assert.AreEqual(1, ovr.PropertyOverrides.Count);
			Assert.AreEqual("speed", ovr.PropertyOverrides[0].FieldName);
			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void SetPropertyOverride_UpdatesExisting()
		{
			var ovr = new NodeOverride("node-1");
			ovr.SetPropertyOverride("speed", 5f, typeof(float));
			ovr.SetPropertyOverride("speed", 10f, typeof(float));

			Assert.AreEqual(1, ovr.PropertyOverrides.Count);
		}

		[Test]
		public void GetPropertyOverride_ReturnsCorrectOverride()
		{
			var ovr = new NodeOverride("node-1");
			ovr.SetPropertyOverride("speed", 5f, typeof(float));

			var result = ovr.GetPropertyOverride("speed");

			Assert.IsNotNull(result);
			Assert.AreEqual("speed", result.FieldName);
		}

		[Test]
		public void GetPropertyOverride_NonExistent_ReturnsNull()
		{
			var ovr = new NodeOverride("node-1");

			var result = ovr.GetPropertyOverride("missing");

			Assert.IsNull(result);
		}

		[Test]
		public void RemovePropertyOverride_RemovesEntry()
		{
			var ovr = new NodeOverride("node-1");
			ovr.SetPropertyOverride("speed", 5f, typeof(float));
			ovr.SetPropertyOverride("health", 100, typeof(int));

			ovr.RemovePropertyOverride("speed");

			Assert.AreEqual(1, ovr.PropertyOverrides.Count);
			Assert.AreEqual("health", ovr.PropertyOverrides[0].FieldName);
		}

		[Test]
		public void RemovePropertyOverride_NonExistent_DoesNotThrow()
		{
			var ovr = new NodeOverride("node-1");

			Assert.DoesNotThrow(() => ovr.RemovePropertyOverride("missing"));
		}

		[Test]
		public void HasAnyOverrides_TrueForDisableAction()
		{
			var ovr = new NodeOverride("node-1", OverrideAction.Disable);

			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void HasAnyOverrides_TrueForReplaceAction()
		{
			var ovr = new NodeOverride("node-1", OverrideAction.Replace);

			Assert.IsTrue(ovr.HasAnyOverrides);
		}

		[Test]
		public void HasAnyOverrides_FalseForModifyWithNoChanges()
		{
			var ovr = new NodeOverride("node-1", OverrideAction.Modify);

			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void HasAnyOverrides_FalseAfterClearingAll()
		{
			var ovr = new NodeOverride("node-1");
			ovr.PositionOverride = new Vector2(1, 2);
			ovr.DisplayNameOverride = "Name";
			ovr.CommentOverride = "Comment";
			ovr.SetPropertyOverride("x", 1, typeof(int));

			ovr.ClearPositionOverride();
			ovr.ClearDisplayNameOverride();
			ovr.ClearCommentOverride();
			ovr.RemovePropertyOverride("x");

			Assert.IsFalse(ovr.HasAnyOverrides);
		}

		[Test]
		public void ReplacementStateTypeAQN_CanBeSet()
		{
			var ovr = new NodeOverride("node-1", OverrideAction.Replace);
			ovr.ReplacementStateTypeAQN = "MyNamespace.MyState, MyAssembly";

			Assert.AreEqual("MyNamespace.MyState, MyAssembly", ovr.ReplacementStateTypeAQN);
		}

		[Test]
		public void IsSerializable()
		{
			Assert.IsTrue(typeof(NodeOverride).IsDefined(typeof(SerializableAttribute), false));
		}
	}

	[TestFixture]
	public class OverrideActionEnumTests
	{
		[Test]
		public void HasExpectedValues()
		{
			Assert.AreEqual(0, (int)OverrideAction.Modify);
			Assert.AreEqual(1, (int)OverrideAction.Replace);
			Assert.AreEqual(2, (int)OverrideAction.Disable);
		}

		[Test]
		public void HasThreeValues()
		{
			var values = Enum.GetValues(typeof(OverrideAction));
			Assert.AreEqual(3, values.Length);
		}
	}
}

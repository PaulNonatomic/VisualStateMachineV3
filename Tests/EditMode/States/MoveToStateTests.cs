using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class MoveToStateTests
	{
		[Test]
		public void MoveToState_HasCorrectAttributes()
		{
			var stateType = typeof(MoveToState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Blue, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Cube, stateStyle.Icon);
		}

		[Test]
		public void MoveToState_HasTargetDataIn()
		{
			var field = typeof(MoveToState).GetField("Target");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Target", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_HasDestinationDataIn()
		{
			var field = typeof(MoveToState).GetField("Destination");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Destination", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_HasDurationDataIn()
		{
			var field = typeof(MoveToState).GetField("Duration");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Duration", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_HasUseLocalSpaceDataIn()
		{
			var field = typeof(MoveToState).GetField("UseLocalSpace");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Use Local Space", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_HasProgressDataOut()
		{
			var field = typeof(MoveToState).GetField("Progress");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Progress", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_HasOnCompleteTransition()
		{
			var field = typeof(MoveToState).GetField("OnComplete");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Complete", attribute.DisplayName);
		}

		[Test]
		public void MoveToState_DurationDefaultsToOne()
		{
			var state = new MoveToState();

			Assert.AreEqual(1f, state.Duration);
		}

		[Test]
		public void MoveToState_UseLocalSpaceDefaultsToFalse()
		{
			var state = new MoveToState();

			Assert.IsFalse(state.UseLocalSpace);
		}

		[Test]
		public void MoveToState_UseUnscaledTimeDefaultsToFalse()
		{
			var state = new MoveToState();

			Assert.IsFalse(state.UseUnscaledTime);
		}

		[Test]
		public void MoveToState_ZeroDuration_SnapsToDestination()
		{
			var go = new GameObject("MoveTest");
			go.transform.position = Vector3.zero;

			var state = new MoveToState
			{
				Target = go.transform,
				Destination = new Vector3(10, 0, 0),
				Duration = 0f
			};
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(10, 0, 0), go.transform.position);
			Assert.IsTrue(transitionInvoked);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void MoveToState_NullTarget_InvokesOnComplete()
		{
			var state = new MoveToState
			{
				Target = null,
				Destination = new Vector3(10, 0, 0)
			};
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void MoveToState_NullOnComplete_DoesNotThrow()
		{
			var state = new MoveToState
			{
				Target = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void MoveToState_OnExit_ResetsPrivateFields()
		{
			var go = new GameObject("MoveTest");
			go.transform.position = Vector3.zero;

			var state = new MoveToState
			{
				Target = go.transform,
				Destination = new Vector3(10, 0, 0),
				Duration = 5f
			};
			state.OnComplete = () => { };
			state.OnEnter();

			state.OnExit();

			var targetField = typeof(MoveToState).GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
			var startPositionField = typeof(MoveToState).GetField("_startPosition", BindingFlags.NonPublic | BindingFlags.Instance);
			var elapsedField = typeof(MoveToState).GetField("_elapsed", BindingFlags.NonPublic | BindingFlags.Instance);

			Assert.IsNull(targetField.GetValue(state));
			Assert.AreEqual(Vector3.zero, (Vector3)startPositionField.GetValue(state));
			Assert.AreEqual(0f, (float)elapsedField.GetValue(state));
			UnityEngine.Object.DestroyImmediate(go);
		}
	}
}

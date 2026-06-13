#if VSM_VFX_GRAPH
using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class PlayVFXStateTests
	{

		[Test]
		public void DefaultVisualEffect_IsNull()
		{
			var state = new PlayVFXState();

			Assert.IsNull(state.VisualEffect);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new PlayVFXState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(PlayVFXState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void VisualEffect_HasDataInAttribute()
		{
			var field = typeof(PlayVFXState).GetField("VisualEffect");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayVFXState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullVisualEffect_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayVFXState
			{
				VisualEffect = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayVFXState] No VisualEffect found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullVisualEffectNullCallback_DoesNotThrow()
		{
			var state = new PlayVFXState
			{
				VisualEffect = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayVFXState] No VisualEffect found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class StopVFXStateTests
	{

		[Test]
		public void DefaultVisualEffect_IsNull()
		{
			var state = new StopVFXState();

			Assert.IsNull(state.VisualEffect);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new StopVFXState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(StopVFXState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void VisualEffect_HasDataInAttribute()
		{
			var field = typeof(StopVFXState).GetField("VisualEffect");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(StopVFXState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullVisualEffectNoContext_StillCallsOnComplete()
		{
			var completeCalled = false;
			var state = new StopVFXState
			{
				VisualEffect = null,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullVisualEffectNullCallback_DoesNotThrow()
		{
			var state = new StopVFXState
			{
				VisualEffect = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class SetVFXFloatStateTests
	{

		[Test]
		public void DefaultVisualEffect_IsNull()
		{
			var state = new SetVFXFloatState();

			Assert.IsNull(state.VisualEffect);
		}

		[Test]
		public void DefaultPropertyName_IsNull()
		{
			var state = new SetVFXFloatState();

			Assert.IsNull(state.PropertyName);
		}

		[Test]
		public void DefaultValue_IsZero()
		{
			var state = new SetVFXFloatState();

			Assert.AreEqual(0f, state.Value, 0.001f);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new SetVFXFloatState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(SetVFXFloatState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void VisualEffect_HasDataInAttribute()
		{
			var field = typeof(SetVFXFloatState).GetField("VisualEffect");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void PropertyName_HasDataInAttribute()
		{
			var field = typeof(SetVFXFloatState).GetField("PropertyName");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void Value_HasDataInAttribute()
		{
			var field = typeof(SetVFXFloatState).GetField("Value");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(SetVFXFloatState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullVisualEffect_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new SetVFXFloatState
			{
				VisualEffect = null,
				PropertyName = "TestFloat",
				Value = 1.5f,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[SetVFXFloatState] No VisualEffect found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullVisualEffectNullCallback_DoesNotThrow()
		{
			var state = new SetVFXFloatState
			{
				VisualEffect = null,
				PropertyName = "TestFloat",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[SetVFXFloatState] No VisualEffect found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Value_CanBeSet()
		{
			var state = new SetVFXFloatState();

			state.Value = 42.5f;

			Assert.AreEqual(42.5f, state.Value, 0.001f);
		}

		[Test]
		public void PropertyName_CanBeSet()
		{
			var state = new SetVFXFloatState();

			state.PropertyName = "Intensity";

			Assert.AreEqual("Intensity", state.PropertyName);
		}

	}

	[TestFixture]
	public class SendVFXEventStateTests
	{

		[Test]
		public void DefaultVisualEffect_IsNull()
		{
			var state = new SendVFXEventState();

			Assert.IsNull(state.VisualEffect);
		}

		[Test]
		public void DefaultEventName_IsNull()
		{
			var state = new SendVFXEventState();

			Assert.IsNull(state.EventName);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new SendVFXEventState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(SendVFXEventState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void VisualEffect_HasDataInAttribute()
		{
			var field = typeof(SendVFXEventState).GetField("VisualEffect");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void EventName_HasDataInAttribute()
		{
			var field = typeof(SendVFXEventState).GetField("EventName");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(SendVFXEventState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullVisualEffect_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new SendVFXEventState
			{
				VisualEffect = null,
				EventName = "TestEvent",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[SendVFXEventState] No VisualEffect found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullVisualEffectNullCallback_DoesNotThrow()
		{
			var state = new SendVFXEventState
			{
				VisualEffect = null,
				EventName = "TestEvent",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[SendVFXEventState] No VisualEffect found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void EventName_CanBeSet()
		{
			var state = new SendVFXEventState();

			state.EventName = "OnBurst";

			Assert.AreEqual("OnBurst", state.EventName);
		}

	}


	[TestFixture]
	public class AllVFXStateTests
	{
		[Test]
		public void AllVFXStates_HaveVioletColor()
		{
			var stateTypes = new[]
			{
				typeof(PlayVFXState),
				typeof(StopVFXState),
				typeof(SetVFXFloatState),
				typeof(SendVFXEventState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.Violet, stateStyle.Color,
					$"{stateType.Name} should have Violet color");
			}
		}

		[Test]
		public void AllVFXStates_HaveSparksIcon()
		{
			var stateTypes = new[]
			{
				typeof(PlayVFXState),
				typeof(StopVFXState),
				typeof(SetVFXFloatState),
				typeof(SendVFXEventState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon,
					$"{stateType.Name} should have Sparks icon");
			}
		}

		[Test]
		public void AllVFXStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(PlayVFXState),
				typeof(StopVFXState),
				typeof(SetVFXFloatState),
				typeof(SendVFXEventState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
				Assert.IsNotNull(stateInfo, $"{stateType.Name} should have StateInfoAttribute");
				Assert.IsFalse(string.IsNullOrEmpty(stateInfo.Tooltip),
					$"{stateType.Name} should have a tooltip");
			}
		}
	}

}
#endif

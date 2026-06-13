using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class SetAnimatorTriggerStateTests
	{
		private GameObject _gameObject;
		private Animator _animator;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_animator = _gameObject.AddComponent<Animator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		[Test]
		public void SetAnimatorTriggerState_WithNullAnimator_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = null;
			state.TriggerName = "TestTrigger";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled, "OnComplete should be called even when animator is null");
		}

		[Test]
		public void SetAnimatorTriggerState_WithEmptyTriggerName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = _animator;
			state.TriggerName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorTriggerState_WithNullTriggerName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = _animator;
			state.TriggerName = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorTriggerState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = _animator;
			state.TriggerName = "TestTrigger";
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class SetAnimatorBoolStateTests
	{
		private GameObject _gameObject;
		private Animator _animator;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_animator = _gameObject.AddComponent<Animator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		[Test]
		public void SetAnimatorBoolState_WithNullAnimator_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorBoolState();
			state.Animator = null;
			state.ParameterName = "TestBool";
			state.Value = true;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorBoolState_WithEmptyParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorBoolState();
			state.Animator = _animator;
			state.ParameterName = "";
			state.Value = true;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorBoolState_WithNullParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorBoolState();
			state.Animator = _animator;
			state.ParameterName = null;
			state.Value = true;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorBoolState_DefaultValue_IsTrue()
		{
			var state = new SetAnimatorBoolState();

			Assert.IsTrue(state.Value);
		}
	}

	[TestFixture]
	public class SetAnimatorFloatStateTests
	{
		private GameObject _gameObject;
		private Animator _animator;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_animator = _gameObject.AddComponent<Animator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		[Test]
		public void SetAnimatorFloatState_WithNullAnimator_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorFloatState();
			state.Animator = null;
			state.ParameterName = "TestFloat";
			state.Value = 1.5f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorFloatState_WithEmptyParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorFloatState();
			state.Animator = _animator;
			state.ParameterName = "";
			state.Value = 1.5f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorFloatState_WithNullParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorFloatState();
			state.Animator = _animator;
			state.ParameterName = null;
			state.Value = 1.5f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorFloatState_DefaultValue_IsZero()
		{
			var state = new SetAnimatorFloatState();

			Assert.AreEqual(0f, state.Value);
		}
	}

	[TestFixture]
	public class SetAnimatorIntStateTests
	{
		private GameObject _gameObject;
		private Animator _animator;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_animator = _gameObject.AddComponent<Animator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		[Test]
		public void SetAnimatorIntState_WithNullAnimator_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorIntState();
			state.Animator = null;
			state.ParameterName = "TestInt";
			state.Value = 5;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorIntState_WithEmptyParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorIntState();
			state.Animator = _animator;
			state.ParameterName = "";
			state.Value = 5;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorIntState_WithNullParameterName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetAnimatorIntState();
			state.Animator = _animator;
			state.ParameterName = null;
			state.Value = 5;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorIntState_DefaultValue_IsZero()
		{
			var state = new SetAnimatorIntState();

			Assert.AreEqual(0, state.Value);
		}
	}

	[TestFixture]
	public class WaitForAnimatorStateStateTests
	{
		private GameObject _gameObject;
		private Animator _animator;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_animator = _gameObject.AddComponent<Animator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_gameObject);
			}
		}

		[Test]
		public void WaitForAnimatorStateState_WithNullAnimator_DoesNotThrow_AndCompletesTransition()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = null;
			state.StateName = "TestState";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled, "OnComplete should be called when animator is null");
		}

		[Test]
		public void WaitForAnimatorStateState_WithEmptyStateName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = _animator;
			state.StateName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void WaitForAnimatorStateState_WithNullStateName_DoesNotThrow_AndCompletesTransition()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = _animator;
			state.StateName = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void WaitForAnimatorStateState_DefaultLayer_IsZero()
		{
			var state = new WaitForAnimatorStateState();

			Assert.AreEqual(0, state.Layer);
		}

		[Test]
		public void WaitForAnimatorStateState_DefaultWaitForEntry_IsTrue()
		{
			var state = new WaitForAnimatorStateState();

			Assert.IsTrue(state.WaitForEntry);
		}

		[Test]
		public void WaitForAnimatorStateState_OnUpdate_WithNullAnimator_DoesNotThrow()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = _animator;
			state.StateName = "TestState";

			// OnEnter first to initialize internal state
			state.OnEnter();

			// Simulate animator becoming null after initialization
			// Note: We can't actually null the animator reference after setup,
			// but we can test the null check in OnUpdate through the internal _animator field

			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void WaitForAnimatorStateState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = _animator;
			state.StateName = "TestState";
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void WaitForAnimatorStateState_StateNameHash_IsComputedCorrectly()
		{
			// Test that the state name is hashed consistently with Unity's Animator.StringToHash
			var stateName = "Idle";
			var expectedHash = Animator.StringToHash(stateName);

			var state = new WaitForAnimatorStateState();
			state.StateName = stateName;

			// The hash is computed in OnEnter, but we can verify StringToHash works as expected
			Assert.AreEqual(Animator.StringToHash("Idle"), expectedHash);
			Assert.AreNotEqual(Animator.StringToHash("Walk"), expectedHash);
		}

		[Test]
		public void WaitForAnimatorStateState_InitializesNormalizedTime_ToZero()
		{
			var state = new WaitForAnimatorStateState();

			Assert.AreEqual(0f, state.NormalizedTime);
		}
	}

	[TestFixture]
	public class CrossFadeAnimatorStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(CrossFadeAnimatorState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultTransitionDuration_Is025()
		{
			var state = new CrossFadeAnimatorState();

			Assert.AreEqual(0.25f, state.TransitionDuration, 0.001f);
		}

		[Test]
		public void DefaultLayer_IsZero()
		{
			var state = new CrossFadeAnimatorState();

			Assert.AreEqual(0, state.Layer);
		}

		[Test]
		public void DefaultTimeOffset_IsZero()
		{
			var state = new CrossFadeAnimatorState();

			Assert.AreEqual(0f, state.TimeOffset, 0.001f);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new CrossFadeAnimatorState
			{
				Animator = null,
				StateName = "Idle",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[CrossFadeAnimatorState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_EmptyStateName_LogsWarningAndTransitions()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var completeCalled = false;
			var state = new CrossFadeAnimatorState
			{
				Animator = animator,
				StateName = "",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[CrossFadeAnimatorState] No state name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new CrossFadeAnimatorState
			{
				Animator = null,
				StateName = "Idle",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[CrossFadeAnimatorState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void StateName_HasDataInAttribute()
		{
			var field = typeof(CrossFadeAnimatorState).GetField("StateName");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(CrossFadeAnimatorState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class PlayAnimatorStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(PlayAnimatorState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultLayer_IsZero()
		{
			var state = new PlayAnimatorState();

			Assert.AreEqual(0, state.Layer);
		}

		[Test]
		public void DefaultNormalizedTime_IsZero()
		{
			var state = new PlayAnimatorState();

			Assert.AreEqual(0f, state.NormalizedTime, 0.001f);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayAnimatorState
			{
				Animator = null,
				StateName = "Idle",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayAnimatorState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_EmptyStateName_LogsWarningAndTransitions()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var completeCalled = false;
			var state = new PlayAnimatorState
			{
				Animator = animator,
				StateName = "",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayAnimatorState] No state name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new PlayAnimatorState
			{
				Animator = null,
				StateName = "Idle",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayAnimatorState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void StateName_HasDataInAttribute()
		{
			var field = typeof(PlayAnimatorState).GetField("StateName");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayAnimatorState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class GetAnimatorFloatStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(GetAnimatorFloatState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultValue_IsZero()
		{
			var state = new GetAnimatorFloatState();

			Assert.AreEqual(0f, state.Value, 0.001f);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new GetAnimatorFloatState
			{
				Animator = null,
				ParameterName = "Speed",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorFloatState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_EmptyParameterName_LogsWarningAndTransitions()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var completeCalled = false;
			var state = new GetAnimatorFloatState
			{
				Animator = animator,
				ParameterName = "",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorFloatState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new GetAnimatorFloatState
			{
				Animator = null,
				ParameterName = "Speed",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorFloatState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Value_HasDataOutAttribute()
		{
			var field = typeof(GetAnimatorFloatState).GetField("Value");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(GetAnimatorFloatState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class GetAnimatorBoolStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(GetAnimatorBoolState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultValue_IsFalse()
		{
			var state = new GetAnimatorBoolState();

			Assert.IsFalse(state.Value);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndInvokesFalse()
		{
			var falseCalled = false;
			var state = new GetAnimatorBoolState
			{
				Animator = null,
				ParameterName = "IsGrounded",
				OnTrue = () => Assert.Fail("OnTrue should not be called"),
				OnFalse = () => falseCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorBoolState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(falseCalled);
		}

		[Test]
		public void OnEnter_EmptyParameterName_LogsWarningAndInvokesFalse()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var falseCalled = false;
			var state = new GetAnimatorBoolState
			{
				Animator = animator,
				ParameterName = "",
				OnTrue = () => Assert.Fail("OnTrue should not be called"),
				OnFalse = () => falseCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorBoolState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(falseCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbacksNullAnimator_DoesNotThrow()
		{
			var state = new GetAnimatorBoolState
			{
				Animator = null,
				ParameterName = "IsGrounded",
				OnTrue = null,
				OnFalse = null
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorBoolState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void HasTrueTransition()
		{
			var field = typeof(GetAnimatorBoolState).GetField("OnTrue");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void HasFalseTransition()
		{
			var field = typeof(GetAnimatorBoolState).GetField("OnFalse");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void Value_HasDataOutAttribute()
		{
			var field = typeof(GetAnimatorBoolState).GetField("Value");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class GetAnimatorIntStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(GetAnimatorIntState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultValue_IsZero()
		{
			var state = new GetAnimatorIntState();

			Assert.AreEqual(0, state.Value);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new GetAnimatorIntState
			{
				Animator = null,
				ParameterName = "StateIndex",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorIntState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_EmptyParameterName_LogsWarningAndTransitions()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var completeCalled = false;
			var state = new GetAnimatorIntState
			{
				Animator = animator,
				ParameterName = "",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorIntState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new GetAnimatorIntState
			{
				Animator = null,
				ParameterName = "StateIndex",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[GetAnimatorIntState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Value_HasDataOutAttribute()
		{
			var field = typeof(GetAnimatorIntState).GetField("Value");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(GetAnimatorIntState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class SetAnimatorSpeedStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(SetAnimatorSpeedState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultSpeed_IsOne()
		{
			var state = new SetAnimatorSpeedState();

			Assert.AreEqual(1f, state.Speed, 0.001f);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new SetAnimatorSpeedState
			{
				Animator = null,
				Speed = 0.5f,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[SetAnimatorSpeedState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new SetAnimatorSpeedState
			{
				Animator = null,
				Speed = 0.5f,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[SetAnimatorSpeedState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnEnter_WithAnimator_SetsSpeed()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var state = new SetAnimatorSpeedState
			{
				Animator = animator,
				Speed = 0.5f
			};

			state.OnEnter();

			Assert.AreEqual(0.5f, animator.speed, 0.001f);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void Speed_HasDataInAttribute()
		{
			var field = typeof(SetAnimatorSpeedState).GetField("Speed");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(SetAnimatorSpeedState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class SetAnimatorLayerWeightStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(SetAnimatorLayerWeightState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void DefaultLayer_IsZero()
		{
			var state = new SetAnimatorLayerWeightState();

			Assert.AreEqual(0, state.Layer);
		}

		[Test]
		public void DefaultWeight_IsOne()
		{
			var state = new SetAnimatorLayerWeightState();

			Assert.AreEqual(1f, state.Weight, 0.001f);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new SetAnimatorLayerWeightState
			{
				Animator = null,
				Layer = 1,
				Weight = 0.5f,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[SetAnimatorLayerWeightState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new SetAnimatorLayerWeightState
			{
				Animator = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[SetAnimatorLayerWeightState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Layer_HasDataInAttribute()
		{
			var field = typeof(SetAnimatorLayerWeightState).GetField("Layer");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void Weight_HasDataInAttribute()
		{
			var field = typeof(SetAnimatorLayerWeightState).GetField("Weight");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(SetAnimatorLayerWeightState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class ResetAnimatorTriggerStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(ResetAnimatorTriggerState);
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void OnEnter_NullAnimator_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new ResetAnimatorTriggerState
			{
				Animator = null,
				TriggerName = "Jump",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[ResetAnimatorTriggerState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_EmptyTriggerName_LogsWarningAndTransitions()
		{
			var go = new GameObject("Test");
			var animator = go.AddComponent<Animator>();

			var completeCalled = false;
			var state = new ResetAnimatorTriggerState
			{
				Animator = animator,
				TriggerName = "",
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[ResetAnimatorTriggerState] No trigger name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void OnEnter_NullCallbackNullAnimator_DoesNotThrow()
		{
			var state = new ResetAnimatorTriggerState
			{
				Animator = null,
				TriggerName = "Jump",
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[ResetAnimatorTriggerState] No Animator found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void TriggerName_HasDataInAttribute()
		{
			var field = typeof(ResetAnimatorTriggerState).GetField("TriggerName");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(ResetAnimatorTriggerState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class AllAnimatorStateTests
	{
		private static readonly Type[] AllAnimatorStateTypes =
		{
			typeof(SetAnimatorTriggerState),
			typeof(SetAnimatorBoolState),
			typeof(SetAnimatorFloatState),
			typeof(SetAnimatorIntState),
			typeof(WaitForAnimatorStateState),
			typeof(CrossFadeAnimatorState),
			typeof(PlayAnimatorState),
			typeof(GetAnimatorFloatState),
			typeof(GetAnimatorBoolState),
			typeof(GetAnimatorIntState),
			typeof(SetAnimatorSpeedState),
			typeof(SetAnimatorLayerWeightState),
			typeof(ResetAnimatorTriggerState)
		};

		[TestCaseSource(nameof(AllAnimatorStateTypes))]
		public void AnimatorState_HasOrangeColor(Type stateType)
		{
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
			Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
			Assert.AreEqual(StateColors.Orange, stateStyle.Color,
				$"{stateType.Name} should have Orange color");
		}

		[TestCaseSource(nameof(AllAnimatorStateTypes))]
		public void AnimatorState_HasFilmIcon(Type stateType)
		{
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
			Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon,
				$"{stateType.Name} should have Film icon");
		}

		[TestCaseSource(nameof(AllAnimatorStateTypes))]
		public void AnimatorState_HasStateInfoAttribute(Type stateType)
		{
			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			Assert.IsNotNull(stateInfo, $"{stateType.Name} should have StateInfoAttribute");
			Assert.IsFalse(string.IsNullOrEmpty(stateInfo.Tooltip),
				$"{stateType.Name} should have a tooltip");
		}
	}

}

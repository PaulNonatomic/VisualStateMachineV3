using System;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.Events;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class InvokeEventStateTests
	{

		[Test]
		public void InvokeEventState_DefaultEvent_IsNull()
		{
			var state = new InvokeEventState();

			Assert.IsNull(state.Event, "Default Event should be null");
		}

		[Test]
		public void InvokeEventState_DefaultOnComplete_IsNull()
		{
			var state = new InvokeEventState();

			Assert.IsNull(state.OnComplete, "Default OnComplete should be null");
		}



		[Test]
		public void InvokeEventState_WithEvent_InvokesEvent()
		{
			var eventInvoked = false;
			var unityEvent = new UnityEvent();
			unityEvent.AddListener(() => eventInvoked = true);

			var state = new InvokeEventState
			{
				Event = unityEvent
			};

			state.OnEnter();

			Assert.IsTrue(eventInvoked, "Event should be invoked");
		}

		[Test]
		public void InvokeEventState_InvokesOnComplete_AfterEvent()
		{
			var completeCalled = false;
			var state = new InvokeEventState
			{
				Event = new UnityEvent(),
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}

		[Test]
		public void InvokeEventState_EventInvokedBeforeOnComplete()
		{
			var callOrder = 0;
			var eventOrder = 0;
			var completeOrder = 0;

			var unityEvent = new UnityEvent();
			unityEvent.AddListener(() => eventOrder = ++callOrder);

			var state = new InvokeEventState
			{
				Event = unityEvent,
				OnComplete = () => completeOrder = ++callOrder
			};

			state.OnEnter();

			Assert.AreEqual(1, eventOrder, "Event should be called first");
			Assert.AreEqual(2, completeOrder, "OnComplete should be called second");
		}



		[Test]
		public void InvokeEventState_WithMultipleListeners_InvokesAll()
		{
			var listener1Called = false;
			var listener2Called = false;
			var unityEvent = new UnityEvent();
			unityEvent.AddListener(() => listener1Called = true);
			unityEvent.AddListener(() => listener2Called = true);

			var state = new InvokeEventState
			{
				Event = unityEvent
			};

			state.OnEnter();

			Assert.IsTrue(listener1Called, "First listener should be called");
			Assert.IsTrue(listener2Called, "Second listener should be called");
		}



		[Test]
		public void InvokeEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeEventState
			{
				Event = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void InvokeEventState_WithNullEvent_StillInvokesOnComplete()
		{
			var completeCalled = false;
			var state = new InvokeEventState
			{
				Event = null,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should still be called");
		}

		[Test]
		public void InvokeEventState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new InvokeEventState
			{
				Event = new UnityEvent(),
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class InvokeStringEventStateTests
	{

		[Test]
		public void InvokeStringEventState_DefaultValue_IsNull()
		{
			var state = new InvokeStringEventState();

			Assert.IsNull(state.Value, "Default Value should be null");
		}



		[Test]
		public void InvokeStringEventState_WithEvent_InvokesWithCorrectValue()
		{
			string receivedValue = null;
			var unityEvent = new UnityEvent<string>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeStringEventState
			{
				Event = unityEvent,
				Value = "TestString"
			};

			state.OnEnter();

			Assert.AreEqual("TestString", receivedValue, "Event should receive correct string value");
		}

		[Test]
		public void InvokeStringEventState_InvokesOnComplete_AfterEvent()
		{
			var completeCalled = false;
			var state = new InvokeStringEventState
			{
				Event = new UnityEvent<string>(),
				Value = "Test",
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}



		[Test]
		public void InvokeStringEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeStringEventState
			{
				Event = null,
				Value = "Test"
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void InvokeStringEventState_WithNullValue_PassesNull()
		{
			string receivedValue = "not-null";
			var unityEvent = new UnityEvent<string>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeStringEventState
			{
				Event = unityEvent,
				Value = null
			};

			state.OnEnter();

			Assert.IsNull(receivedValue, "Event should receive null value");
		}

		[Test]
		public void InvokeStringEventState_WithEmptyString_PassesEmpty()
		{
			string receivedValue = null;
			var unityEvent = new UnityEvent<string>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeStringEventState
			{
				Event = unityEvent,
				Value = ""
			};

			state.OnEnter();

			Assert.AreEqual("", receivedValue, "Event should receive empty string");
		}

	}

	[TestFixture]
	public class InvokeFloatEventStateTests
	{

		[Test]
		public void InvokeFloatEventState_DefaultValue_IsZero()
		{
			var state = new InvokeFloatEventState();

			Assert.AreEqual(0f, state.Value, "Default Value should be 0");
		}



		[Test]
		public void InvokeFloatEventState_WithEvent_InvokesWithCorrectValue()
		{
			float receivedValue = -1f;
			var unityEvent = new UnityEvent<float>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeFloatEventState
			{
				Event = unityEvent,
				Value = 3.14f
			};

			state.OnEnter();

			Assert.AreEqual(3.14f, receivedValue, 0.001f, "Event should receive correct float value");
		}

		[Test]
		public void InvokeFloatEventState_InvokesOnComplete_AfterEvent()
		{
			var completeCalled = false;
			var state = new InvokeFloatEventState
			{
				Event = new UnityEvent<float>(),
				Value = 1.0f,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}



		[Test]
		public void InvokeFloatEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeFloatEventState
			{
				Event = null,
				Value = 1.0f
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void InvokeFloatEventState_WithNegativeValue_PassesCorrectly()
		{
			float receivedValue = 0f;
			var unityEvent = new UnityEvent<float>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeFloatEventState
			{
				Event = unityEvent,
				Value = -99.5f
			};

			state.OnEnter();

			Assert.AreEqual(-99.5f, receivedValue, 0.001f, "Event should receive negative float value");
		}

		[Test]
		public void InvokeFloatEventState_WithLargeValue_PassesCorrectly()
		{
			float receivedValue = 0f;
			var unityEvent = new UnityEvent<float>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeFloatEventState
			{
				Event = unityEvent,
				Value = 999999.99f
			};

			state.OnEnter();

			Assert.AreEqual(999999.99f, receivedValue, 0.01f, "Event should receive large float value");
		}

	}

	[TestFixture]
	public class InvokeIntEventStateTests
	{

		[Test]
		public void InvokeIntEventState_DefaultValue_IsZero()
		{
			var state = new InvokeIntEventState();

			Assert.AreEqual(0, state.Value, "Default Value should be 0");
		}



		[Test]
		public void InvokeIntEventState_WithEvent_InvokesWithCorrectValue()
		{
			int receivedValue = -1;
			var unityEvent = new UnityEvent<int>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeIntEventState
			{
				Event = unityEvent,
				Value = 42
			};

			state.OnEnter();

			Assert.AreEqual(42, receivedValue, "Event should receive correct int value");
		}

		[Test]
		public void InvokeIntEventState_InvokesOnComplete_AfterEvent()
		{
			var completeCalled = false;
			var state = new InvokeIntEventState
			{
				Event = new UnityEvent<int>(),
				Value = 1,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}



		[Test]
		public void InvokeIntEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeIntEventState
			{
				Event = null,
				Value = 1
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void InvokeIntEventState_WithNegativeValue_PassesCorrectly()
		{
			int receivedValue = 0;
			var unityEvent = new UnityEvent<int>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeIntEventState
			{
				Event = unityEvent,
				Value = -100
			};

			state.OnEnter();

			Assert.AreEqual(-100, receivedValue, "Event should receive negative int value");
		}

		[Test]
		public void InvokeIntEventState_WithZeroValue_PassesCorrectly()
		{
			int receivedValue = -1;
			var unityEvent = new UnityEvent<int>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeIntEventState
			{
				Event = unityEvent,
				Value = 0
			};

			state.OnEnter();

			Assert.AreEqual(0, receivedValue, "Event should receive zero value");
		}

		[Test]
		public void InvokeIntEventState_WithMaxValue_PassesCorrectly()
		{
			int receivedValue = 0;
			var unityEvent = new UnityEvent<int>();
			unityEvent.AddListener(v => receivedValue = v);

			var state = new InvokeIntEventState
			{
				Event = unityEvent,
				Value = int.MaxValue
			};

			state.OnEnter();

			Assert.AreEqual(int.MaxValue, receivedValue, "Event should receive max int value");
		}

	}
}

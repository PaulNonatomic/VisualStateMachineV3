using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class SetAnimatorTriggerStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void SetAnimatorTriggerState_WithNoAnimator_LogsWarning()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = null;
			state.TriggerName = "TestTrigger";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorTriggerState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should still be called");
		}

		[Test]
		public void SetAnimatorTriggerState_WithEmptyTriggerName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new SetAnimatorTriggerState();
			state.Animator = animator;
			state.TriggerName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorTriggerState] No trigger name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorTriggerState_WithNullTriggerName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new SetAnimatorTriggerState();
			state.Animator = animator;
			state.TriggerName = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorTriggerState] No trigger name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetAnimatorTriggerState_InvokesOnComplete()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorTriggerState_WithNullTransition_DoesNotThrow()
		{
			var state = new SetAnimatorTriggerState();
			state.Animator = null;
			state.OnComplete = null;

			LogAssert.ignoreFailingMessages = true;
			Assert.DoesNotThrow(() => state.OnEnter());
			LogAssert.ignoreFailingMessages = false;
		}

	}

	[TestFixture]
	public class SetAnimatorBoolStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void SetAnimatorBoolState_DefaultValue_IsTrue()
		{
			var state = new SetAnimatorBoolState();

			Assert.IsTrue(state.Value, "Default value should be true");
		}



		[Test]
		public void SetAnimatorBoolState_WithNoAnimator_LogsWarning()
		{
			var state = new SetAnimatorBoolState();
			state.Animator = null;
			state.ParameterName = "TestBool";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorBoolState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorBoolState_WithEmptyParameterName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new SetAnimatorBoolState();
			state.Animator = animator;
			state.ParameterName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorBoolState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetAnimatorBoolState_InvokesOnComplete()
		{
			var state = new SetAnimatorBoolState();
			state.Animator = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			Assert.IsTrue(completeCalled);
		}

	}

	[TestFixture]
	public class SetAnimatorFloatStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void SetAnimatorFloatState_DefaultValue_IsZero()
		{
			var state = new SetAnimatorFloatState();

			Assert.AreEqual(0f, state.Value);
		}



		[Test]
		public void SetAnimatorFloatState_WithNoAnimator_LogsWarning()
		{
			var state = new SetAnimatorFloatState();
			state.Animator = null;
			state.ParameterName = "TestFloat";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorFloatState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorFloatState_WithEmptyParameterName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new SetAnimatorFloatState();
			state.Animator = animator;
			state.ParameterName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorFloatState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetAnimatorFloatState_InvokesOnComplete()
		{
			var state = new SetAnimatorFloatState();
			state.Animator = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			Assert.IsTrue(completeCalled);
		}

	}

	[TestFixture]
	public class SetAnimatorIntStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void SetAnimatorIntState_DefaultValue_IsZero()
		{
			var state = new SetAnimatorIntState();

			Assert.AreEqual(0, state.Value);
		}



		[Test]
		public void SetAnimatorIntState_WithNoAnimator_LogsWarning()
		{
			var state = new SetAnimatorIntState();
			state.Animator = null;
			state.ParameterName = "TestInt";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorIntState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetAnimatorIntState_WithEmptyParameterName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new SetAnimatorIntState();
			state.Animator = animator;
			state.ParameterName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetAnimatorIntState] No parameter name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetAnimatorIntState_InvokesOnComplete()
		{
			var state = new SetAnimatorIntState();
			state.Animator = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			Assert.IsTrue(completeCalled);
		}

	}

	[TestFixture]
	public class WaitForAnimatorStateStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
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
		public void WaitForAnimatorStateState_WithNoAnimator_LogsWarning()
		{
			var state = new WaitForAnimatorStateState();
			state.Animator = null;
			state.StateName = "TestState";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[WaitForAnimatorStateState] No Animator found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void WaitForAnimatorStateState_WithEmptyStateName_LogsWarning()
		{
			var animator = _testObject.AddComponent<Animator>();

			var state = new WaitForAnimatorStateState();
			state.Animator = animator;
			state.StateName = "";
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[WaitForAnimatorStateState] No state name specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void WaitForAnimatorStateState_NormalizedTimeOutput_IsAccessible()
		{
			var state = new WaitForAnimatorStateState();

			Assert.AreEqual(0f, state.NormalizedTime);
			state.NormalizedTime = 0.5f;
			Assert.AreEqual(0.5f, state.NormalizedTime);
		}

	}
}

using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class SetPositionStateTests
	{
		private GameObject _testObject;
		private GameObject _parent;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
			_parent = new GameObject("Parent");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}

			if (_parent != null)
			{
				UnityEngine.Object.Destroy(_parent);
			}
		}


		[Test]
		public void SetPositionState_SetsWorldPosition()
		{
			var state = new SetPositionState();
			state.Target = _testObject.transform;
			state.Position = new Vector3(10, 20, 30);
			state.UseLocalSpace = false;

			state.OnEnter();

			Assert.AreEqual(new Vector3(10, 20, 30), _testObject.transform.position);
		}

		[Test]
		public void SetPositionState_SetsLocalPosition_WhenUseLocalSpaceIsTrue()
		{
			_testObject.transform.SetParent(_parent.transform);
			_parent.transform.position = new Vector3(100, 0, 0);

			var state = new SetPositionState();
			state.Target = _testObject.transform;
			state.Position = new Vector3(10, 0, 0);
			state.UseLocalSpace = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(10, 0, 0), _testObject.transform.localPosition);
			Assert.AreEqual(new Vector3(110, 0, 0), _testObject.transform.position);
		}



		[Test]
		public void SetPositionState_InvokesOnComplete()
		{
			var state = new SetPositionState();
			state.Target = _testObject.transform;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetPositionState_WithNullTarget_LogsWarning()
		{
			var state = new SetPositionState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetPositionState] No target transform specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetPositionState_DefaultPosition_IsZero()
		{
			var state = new SetPositionState();

			Assert.AreEqual(Vector3.zero, state.Position);
		}

		[Test]
		public void SetPositionState_DefaultUseLocalSpace_IsFalse()
		{
			var state = new SetPositionState();

			Assert.IsFalse(state.UseLocalSpace);
		}

	}

	[TestFixture]
	public class SetRotationStateTests
	{
		private GameObject _testObject;
		private GameObject _parent;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
			_parent = new GameObject("Parent");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}

			if (_parent != null)
			{
				UnityEngine.Object.Destroy(_parent);
			}
		}


		[Test]
		public void SetRotationState_SetsWorldRotation()
		{
			var state = new SetRotationState();
			state.Target = _testObject.transform;
			state.EulerAngles = new Vector3(0, 90, 0);
			state.UseLocalSpace = false;

			state.OnEnter();

			Assert.AreEqual(90f, _testObject.transform.eulerAngles.y, 0.01f);
		}

		[Test]
		public void SetRotationState_SetsLocalRotation_WhenUseLocalSpaceIsTrue()
		{
			_testObject.transform.SetParent(_parent.transform);
			_parent.transform.rotation = Quaternion.Euler(0, 45, 0);

			var state = new SetRotationState();
			state.Target = _testObject.transform;
			state.EulerAngles = new Vector3(0, 30, 0);
			state.UseLocalSpace = true;

			state.OnEnter();

			Assert.AreEqual(30f, _testObject.transform.localEulerAngles.y, 0.01f);
		}



		[Test]
		public void SetRotationState_InvokesOnComplete()
		{
			var state = new SetRotationState();
			state.Target = _testObject.transform;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetRotationState_WithNullTarget_LogsWarning()
		{
			var state = new SetRotationState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetRotationState] No target transform specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetRotationState_DefaultEulerAngles_IsZero()
		{
			var state = new SetRotationState();

			Assert.AreEqual(Vector3.zero, state.EulerAngles);
		}

		[Test]
		public void SetRotationState_DefaultUseLocalSpace_IsFalse()
		{
			var state = new SetRotationState();

			Assert.IsFalse(state.UseLocalSpace);
		}

	}

	[TestFixture]
	public class SetScaleStateTests
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
		public void SetScaleState_SetsLocalScale()
		{
			var state = new SetScaleState();
			state.Target = _testObject.transform;
			state.Scale = new Vector3(2, 3, 4);

			state.OnEnter();

			Assert.AreEqual(new Vector3(2, 3, 4), _testObject.transform.localScale);
		}

		[Test]
		public void SetScaleState_CanSetUniformScale()
		{
			var state = new SetScaleState();
			state.Target = _testObject.transform;
			state.Scale = Vector3.one * 5;

			state.OnEnter();

			Assert.AreEqual(Vector3.one * 5, _testObject.transform.localScale);
		}



		[Test]
		public void SetScaleState_InvokesOnComplete()
		{
			var state = new SetScaleState();
			state.Target = _testObject.transform;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetScaleState_WithNullTarget_LogsWarning()
		{
			var state = new SetScaleState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SetScaleState] No target transform specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void SetScaleState_DefaultScale_IsOne()
		{
			var state = new SetScaleState();

			Assert.AreEqual(Vector3.one, state.Scale);
		}

	}

	[TestFixture]
	public class LookAtStateTests
	{
		private GameObject _source;
		private GameObject _target;

		[SetUp]
		public void SetUp()
		{
			_source = new GameObject("Source");
			_target = new GameObject("Target");
		}

		[TearDown]
		public void TearDown()
		{
			if (_source != null)
			{
				UnityEngine.Object.Destroy(_source);
			}

			if (_target != null)
			{
				UnityEngine.Object.Destroy(_target);
			}
		}


		[Test]
		public void LookAtState_RotatesToFaceTargetTransform()
		{
			_source.transform.position = Vector3.zero;
			_target.transform.position = new Vector3(0, 0, 10);

			var state = new LookAtState();
			state.Source = _source.transform;
			state.TargetTransform = _target.transform;

			state.OnEnter();

			// Should be facing forward (towards Z+)
			Assert.AreEqual(0f, _source.transform.eulerAngles.y, 1f);
		}

		[Test]
		public void LookAtState_RotatesToFaceTargetPosition()
		{
			_source.transform.position = Vector3.zero;

			var state = new LookAtState();
			state.Source = _source.transform;
			state.TargetTransform = null;
			state.TargetPosition = new Vector3(10, 0, 0);

			state.OnEnter();

			// Should be facing right (towards X+)
			Assert.AreEqual(90f, _source.transform.eulerAngles.y, 1f);
		}

		[Test]
		public void LookAtState_UsesTargetTransform_OverTargetPosition()
		{
			_source.transform.position = Vector3.zero;
			_target.transform.position = new Vector3(0, 0, 10); // Facing forward

			var state = new LookAtState();
			state.Source = _source.transform;
			state.TargetTransform = _target.transform;
			state.TargetPosition = new Vector3(10, 0, 0); // Would face right if used

			state.OnEnter();

			// Should be facing forward because TargetTransform takes priority
			Assert.AreEqual(0f, _source.transform.eulerAngles.y, 1f);
		}



		[Test]
		public void LookAtState_InvokesOnComplete()
		{
			var state = new LookAtState();
			state.Source = _source.transform;
			state.TargetPosition = Vector3.forward;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void LookAtState_WithNullSource_LogsWarning()
		{
			var state = new LookAtState();
			state.Source = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[LookAtState] No source transform specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void LookAtState_DefaultWorldUp_IsVectorUp()
		{
			var state = new LookAtState();

			Assert.AreEqual(Vector3.up, state.WorldUp);
		}

	}

	[TestFixture]
	public class MoveToStateTests
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


		[UnityTest]
		public IEnumerator MoveToState_MovesToDestination_OverDuration()
		{
			_testObject.transform.position = Vector3.zero;

			var state = new MoveToState();
			state.Target = _testObject.transform;
			state.Destination = new Vector3(10, 0, 0);
			state.Duration = 0.3f;

			state.OnEnter();

			// Simulate updates over time (state needs OnUpdate calls to move)
			float elapsed = 0f;
			while (elapsed < 0.15f)
			{
				state.OnUpdate();
				yield return null;
				elapsed += Time.deltaTime;
			}

			// Should be moving (somewhere between start and end)
			Assert.Greater(_testObject.transform.position.x, 0f);
			Assert.Less(_testObject.transform.position.x, 10f);

			// Continue until completion
			while (elapsed < 0.4f)
			{
				state.OnUpdate();
				yield return null;
				elapsed += Time.deltaTime;
			}

			// Should be at destination
			Assert.AreEqual(10f, _testObject.transform.position.x, 0.1f);
		}

		[UnityTest]
		public IEnumerator MoveToState_UpdatesProgress_OverTime()
		{
			_testObject.transform.position = Vector3.zero;

			var state = new MoveToState();
			state.Target = _testObject.transform;
			state.Destination = new Vector3(10, 0, 0);
			state.Duration = 0.5f;

			state.OnEnter();

			yield return new WaitForSeconds(0.2f);

			// Update a few times
			for (int i = 0; i < 10; i++)
			{
				state.OnUpdate();
				yield return null;
			}

			Assert.Greater(state.Progress, 0f);
			Assert.Less(state.Progress, 1f);
		}

		[Test]
		public void MoveToState_WithZeroDuration_CompletesImmediately()
		{
			_testObject.transform.position = Vector3.zero;

			var state = new MoveToState();
			state.Target = _testObject.transform;
			state.Destination = new Vector3(10, 0, 0);
			state.Duration = 0f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
			Assert.AreEqual(new Vector3(10, 0, 0), _testObject.transform.position);
		}

		[UnityTest]
		public IEnumerator MoveToState_InvokesOnComplete_WhenDone()
		{
			_testObject.transform.position = Vector3.zero;

			var state = new MoveToState();
			state.Target = _testObject.transform;
			state.Destination = new Vector3(10, 0, 0);
			state.Duration = 0.1f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			// Simulate updates until complete
			float elapsed = 0;
			while (elapsed < 0.2f)
			{
				state.OnUpdate();
				yield return null;
				elapsed += Time.deltaTime;
			}

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void MoveToState_WithUseLocalSpace_UsesLocalPosition()
		{
			var parent = new GameObject("Parent");
			parent.transform.position = new Vector3(100, 0, 0);
			_testObject.transform.SetParent(parent.transform);
			_testObject.transform.localPosition = Vector3.zero;

			var state = new MoveToState();
			state.Target = _testObject.transform;
			state.Destination = new Vector3(10, 0, 0);
			state.Duration = 0f; // Immediate
			state.UseLocalSpace = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(10, 0, 0), _testObject.transform.localPosition);

			UnityEngine.Object.Destroy(parent);
		}



		[Test]
		public void MoveToState_DefaultDuration_IsOne()
		{
			var state = new MoveToState();

			Assert.AreEqual(1f, state.Duration);
		}

		[Test]
		public void MoveToState_DefaultUseLocalSpace_IsFalse()
		{
			var state = new MoveToState();

			Assert.IsFalse(state.UseLocalSpace);
		}

		[Test]
		public void MoveToState_DefaultUseUnscaledTime_IsFalse()
		{
			var state = new MoveToState();

			Assert.IsFalse(state.UseUnscaledTime);
		}



		[Test]
		public void MoveToState_WithNullTarget_LogsWarning()
		{
			var state = new MoveToState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[MoveToState] No target transform specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

	}
}

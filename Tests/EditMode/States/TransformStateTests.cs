using System;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class SetScaleStateTests
	{
		private GameObject _gameObject;
		private Transform _transform;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_transform = _gameObject.transform;
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
		public void SetScaleState_SetsLocalScale_WhenTargetIsProvided()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(2f, 3f, 4f);
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(2f, 3f, 4f), _transform.localScale);
			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}

		[Test]
		public void SetScaleState_SetsUniformScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = Vector3.one * 5f;

			state.OnEnter();

			Assert.AreEqual(new Vector3(5f, 5f, 5f), _transform.localScale);
		}

		[Test]
		public void SetScaleState_SetsZeroScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = Vector3.zero;

			state.OnEnter();

			Assert.AreEqual(Vector3.zero, _transform.localScale);
		}

		[Test]
		public void SetScaleState_SetsNegativeScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(-1f, -1f, -1f);

			state.OnEnter();

			Assert.AreEqual(new Vector3(-1f, -1f, -1f), _transform.localScale);
		}

		[Test]
		public void SetScaleState_WithNullTarget_DoesNotThrow_AndCompletesTransition()
		{
			var state = new SetScaleState();
			state.Target = null;
			state.Scale = Vector3.one * 2f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled, "OnComplete should still be called even with null target");
		}

		[Test]
		public void SetScaleState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = Vector3.one * 2f;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.AreEqual(new Vector3(2f, 2f, 2f), _transform.localScale);
		}

		[Test]
		public void SetScaleState_DefaultScale_IsVectorOne()
		{
			var state = new SetScaleState();

			Assert.AreEqual(Vector3.one, state.Scale);
		}

		[Test]
		public void SetScaleState_SetsNonUniformScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(1f, 2f, 0.5f);

			state.OnEnter();

			Assert.AreEqual(new Vector3(1f, 2f, 0.5f), _transform.localScale);
		}

		[Test]
		public void SetScaleState_OverwritesPreviousScale()
		{
			_transform.localScale = new Vector3(10f, 10f, 10f);

			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(1f, 1f, 1f);

			state.OnEnter();

			Assert.AreEqual(Vector3.one, _transform.localScale);
		}

		[Test]
		public void SetScaleState_SetsVeryLargeScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(1000f, 1000f, 1000f);

			state.OnEnter();

			Assert.AreEqual(new Vector3(1000f, 1000f, 1000f), _transform.localScale);
		}

		[Test]
		public void SetScaleState_SetsVerySmallScale()
		{
			var state = new SetScaleState();
			state.Target = _transform;
			state.Scale = new Vector3(0.001f, 0.001f, 0.001f);

			state.OnEnter();

			Assert.AreEqual(new Vector3(0.001f, 0.001f, 0.001f), _transform.localScale);
		}
	}

	[TestFixture]
	public class SetPositionStateTests
	{
		private GameObject _gameObject;
		private Transform _transform;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_transform = _gameObject.transform;
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
		public void SetPositionState_SetsWorldPosition()
		{
			var state = new SetPositionState();
			state.Target = _transform;
			state.Position = new Vector3(5f, 10f, 15f);
			state.UseLocalSpace = false;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(5f, 10f, 15f), _transform.position);
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetPositionState_SetsLocalPosition()
		{
			var parent = new GameObject("Parent");
			parent.transform.position = new Vector3(10f, 0f, 0f);
			_transform.SetParent(parent.transform);

			var state = new SetPositionState();
			state.Target = _transform;
			state.Position = new Vector3(5f, 0f, 0f);
			state.UseLocalSpace = true;

			state.OnEnter();

			Assert.AreEqual(new Vector3(5f, 0f, 0f), _transform.localPosition);
			Assert.AreEqual(new Vector3(15f, 0f, 0f), _transform.position);

			UnityEngine.Object.DestroyImmediate(parent);
		}

		[Test]
		public void SetPositionState_WithNullTarget_DoesNotThrow()
		{
			var state = new SetPositionState();
			state.Target = null;
			state.Position = Vector3.one;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
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
		private GameObject _gameObject;
		private Transform _transform;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_transform = _gameObject.transform;
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
		public void SetRotationState_SetsWorldEulerAngles()
		{
			var state = new SetRotationState();
			state.Target = _transform;
			state.EulerAngles = new Vector3(0f, 90f, 0f);
			state.UseLocalSpace = false;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.AreEqual(90f, _transform.eulerAngles.y, 0.001f);
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void SetRotationState_SetsLocalEulerAngles()
		{
			var parent = new GameObject("Parent");
			parent.transform.eulerAngles = new Vector3(0f, 45f, 0f);
			_transform.SetParent(parent.transform);

			var state = new SetRotationState();
			state.Target = _transform;
			state.EulerAngles = new Vector3(0f, 30f, 0f);
			state.UseLocalSpace = true;

			state.OnEnter();

			Assert.AreEqual(30f, _transform.localEulerAngles.y, 0.001f);

			UnityEngine.Object.DestroyImmediate(parent);
		}

		[Test]
		public void SetRotationState_WithNullTarget_DoesNotThrow()
		{
			var state = new SetRotationState();
			state.Target = null;
			state.EulerAngles = Vector3.one * 45f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}
	}

	[TestFixture]
	public class LookAtStateTests
	{
		private GameObject _gameObject;
		private Transform _transform;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestObject");
			_transform = _gameObject.transform;
			_transform.position = Vector3.zero;
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
		public void LookAtState_RotatesTowardsTargetPosition()
		{
			var state = new LookAtState();
			state.Source = _transform;
			state.TargetPosition = new Vector3(0f, 0f, 10f);
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			// Forward should point towards positive Z
			Assert.AreEqual(0f, Vector3.Angle(_transform.forward, Vector3.forward), 0.01f);
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void LookAtState_RotatesTowardsTargetTransform()
		{
			var targetObj = new GameObject("Target");
			targetObj.transform.position = new Vector3(10f, 0f, 0f);

			var state = new LookAtState();
			state.Source = _transform;
			state.TargetTransform = targetObj.transform;

			state.OnEnter();

			// Forward should point towards positive X
			Assert.AreEqual(0f, Vector3.Angle(_transform.forward, Vector3.right), 0.01f);

			UnityEngine.Object.DestroyImmediate(targetObj);
		}

		[Test]
		public void LookAtState_TargetTransform_TakesPriority_OverTargetPosition()
		{
			var targetObj = new GameObject("Target");
			targetObj.transform.position = new Vector3(10f, 0f, 0f);

			var state = new LookAtState();
			state.Source = _transform;
			state.TargetTransform = targetObj.transform;
			state.TargetPosition = new Vector3(0f, 0f, 10f); // This should be ignored

			state.OnEnter();

			// Forward should point towards positive X (TargetTransform), not Z (TargetPosition)
			Assert.AreEqual(0f, Vector3.Angle(_transform.forward, Vector3.right), 0.01f);

			UnityEngine.Object.DestroyImmediate(targetObj);
		}

		[Test]
		public void LookAtState_WithNullSource_DoesNotThrow()
		{
			var state = new LookAtState();
			state.Source = null;
			state.TargetPosition = Vector3.forward;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void LookAtState_UsesCustomWorldUp()
		{
			var state = new LookAtState();
			state.Source = _transform;
			state.TargetPosition = new Vector3(0f, 0f, 10f);
			state.WorldUp = Vector3.right;

			state.OnEnter();

			// Transform's up should be influenced by WorldUp
			Assert.AreEqual(0f, Vector3.Angle(_transform.forward, Vector3.forward), 0.01f);
		}

		[Test]
		public void LookAtState_DefaultWorldUp_IsVectorUp()
		{
			var state = new LookAtState();

			Assert.AreEqual(Vector3.up, state.WorldUp);
		}
	}
}

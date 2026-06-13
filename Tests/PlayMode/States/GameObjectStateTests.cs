using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class SetActiveStateTests
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
		public IEnumerator SetActiveState_WhenActiveIsTrue_ActivatesTarget()
		{
			_testObject.SetActive(false);
			Assert.IsFalse(_testObject.activeSelf);

			var state = new SetActiveState();
			state.Target = _testObject;
			state.Active = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(_testObject.activeSelf, "Target should be activated");
		}

		[UnityTest]
		public IEnumerator SetActiveState_WhenActiveIsFalse_DeactivatesTarget()
		{
			_testObject.SetActive(true);
			Assert.IsTrue(_testObject.activeSelf);

			var state = new SetActiveState();
			state.Target = _testObject;
			state.Active = false;

			state.OnEnter();

			yield return null;

			Assert.IsFalse(_testObject.activeSelf, "Target should be deactivated");
		}



		[Test]
		public void SetActiveState_InvokesOnComplete_AfterSettingActive()
		{
			var state = new SetActiveState();
			state.Target = _testObject;
			state.Active = true;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called");
		}

		[Test]
		public void SetActiveState_WithNullTarget_InvokesOnComplete()
		{
			var state = new SetActiveState();
			state.Target = null;
			state.Active = true;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			// Expect a warning log
			LogAssert.Expect(LogType.Warning, "[SetActiveState] No target GameObject specified and no context GameObject available");
			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called even with null target");
		}

		[Test]
		public void SetActiveState_WithNullTransition_DoesNotThrow()
		{
			var state = new SetActiveState();
			state.Target = _testObject;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void SetActiveState_DefaultActive_IsTrue()
		{
			var state = new SetActiveState();

			Assert.IsTrue(state.Active, "Default Active should be true");
		}

	}

	[TestFixture]
	public class DestroyStateTests
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
		public IEnumerator DestroyState_DestroysTarget()
		{
			var state = new DestroyState();
			state.Target = _testObject;
			state.Delay = 0f;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(_testObject == null, "Target should be destroyed");
			_testObject = null; // Prevent TearDown from trying to destroy again
		}

		[UnityTest]
		public IEnumerator DestroyState_WithDelay_DestroysAfterDelay()
		{
			var state = new DestroyState();
			state.Target = _testObject;
			state.Delay = 0.2f;

			state.OnEnter();

			yield return null;
			Assert.IsFalse(_testObject == null, "Target should not be destroyed immediately");

			yield return new WaitForSeconds(0.3f);

			Assert.IsTrue(_testObject == null, "Target should be destroyed after delay");
			_testObject = null;
		}



		[Test]
		public void DestroyState_InvokesOnComplete_Immediately()
		{
			var state = new DestroyState();
			state.Target = _testObject;
			state.Delay = 0f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called immediately");
		}

		[Test]
		public void DestroyState_WithDelay_InvokesOnComplete_Immediately()
		{
			var state = new DestroyState();
			state.Target = _testObject;
			state.Delay = 1f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called immediately even with delay");
		}

		[Test]
		public void DestroyState_WithNullTarget_LogsWarning_AndInvokesOnComplete()
		{
			var state = new DestroyState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[DestroyState] No target GameObject to destroy");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void DestroyState_WithNullTransition_DoesNotThrow()
		{
			var state = new DestroyState();
			state.Target = _testObject;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void DestroyState_DefaultDelay_IsZero()
		{
			var state = new DestroyState();

			Assert.AreEqual(0f, state.Delay, "Default delay should be 0");
		}

	}

	[TestFixture]
	public class DestroyImmediateStateTests
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
				UnityEngine.Object.DestroyImmediate(_testObject);
			}
		}


		[Test]
		public void DestroyImmediateState_DestroysTarget_Immediately()
		{
			var state = new DestroyImmediateState();
			state.Target = _testObject;

			state.OnEnter();

			Assert.IsTrue(_testObject == null, "Target should be destroyed immediately");
			_testObject = null;
		}



		[Test]
		public void DestroyImmediateState_InvokesOnComplete()
		{
			var state = new DestroyImmediateState();
			state.Target = _testObject;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
			_testObject = null;
		}

		[Test]
		public void DestroyImmediateState_WithNullTarget_LogsWarning()
		{
			var state = new DestroyImmediateState();
			state.Target = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[DestroyImmediateState] No target GameObject to destroy");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

	}

	[TestFixture]
	public class SpawnPrefabStateTests
	{
		private GameObject _prefab;
		private GameObject _parent;

		[SetUp]
		public void SetUp()
		{
			_prefab = new GameObject("TestPrefab");
			_parent = new GameObject("Parent");
		}

		[TearDown]
		public void TearDown()
		{
			if (_prefab != null)
			{
				UnityEngine.Object.DestroyImmediate(_prefab);
			}

			if (_parent != null)
			{
				UnityEngine.Object.Destroy(_parent);
			}
		}


		[UnityTest]
		public IEnumerator SpawnPrefabState_InstantiatesPrefab()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.Position = Vector3.zero;

			state.OnEnter();

			yield return null;

			Assert.IsNotNull(state.SpawnedObject, "SpawnedObject should be set");
			Assert.AreNotSame(_prefab, state.SpawnedObject, "Should be a new instance, not the prefab");

			UnityEngine.Object.Destroy(state.SpawnedObject);
		}

		[UnityTest]
		public IEnumerator SpawnPrefabState_SpawnsAtPosition()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.Position = new Vector3(10, 20, 30);

			state.OnEnter();

			yield return null;

			Assert.AreEqual(new Vector3(10, 20, 30), state.SpawnedObject.transform.position);

			UnityEngine.Object.Destroy(state.SpawnedObject);
		}

		[UnityTest]
		public IEnumerator SpawnPrefabState_SpawnsWithRotation()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.Rotation = Quaternion.Euler(0, 90, 0);

			state.OnEnter();

			yield return null;

			Assert.AreEqual(90f, state.SpawnedObject.transform.eulerAngles.y, 0.01f);

			UnityEngine.Object.Destroy(state.SpawnedObject);
		}

		[UnityTest]
		public IEnumerator SpawnPrefabState_ParentsToSpecifiedParent()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.Parent = _parent.transform;

			state.OnEnter();

			yield return null;

			Assert.AreEqual(_parent.transform, state.SpawnedObject.transform.parent);

			UnityEngine.Object.Destroy(state.SpawnedObject);
		}

		[UnityTest]
		public IEnumerator SpawnPrefabState_WithUseLocalPosition_SetsLocalPosition()
		{
			_parent.transform.position = new Vector3(100, 0, 0);

			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.Parent = _parent.transform;
			state.Position = new Vector3(10, 0, 0);
			state.UseLocalPosition = true;

			state.OnEnter();

			yield return null;

			Assert.AreEqual(new Vector3(10, 0, 0), state.SpawnedObject.transform.localPosition);

			UnityEngine.Object.Destroy(state.SpawnedObject);
		}



		[Test]
		public void SpawnPrefabState_InvokesOnComplete()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);

			if (state.SpawnedObject != null)
			{
				UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
			}
		}

		[Test]
		public void SpawnPrefabState_WithNullPrefab_LogsWarning_AndInvokesOnComplete()
		{
			var state = new SpawnPrefabState();
			state.Prefab = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[SpawnPrefabState] No prefab specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
			Assert.IsNull(state.SpawnedObject);
		}

		[Test]
		public void SpawnPrefabState_WithNullTransition_DoesNotThrow()
		{
			var state = new SpawnPrefabState();
			state.Prefab = _prefab;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());

			if (state.SpawnedObject != null)
			{
				UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
			}
		}



		[Test]
		public void SpawnPrefabState_DefaultPosition_IsZero()
		{
			var state = new SpawnPrefabState();

			Assert.AreEqual(Vector3.zero, state.Position);
		}

		[Test]
		public void SpawnPrefabState_DefaultRotation_IsIdentity()
		{
			var state = new SpawnPrefabState();

			Assert.AreEqual(Quaternion.identity, state.Rotation);
		}

		[Test]
		public void SpawnPrefabState_DefaultParentToContext_IsFalse()
		{
			var state = new SpawnPrefabState();

			Assert.IsFalse(state.ParentToContext);
		}

		[Test]
		public void SpawnPrefabState_DefaultUseLocalPosition_IsFalse()
		{
			var state = new SpawnPrefabState();

			Assert.IsFalse(state.UseLocalPosition);
		}

	}
}

using System;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class SpawnPrefabStateTests
	{
		private GameObject _prefab;

		[SetUp]
		public void SetUp()
		{
			_prefab = new GameObject("TestPrefab");
		}

		[TearDown]
		public void TearDown()
		{
			if (_prefab != null)
			{
				UnityEngine.Object.DestroyImmediate(_prefab);
			}
		}


		[Test]
		public void Prefab_Default_IsNull()
		{
			var state = new SpawnPrefabState();

			Assert.IsNull(state.Prefab);
		}

		[Test]
		public void Position_Default_IsZero()
		{
			var state = new SpawnPrefabState();

			Assert.AreEqual(Vector3.zero, state.Position);
		}

		[Test]
		public void Rotation_Default_IsIdentity()
		{
			var state = new SpawnPrefabState();

			Assert.AreEqual(Quaternion.identity, state.Rotation);
		}

		[Test]
		public void Parent_Default_IsNull()
		{
			var state = new SpawnPrefabState();

			Assert.IsNull(state.Parent);
		}

		[Test]
		public void ParentToContext_Default_IsFalse()
		{
			var state = new SpawnPrefabState();

			Assert.IsFalse(state.ParentToContext);
		}

		[Test]
		public void UseLocalPosition_Default_IsFalse()
		{
			var state = new SpawnPrefabState();

			Assert.IsFalse(state.UseLocalPosition);
		}

		[Test]
		public void SpawnedObject_Default_IsNull()
		{
			var state = new SpawnPrefabState();

			Assert.IsNull(state.SpawnedObject);
		}

		[Test]
		public void OnComplete_Default_IsNull()
		{
			var state = new SpawnPrefabState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void OnEnter_NullPrefab_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new SpawnPrefabState
			{
				Prefab = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[SpawnPrefabState] No prefab specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullPrefab_NullOnComplete_DoesNotThrow()
		{
			var state = new SpawnPrefabState
			{
				Prefab = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[SpawnPrefabState] No prefab specified");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnEnter_NullPrefab_SpawnedObjectRemainsNull()
		{
			var state = new SpawnPrefabState
			{
				Prefab = null,
				OnComplete = () => { }
			};

			LogAssert.Expect(LogType.Warning, "[SpawnPrefabState] No prefab specified");
			state.OnEnter();

			Assert.IsNull(state.SpawnedObject);
		}



		[Test]
		public void OnEnter_WithPrefab_SpawnsObject()
		{
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.IsNotNull(state.SpawnedObject);
			Assert.AreNotSame(_prefab, state.SpawnedObject);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}

		[Test]
		public void OnEnter_WithPrefab_CallsOnComplete()
		{
			var completeCalled = false;
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}

		[Test]
		public void OnEnter_WithPosition_SpawnsAtPosition()
		{
			var targetPos = new Vector3(10f, 20f, 30f);
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				Position = targetPos,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.AreEqual(targetPos, state.SpawnedObject.transform.position);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}

		[Test]
		public void OnEnter_WithRotation_SpawnsWithRotation()
		{
			var targetRot = Quaternion.Euler(45f, 90f, 0f);
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				Rotation = targetRot,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.AreEqual(targetRot.x, state.SpawnedObject.transform.rotation.x, 0.001f);
			Assert.AreEqual(targetRot.y, state.SpawnedObject.transform.rotation.y, 0.001f);
			Assert.AreEqual(targetRot.z, state.SpawnedObject.transform.rotation.z, 0.001f);
			Assert.AreEqual(targetRot.w, state.SpawnedObject.transform.rotation.w, 0.001f);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}



		[Test]
		public void OnEnter_WithExplicitParent_ParentsToTransform()
		{
			var parentObj = new GameObject("Parent");
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				Parent = parentObj.transform,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.AreSame(parentObj.transform, state.SpawnedObject.transform.parent);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
			UnityEngine.Object.DestroyImmediate(parentObj);
		}

		[Test]
		public void OnEnter_WithParentAndLocalPosition_SetsLocalTransform()
		{
			var parentObj = new GameObject("Parent");
			parentObj.transform.position = new Vector3(100f, 0f, 0f);
			var localPos = new Vector3(5f, 5f, 5f);
			var localRot = Quaternion.Euler(0f, 90f, 0f);

			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				Parent = parentObj.transform,
				Position = localPos,
				Rotation = localRot,
				UseLocalPosition = true,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.AreEqual(localPos.x, state.SpawnedObject.transform.localPosition.x, 0.01f);
			Assert.AreEqual(localPos.y, state.SpawnedObject.transform.localPosition.y, 0.01f);
			Assert.AreEqual(localPos.z, state.SpawnedObject.transform.localPosition.z, 0.01f);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
			UnityEngine.Object.DestroyImmediate(parentObj);
		}

		[Test]
		public void OnEnter_NoParent_SpawnsAtWorldPosition()
		{
			var worldPos = new Vector3(7f, 8f, 9f);
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				Position = worldPos,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.IsNull(state.SpawnedObject.transform.parent);
			Assert.AreEqual(worldPos, state.SpawnedObject.transform.position);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}

		[Test]
		public void OnEnter_ParentToContext_NoContext_SpawnsWithoutParent()
		{
			var state = new SpawnPrefabState
			{
				Prefab = _prefab,
				ParentToContext = true,
				OnComplete = () => { }
			};

			state.OnEnter();

			Assert.IsNull(state.SpawnedObject.transform.parent);

			UnityEngine.Object.DestroyImmediate(state.SpawnedObject);
		}



		[Test]
		public void Prefab_SetAndGet()
		{
			var state = new SpawnPrefabState();

			state.Prefab = _prefab;

			Assert.AreSame(_prefab, state.Prefab);
		}

		[Test]
		public void Position_SetAndGet()
		{
			var state = new SpawnPrefabState();

			state.Position = new Vector3(1f, 2f, 3f);

			Assert.AreEqual(new Vector3(1f, 2f, 3f), state.Position);
		}

		[Test]
		public void Rotation_SetAndGet()
		{
			var state = new SpawnPrefabState();
			var rot = Quaternion.Euler(30f, 60f, 90f);

			state.Rotation = rot;

			Assert.AreEqual(rot, state.Rotation);
		}

		[Test]
		public void ParentToContext_SetAndGet()
		{
			var state = new SpawnPrefabState();

			state.ParentToContext = true;

			Assert.IsTrue(state.ParentToContext);
		}

		[Test]
		public void UseLocalPosition_SetAndGet()
		{
			var state = new SpawnPrefabState();

			state.UseLocalPosition = true;

			Assert.IsTrue(state.UseLocalPosition);
		}

	}
}

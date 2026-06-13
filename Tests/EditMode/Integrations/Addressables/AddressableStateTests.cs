using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Addressables;
using Nonatomic.VSM.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nonatomic.VSM.Tests.Addressables
{
	[TestFixture]
	public class LoadAddressableStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(LoadAddressableState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(LoadAddressableState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Teal, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Download, stateStyle.Icon);
		}

		[Test]
		public void HasAssetReferenceDataIn_Required()
		{
			var field = typeof(LoadAddressableState).GetField("AssetReference");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Asset Reference", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasLoadedAssetDataOut()
		{
			var field = typeof(LoadAddressableState).GetField("LoadedAsset");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Loaded Asset", attr.DisplayName);
		}

		[Test]
		public void HasProgressDataOut()
		{
			var field = typeof(LoadAddressableState).GetField("Progress");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Progress", attr.DisplayName);
		}

		[Test]
		public void HasOnLoadedTransition()
		{
			var field = typeof(LoadAddressableState).GetField("OnLoaded");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Loaded", attr.DisplayName);
		}

		[Test]
		public void HasOnFailedTransition()
		{
			var field = typeof(LoadAddressableState).GetField("OnFailed");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Failed", attr.DisplayName);
		}

		[Test]
		public void AssetReferenceField_IsCorrectType()
		{
			var field = typeof(LoadAddressableState).GetField("AssetReference");

			Assert.IsNotNull(field);
			Assert.AreEqual(typeof(AssetReference), field.FieldType);
		}

		[Test]
		public void DefaultAssetReference_IsNull()
		{
			var state = new LoadAddressableState();

			Assert.IsNull(state.AssetReference);
		}

		[Test]
		public void DefaultLoadedAsset_IsNull()
		{
			var state = new LoadAddressableState();

			Assert.IsNull(state.LoadedAsset);
		}
	}

	[TestFixture]
	public class SpawnAddressableStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(SpawnAddressableState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(SpawnAddressableState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Teal, stateStyle.Color);
			Assert.AreEqual(NodeIcon.DownloadCircle, stateStyle.Icon);
		}

		[Test]
		public void HasAssetReferenceDataIn_Required()
		{
			var field = typeof(SpawnAddressableState).GetField("AssetReference");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Asset Reference", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasParentDataIn()
		{
			var field = typeof(SpawnAddressableState).GetField("Parent");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Parent", attr.DisplayName);
		}

		[Test]
		public void HasPositionDataIn()
		{
			var field = typeof(SpawnAddressableState).GetField("Position");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Position", attr.DisplayName);
		}

		[Test]
		public void HasRotationDataIn()
		{
			var field = typeof(SpawnAddressableState).GetField("Rotation");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Rotation", attr.DisplayName);
		}

		[Test]
		public void HasInstanceDataOut()
		{
			var field = typeof(SpawnAddressableState).GetField("Instance");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Instance", attr.DisplayName);
		}

		[Test]
		public void HasOnSpawnedTransition()
		{
			var field = typeof(SpawnAddressableState).GetField("OnSpawned");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Spawned", attr.DisplayName);
		}

		[Test]
		public void HasOnFailedTransition()
		{
			var field = typeof(SpawnAddressableState).GetField("OnFailed");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Failed", attr.DisplayName);
		}

		[Test]
		public void RotationDefaultsToIdentity()
		{
			var state = new SpawnAddressableState();

			Assert.AreEqual(Quaternion.identity, state.Rotation);
		}

		[Test]
		public void DefaultInstance_IsNull()
		{
			var state = new SpawnAddressableState();

			Assert.IsNull(state.Instance);
		}
	}

	[TestFixture]
	public class ReleaseAddressableStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(ReleaseAddressableState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(ReleaseAddressableState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Teal, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Trash, stateStyle.Icon);
		}

		[Test]
		public void HasInstanceDataIn_Required()
		{
			var field = typeof(ReleaseAddressableState).GetField("Instance");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Instance", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasOnCompleteTransition()
		{
			var field = typeof(ReleaseAddressableState).GetField("OnComplete");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void OnEnter_NullInstance_InvokesOnComplete()
		{
			var state = new ReleaseAddressableState { Instance = null };
			var done = false;
			state.OnComplete = () => done = true;

			state.OnEnter();

			Assert.IsTrue(done);
		}

		[Test]
		public void OnEnter_NullInstanceNullOnComplete_DoesNotThrow()
		{
			var state = new ReleaseAddressableState
			{
				Instance = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class AddressableStatesCommonTests
	{
		[Test]
		public void AllAddressableStates_HaveTealColor()
		{
			var stateTypes = new[]
			{
				typeof(LoadAddressableState),
				typeof(SpawnAddressableState),
				typeof(ReleaseAddressableState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.Teal, stateStyle.Color,
					$"{stateType.Name} should have Teal color");
			}
		}

		[Test]
		public void AllAddressableStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(LoadAddressableState),
				typeof(SpawnAddressableState),
				typeof(ReleaseAddressableState)
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

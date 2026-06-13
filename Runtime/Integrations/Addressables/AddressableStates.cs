using System;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Nonatomic.VSM.Addressables
{
	/// <summary>
	/// Loads an addressable asset asynchronously and transitions when complete.
	/// The loaded asset is available via the Loaded Asset output port.
	/// </summary>
	[StateInfo(
		"Loads an addressable asset",
		"Asynchronously loads an asset via its AssetReference. The loaded asset " +
		"is output for downstream states. Transitions to Loaded on success or " +
		"Failed on error. Supports cancellation via the state's CancellationToken."
	)]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.Download)]
	public class LoadAddressableState : State
	{
		/// <summary>
		/// The addressable asset reference to load.
		/// </summary>
		[DataIn("Asset Reference", Required = true)]
		public AssetReference AssetReference;

		/// <summary>
		/// The loaded asset. Available after the Loaded transition fires.
		/// </summary>
		[DataOut("Loaded Asset")]
		public Object LoadedAsset;

		/// <summary>
		/// Loading progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when the asset is loaded successfully.
		/// </summary>
		[Transition("Loaded")]
		public Action OnLoaded;

		/// <summary>
		/// Transition that fires when loading fails.
		/// </summary>
		[Transition("Failed")]
		public Action OnFailed;

		private AsyncOperationHandle<Object> _handle;
		private bool _hasHandle;

		public override async Task OnEnterAsync(CancellationToken ct)
		{
			LoadedAsset = null;
			Progress = 0f;

			if (AssetReference == null || !AssetReference.RuntimeKeyIsValid())
			{
				LogWarning("Invalid asset reference");
				OnFailed?.Invoke();
				return;
			}

			_handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Object>(AssetReference);
			_hasHandle = true;

			await _handle.Task;

			ct.ThrowIfCancellationRequested();

			Progress = 1f;

			if (_handle.Status == AsyncOperationStatus.Succeeded)
			{
				LoadedAsset = _handle.Result;
				OnLoaded?.Invoke();
			}
			else
			{
				LogWarning($"Failed to load addressable: {_handle.OperationException?.Message}");
				OnFailed?.Invoke();
			}
		}

		public override void OnExit()
		{
			if (_hasHandle && !_handle.IsValid())
			{
				_hasHandle = false;
			}
		}

		public override void OnReset()
		{
			ReleaseHandle();
		}

		private void ReleaseHandle()
		{
			if (_hasHandle && _handle.IsValid())
			{
				UnityEngine.AddressableAssets.Addressables.Release(_handle);
			}

			_hasHandle = false;
			LoadedAsset = null;
		}
	}

	/// <summary>
	/// Instantiates an addressable prefab asynchronously.
	/// The spawned instance is available via the Instance output port.
	/// </summary>
	[StateInfo(
		"Spawns an addressable prefab",
		"Asynchronously instantiates a prefab via its AssetReference. Optionally " +
		"parents the instance to the context transform. The spawned instance " +
		"is output for downstream states. Supports cancellation."
	)]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.DownloadCircle)]
	public class SpawnAddressableState : State
	{
		/// <summary>
		/// The addressable prefab reference to instantiate.
		/// </summary>
		[DataIn("Asset Reference", Required = true)]
		public AssetReference AssetReference;

		/// <summary>
		/// Optional parent transform. If null, the instance is placed at the root.
		/// </summary>
		[DataIn("Parent")]
		public Transform Parent;

		/// <summary>
		/// Spawn position in world space.
		/// </summary>
		[DataIn("Position")]
		public Vector3 Position;

		/// <summary>
		/// Spawn rotation.
		/// </summary>
		[DataIn("Rotation")]
		public Quaternion Rotation = Quaternion.identity;

		/// <summary>
		/// The spawned instance. Available after the Spawned transition fires.
		/// </summary>
		[DataOut("Instance")]
		public GameObject Instance;

		/// <summary>
		/// Transition that fires when the prefab is instantiated.
		/// </summary>
		[Transition("Spawned")]
		public Action OnSpawned;

		/// <summary>
		/// Transition that fires when instantiation fails.
		/// </summary>
		[Transition("Failed")]
		public Action OnFailed;

		public override async Task OnEnterAsync(CancellationToken ct)
		{
			Instance = null;

			if (AssetReference == null || !AssetReference.RuntimeKeyIsValid())
			{
				LogWarning("Invalid asset reference");
				OnFailed?.Invoke();
				return;
			}

			var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(
				AssetReference,
				Position,
				Rotation,
				Parent
			);

			await handle.Task;

			ct.ThrowIfCancellationRequested();

			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				Instance = handle.Result;
				OnSpawned?.Invoke();
			}
			else
			{
				LogWarning($"Failed to spawn addressable: {handle.OperationException?.Message}");
				OnFailed?.Invoke();
			}
		}
	}

	/// <summary>
	/// Releases an addressable instance that was previously spawned with SpawnAddressableState.
	/// </summary>
	[StateInfo(
		"Releases an addressable instance",
		"Releases a GameObject that was instantiated via Addressables. " +
		"This frees the associated memory and unloads the asset if no " +
		"other references exist. Transitions immediately after release."
	)]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.Trash)]
	public class ReleaseAddressableState : State
	{
		/// <summary>
		/// The addressable instance to release.
		/// </summary>
		[DataIn("Instance", Required = true)]
		public GameObject Instance;

		/// <summary>
		/// Transition that fires after release.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Instance == null)
			{
				LogWarning("No instance to release");
				OnComplete?.Invoke();
				return;
			}

			UnityEngine.AddressableAssets.Addressables.ReleaseInstance(Instance);
			OnComplete?.Invoke();
		}
	}
}

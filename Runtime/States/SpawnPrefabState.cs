using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that instantiates a prefab and immediately transitions.
	/// </summary>
	[StateInfo(
		"Spawns a prefab instance",
		"Instantiates a prefab at the specified position and rotation. Can optionally " +
		"parent the spawned object to the context GameObject or a specified parent. " +
		"Outputs the spawned GameObject for use by subsequent states."
	)]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.PlusCircle)]
	public class SpawnPrefabState : State
	{
		/// <summary>
		/// The prefab to instantiate.
		/// </summary>
		[DataIn("Prefab", Required = true)]
		public GameObject Prefab;

		/// <summary>
		/// Position to spawn at. If UseLocalPosition is true, this is relative to the parent.
		/// </summary>
		[DataIn("Position")]
		public Vector3 Position = Vector3.zero;

		/// <summary>
		/// Rotation to spawn with.
		/// </summary>
		[DataIn("Rotation")]
		public Quaternion Rotation = Quaternion.identity;

		/// <summary>
		/// Parent transform for the spawned object. If null and ParentToContext is true,
		/// uses the context GameObject's transform.
		/// </summary>
		[DataIn("Parent")]
		public Transform Parent;

		/// <summary>
		/// If true and Parent is null, parents to the context GameObject.
		/// </summary>
		[DataIn("Parent To Context")]
		public bool ParentToContext = false;

		/// <summary>
		/// If true, Position and Rotation are in local space relative to parent.
		/// </summary>
		[DataIn("Use Local Position")]
		public bool UseLocalPosition = false;

		/// <summary>
		/// The spawned GameObject.
		/// </summary>
		[DataOut("Spawned Object")]
		public GameObject SpawnedObject;

		/// <summary>
		/// Transition that fires after spawning.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Prefab == null)
			{
				LogWarning("No prefab specified");
				OnComplete?.Invoke();
				return;
			}

			var parent = Parent;
			if (parent == null && ParentToContext && Transform != null)
			{
				parent = Transform;
			}

			if (parent != null && UseLocalPosition)
			{
				SpawnedObject = UnityEngine.Object.Instantiate(Prefab, parent);
				SpawnedObject.transform.localPosition = Position;
				SpawnedObject.transform.localRotation = Rotation;
			}
			else if (parent != null)
			{
				SpawnedObject = UnityEngine.Object.Instantiate(Prefab, Position, Rotation, parent);
			}
			else
			{
				SpawnedObject = UnityEngine.Object.Instantiate(Prefab, Position, Rotation);
			}

			OnComplete?.Invoke();
		}
	}
}

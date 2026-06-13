using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that sets a transform's position and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets a transform's position",
		"Sets the position of the target transform to the specified value. " +
		"Can set either world or local position."
	)]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Cube)]
	public class SetPositionState : State
	{
		/// <summary>
		/// The target transform. If null, uses the context GameObject's transform.
		/// </summary>
		[DataIn("Target")]
		public Transform Target;

		/// <summary>
		/// The position to set.
		/// </summary>
		[DataIn("Position")]
		public Vector3 Position = Vector3.zero;

		/// <summary>
		/// If true, sets local position instead of world position.
		/// </summary>
		[DataIn("Use Local Space")]
		public bool UseLocalSpace = false;

		/// <summary>
		/// Transition that fires after setting position.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : Transform;

			if (target == null)
			{
				LogWarning("No target transform specified");
				OnComplete?.Invoke();
				return;
			}

			if (UseLocalSpace)
			{
				target.localPosition = Position;
			}
			else
			{
				target.position = Position;
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets a transform's rotation and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets a transform's rotation",
		"Sets the rotation of the target transform to the specified value. " +
		"Can set either world or local rotation using Euler angles."
	)]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Cube)]
	public class SetRotationState : State
	{
		/// <summary>
		/// The target transform. If null, uses the context GameObject's transform.
		/// </summary>
		[DataIn("Target")]
		public Transform Target;

		/// <summary>
		/// The rotation to set (Euler angles).
		/// </summary>
		[DataIn("Rotation")]
		public Vector3 EulerAngles = Vector3.zero;

		/// <summary>
		/// If true, sets local rotation instead of world rotation.
		/// </summary>
		[DataIn("Use Local Space")]
		public bool UseLocalSpace = false;

		/// <summary>
		/// Transition that fires after setting rotation.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : Transform;

			if (target == null)
			{
				LogWarning("No target transform specified");
				OnComplete?.Invoke();
				return;
			}

			if (UseLocalSpace)
			{
				target.localEulerAngles = EulerAngles;
			}
			else
			{
				target.eulerAngles = EulerAngles;
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets a transform's scale and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets a transform's scale",
		"Sets the local scale of the target transform to the specified value."
	)]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Cube)]
	public class SetScaleState : State
	{
		/// <summary>
		/// The target transform. If null, uses the context GameObject's transform.
		/// </summary>
		[DataIn("Target")]
		public Transform Target;

		/// <summary>
		/// The scale to set.
		/// </summary>
		[DataIn("Scale")]
		public Vector3 Scale = Vector3.one;

		/// <summary>
		/// Transition that fires after setting scale.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : Transform;

			if (target == null)
			{
				LogWarning("No target transform specified");
				OnComplete?.Invoke();
				return;
			}

			target.localScale = Scale;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that makes a transform look at a target and immediately transitions.
	/// </summary>
	[StateInfo(
		"Makes a transform look at a target",
		"Rotates the transform so its forward vector points at the target position or transform."
	)]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Cube)]
	public class LookAtState : State
	{
		/// <summary>
		/// The transform to rotate. If null, uses the context GameObject's transform.
		/// </summary>
		[DataIn("Source")]
		public Transform Source;

		/// <summary>
		/// The target transform to look at. Takes priority over TargetPosition.
		/// </summary>
		[DataIn("Target Transform")]
		public Transform TargetTransform;

		/// <summary>
		/// The position to look at. Used if TargetTransform is null.
		/// </summary>
		[DataIn("Target Position")]
		public Vector3 TargetPosition;

		/// <summary>
		/// The world up direction.
		/// </summary>
		[DataIn("World Up")]
		public Vector3 WorldUp = Vector3.up;

		/// <summary>
		/// Transition that fires after rotating.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var source = Source != null ? Source : Transform;

			if (source == null)
			{
				LogWarning("No source transform specified");
				OnComplete?.Invoke();
				return;
			}

			var targetPos = TargetTransform != null ? TargetTransform.position : TargetPosition;
			source.LookAt(targetPos, WorldUp);

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that translates a transform over time.
	/// </summary>
	[StateInfo(
		"Moves a transform over time",
		"Smoothly translates a transform from its current position to a target position " +
		"over a specified duration."
	)]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Cube)]
	public class MoveToState : State
	{
		/// <summary>
		/// The target transform. If null, uses the context GameObject's transform.
		/// </summary>
		[DataIn("Target")]
		public Transform Target;

		/// <summary>
		/// The destination position.
		/// </summary>
		[DataIn("Destination")]
		public Vector3 Destination;

		/// <summary>
		/// Duration of the movement in seconds.
		/// </summary>
		[DataIn("Duration")]
		public float Duration = 1f;

		/// <summary>
		/// If true, uses local position instead of world position.
		/// </summary>
		[DataIn("Use Local Space")]
		public bool UseLocalSpace = false;

		/// <summary>
		/// If true, uses unscaled time.
		/// </summary>
		[DataIn("Use Unscaled Time")]
		public bool UseUnscaledTime = false;

		/// <summary>
		/// Current progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when movement completes.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private Transform _target;
		private Vector3 _startPosition;
		private float _elapsed;

		public override void OnEnter()
		{
			_target = Target != null ? Target : Transform;

			if (_target == null)
			{
				LogWarning("No target transform specified");
				OnComplete?.Invoke();
				return;
			}

			_startPosition = UseLocalSpace ? _target.localPosition : _target.position;
			_elapsed = 0f;
			Progress = 0f;

			if (Duration <= 0f)
			{
				ApplyPosition(1f);
				OnComplete?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (_target == null || Duration <= 0f) return;

			var deltaTime = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			_elapsed += deltaTime;
			Progress = Mathf.Clamp01(_elapsed / Duration);

			ApplyPosition(Progress);

			if (_elapsed >= Duration)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			_target = null;
			_startPosition = Vector3.zero;
			_elapsed = 0f;
		}

		private void ApplyPosition(float t)
		{
			var position = Vector3.Lerp(_startPosition, Destination, t);

			if (UseLocalSpace)
			{
				_target.localPosition = position;
			}
			else
			{
				_target.position = position;
			}
		}
	}
}

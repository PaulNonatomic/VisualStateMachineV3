using Nonatomic.VSM.Core;
using Nonatomic.VSM.Samples.BuilderAPI.States;
using UnityEngine;

namespace Nonatomic.VSM.Samples.BuilderAPI
{
	/// <summary>
	/// Demonstrates creating a state machine entirely in code using the StateMachineBuilder API.
	/// No graph asset is required - the state machine is built programmatically at runtime.
	/// </summary>
	public class BuilderAPIController : MonoBehaviour
	{
		[Header("Configuration")]
		[SerializeField] private float _moveSpeed = 5f;
		[SerializeField] private float _jumpHeight = 2f;
		[SerializeField] private float _jumpDuration = 0.6f;

		private StateMachineInstance _stateMachine;

		private void Start()
		{
			// Build a complete state machine in code using the fluent builder API.
			// No graph asset needed - states, transitions, and variables are all defined here.
			var builder = StateMachineBuilder.Create(gameObject);

			// Add states - AddState returns a StateRef for positioning and chaining
			var idle = builder.AddState<IdleState>().AtPosition(100, 200);
			var move = builder.AddState<MoveState>().AtPosition(400, 100);
			var jump = builder.AddState<JumpState>().AtPosition(400, 300);

			// Define transitions using StateRef chaining
			idle.TransitionTo("On Move", move)
				.TransitionTo("On Jump", jump);

			move.TransitionTo("On Stop", idle)
				.TransitionTo("On Jump", jump);

			jump.TransitionTo("On Land", idle);

			// Add blackboard variables that bind to [DataIn] ports
			builder.AddVariable("Speed", _moveSpeed, exposed: true);
			builder.AddVariable("Jump Height", _jumpHeight, exposed: true);
			builder.AddVariable("Jump Duration", _jumpDuration, exposed: true);

			// Set entry state and build
			_stateMachine = builder
				.SetEntryState(idle)
				.BuildAndStart();

			_stateMachine.OnStateChanged += (prev, next) =>
			{
				Debug.Log($"[BuilderAPI] {prev?.GetType().Name ?? "None"} -> {next?.GetType().Name ?? "None"}");
			};
		}

		private void Update()
		{
			_stateMachine?.Update();
		}

		private void OnDestroy()
		{
			_stateMachine?.Stop();
		}
	}
}

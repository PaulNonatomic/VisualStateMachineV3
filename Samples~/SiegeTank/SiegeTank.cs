using System;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank
{
	/// <summary>
	/// Siege Tank component that serves as both the MonoBehaviour and the state machine context.
	///
	/// This pattern consolidates the runner and context into a single component:
	/// - States use State&lt;SiegeTank&gt; and access this component directly via SharedContext
	/// - All tank state and configuration lives on this component
	/// - Uses StateMachineController for clean state machine integration
	///
	/// This is cleaner than having separate Runner and Context classes because:
	/// - Single source of truth for tank data
	/// - States can access any public member of the tank
	/// - No need to wire up context references manually
	/// </summary>
	[AddComponentMenu("Visual State Machine/Samples/Siege Tank")]
	public class SiegeTank : MonoBehaviour
	{
		// Public event
		public event Action<State, State> OnStateChanged;

		// Public auto-properties
		public bool IsSieged { get; set; }
		public bool IsTransforming { get; set; }
		public Transform CurrentTarget { get; set; }
		public float LastFireTime { get; set; }
		public Vector3 MoveDestination { get; set; }
		public bool HasMoveOrder { get; set; }
		public float CurrentDamage { get; set; }
		public float CurrentRange { get; set; }
		public float CurrentFireRate { get; set; }

		// Public expression-bodied properties
		public SiegeTankConfig Config => _config;
		public Animator Animator => _animator;
		public StateMachineController<SiegeTank> StateMachine => _stateMachine;
		public bool IsRunning => _stateMachine?.IsRunning ?? false;
		public State CurrentState => _stateMachine?.CurrentState;

		// Serialized private fields
		[Header("State Machine")] [SerializeField]
		private SiegeTankGraph _graph;

		[Header("Configuration")] [SerializeField]
		private SiegeTankConfig _config = new SiegeTankConfig();

		[Header("References")] [SerializeField]
		private Animator _animator;

		// Private fields
		private StateMachineController<SiegeTank> _stateMachine;

		// Public methods
		public void StartStateMachine()
		{
			_stateMachine?.Start();
		}

		public void StopStateMachine()
		{
			_stateMachine?.Stop();
		}

		public void RestartStateMachine()
		{
			_stateMachine?.Restart();
		}

		public void PauseStateMachine()
		{
			_stateMachine?.Pause();
		}

		public void ResumeStateMachine()
		{
			_stateMachine?.Resume();
		}

		public void MoveTo(Vector3 destination)
		{
			if (IsSieged)
			{
				Debug.LogWarning("Cannot move while in Siege Mode!");
				return;
			}

			if (IsTransforming)
			{
				Debug.LogWarning("Cannot move while transforming!");
				return;
			}

			MoveDestination = destination;
			HasMoveOrder = true;
		}

		public void SetTarget(Transform target)
		{
			CurrentTarget = target;
		}

		public void ClearTarget()
		{
			CurrentTarget = null;
		}

		public void Stop()
		{
			HasMoveOrder = false;
		}

		public void ResetState()
		{
			IsSieged = false;
			IsTransforming = false;
			CurrentTarget = null;
			HasMoveOrder = false;
			LastFireTime = 0f;
		}

		// Private methods
		private void Awake()
		{
			if (_graph == null)
			{
				Debug.LogWarning($"SiegeTank on '{gameObject.name}' has no graph assigned.");
				return;
			}

			_stateMachine = new StateMachineController<SiegeTank>(_graph, this);
			_stateMachine.OnStateChanged += HandleStateChanged;
			_stateMachine.Start();
		}

		private void OnDestroy()
		{
			if (_stateMachine != null)
			{
				_stateMachine.OnStateChanged -= HandleStateChanged;
				_stateMachine.Dispose();
			}
		}

		private void Update()
		{
			_stateMachine?.Update();
		}

		private void FixedUpdate()
		{
			_stateMachine?.FixedUpdate();
		}

		private void LateUpdate()
		{
			_stateMachine?.LateUpdate();
		}

		private void HandleStateChanged(State previousState, State newState)
		{
			OnStateChanged?.Invoke(previousState, newState);
		}

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = IsSieged ? Color.red : Color.blue;
			Gizmos.DrawWireSphere(transform.position, CurrentRange > 0 ? CurrentRange : _config.tankModeRange);

			if (IsSieged)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(transform.position, _config.siegeModeMinRange);
			}

			if (HasMoveOrder)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(transform.position, MoveDestination);
				Gizmos.DrawSphere(MoveDestination, 0.5f);
			}

			if (CurrentTarget != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, CurrentTarget.position);
			}
		}
#endif
	}
}

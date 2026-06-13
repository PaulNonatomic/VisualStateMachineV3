using System;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Static callbacks for editor integration.
	/// The editor hooks into these to track runtime instances.
	/// </summary>
	public static class StateMachineControllerCallbacks
	{
		public static event Action<StateMachineInstance, StateMachineGraph, GameObject> OnControllerCreated;
		public static event Action<StateMachineInstance> OnControllerDisposed;

		/// <summary>
		/// Invokes the OnControllerCreated event.
		/// </summary>
		internal static void RaiseControllerCreated(StateMachineInstance instance, StateMachineGraph graph, GameObject owner)
		{
			OnControllerCreated?.Invoke(instance, graph, owner);
		}

		/// <summary>
		/// Invokes the OnControllerDisposed event.
		/// </summary>
		internal static void RaiseControllerDisposed(StateMachineInstance instance)
		{
			OnControllerDisposed?.Invoke(instance);
		}

		/// <summary>
		/// Removes all subscribers. Intended for test cleanup.
		/// </summary>
		public static void Reset()
		{
			OnControllerCreated = null;
			OnControllerDisposed = null;
		}
	}

	/// <summary>
	/// A non-MonoBehaviour helper class for running state machines.
	/// Use this when you want to integrate a state machine into an existing MonoBehaviour
	/// without inheritance.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class handles all the boilerplate of creating and managing a StateMachineInstance.
	/// Your MonoBehaviour just needs to call the lifecycle methods (Update, FixedUpdate, etc.)
	/// and this controller handles the rest.
	/// </para>
	/// <example>
	/// Basic usage with a typed context:
	/// <code>
	/// public class Enemy : MonoBehaviour
	/// {
	///     [SerializeField] private EnemyGraph _graph;
	///     private StateMachineController&lt;Enemy&gt; _stateMachine;
	///
	///     // Context data - states access these via SharedContext
	///     public float Health { get; set; } = 100f;
	///     public Transform Target { get; set; }
	///
	///     private void Awake()
	///     {
	///         // Pass 'this' as both the owner and the context
	///         _stateMachine = new StateMachineController&lt;Enemy&gt;(_graph, this);
	///         _stateMachine.Start();
	///     }
	///
	///     private void Update() => _stateMachine.Update();
	///     private void OnDestroy() => _stateMachine.Dispose();
	/// }
	/// </code>
	/// </example>
	/// </remarks>
	/// <typeparam name="TContext">The context type that states will access.</typeparam>
	public class StateMachineController<TContext> where TContext : class
	{
		private readonly StateMachineGraph _graph;
		private readonly TContext _context;
		private readonly GameObject _owner;
		private StateMachineInstance _instance;

		/// <summary>
		/// The underlying state machine instance.
		/// </summary>
		public StateMachineInstance Instance => _instance;

		/// <summary>
		/// The shared context accessible by states.
		/// </summary>
		public TContext Context => _context;

		/// <summary>
		/// Whether the state machine is currently running.
		/// </summary>
		public bool IsRunning => _instance?.IsRunning ?? false;

		/// <summary>
		/// Whether the state machine is paused.
		/// </summary>
		public bool IsPaused => _instance?.IsPaused ?? false;

		/// <summary>
		/// The currently active state.
		/// </summary>
		public State CurrentState => _instance?.CurrentState;

		/// <summary>
		/// The blackboard for this instance.
		/// </summary>
		public Blackboard.Blackboard Blackboard => _instance?.Blackboard;

		/// <summary>
		/// Fired when a state transition occurs.
		/// </summary>
		public event Action<State, State> OnStateChanged;

		/// <summary>
		/// Fired when the state machine starts.
		/// </summary>
		public event Action OnStarted;

		/// <summary>
		/// Fired when the state machine stops.
		/// </summary>
		public event Action OnStopped;

		/// <summary>
		/// Creates a new state machine controller.
		/// </summary>
		/// <param name="graph">The state machine graph to run.</param>
		/// <param name="context">The shared context for states. Often this is the MonoBehaviour itself.</param>
		/// <param name="owner">The GameObject that owns this state machine. If null and context is a Component, uses its gameObject.</param>
		public StateMachineController(StateMachineGraph graph, TContext context, GameObject owner = null)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			_context = context ?? throw new ArgumentNullException(nameof(context));

			// Try to get the owner from the context if not provided
			if (owner == null && context is Component component)
			{
				owner = component.gameObject;
			}

			_owner = owner;

			Initialize();
		}

		private void Initialize()
		{
			// Create the basic context for the instance
			var basicContext = _owner != null
				? new StateMachineContext(_owner)
				: new StateMachineContext();

			// Create the state machine instance with the shared context
			_instance = new StateMachineInstance(_graph, basicContext, _context);

			// Wire up events
			_instance.OnStateChanged += HandleStateChanged;
			_instance.OnStarted += HandleStarted;
			_instance.OnStopped += HandleStopped;

			// Notify listeners (editor hooks into this for graph visualization)
			StateMachineControllerCallbacks.RaiseControllerCreated(_instance, _graph, _owner);
		}

		/// <summary>
		/// Starts the state machine.
		/// </summary>
		public void Start()
		{
			_instance?.Start();
		}

		/// <summary>
		/// Stops the state machine.
		/// </summary>
		public void Stop()
		{
			_instance?.Stop();
		}

		/// <summary>
		/// Pauses the state machine (stops updates but doesn't exit current state).
		/// </summary>
		public void Pause()
		{
			_instance?.Pause();
		}

		/// <summary>
		/// Resumes a paused state machine.
		/// </summary>
		public void Resume()
		{
			_instance?.Resume();
		}

		/// <summary>
		/// Restarts the state machine from the beginning.
		/// </summary>
		public void Restart()
		{
			Stop();
			Start();
		}

		/// <summary>
		/// Call this from MonoBehaviour.Update().
		/// </summary>
		public void Update()
		{
			_instance?.Update();
		}

		/// <summary>
		/// Call this from MonoBehaviour.FixedUpdate().
		/// </summary>
		public void FixedUpdate()
		{
			_instance?.FixedUpdate();
		}

		/// <summary>
		/// Call this from MonoBehaviour.LateUpdate().
		/// </summary>
		public void LateUpdate()
		{
			_instance?.LateUpdate();
		}

		/// <summary>
		/// Sets a blackboard variable value.
		/// </summary>
		public void SetBlackboardValue<T>(string key, T value)
		{
			_instance?.Blackboard?.Set(key, value);
		}

		/// <summary>
		/// Gets a blackboard variable value.
		/// </summary>
		public T GetBlackboardValue<T>(string key, T defaultValue = default)
		{
			if (_instance?.Blackboard == null)
			{
				return defaultValue;
			}

			return _instance.Blackboard.Get(key, defaultValue);
		}

		/// <summary>
		/// Cleans up resources. Call this from OnDestroy().
		/// </summary>
		public void Dispose()
		{
			if (_instance != null)
			{
				// Notify listeners (editor hooks into this)
				StateMachineControllerCallbacks.RaiseControllerDisposed(_instance);

				_instance.OnStateChanged -= HandleStateChanged;
				_instance.OnStarted -= HandleStarted;
				_instance.OnStopped -= HandleStopped;
				_instance.Stop();
				_instance = null;
			}
		}

		private void HandleStateChanged(State previousState, State newState)
		{
			OnStateChanged?.Invoke(previousState, newState);
		}

		private void HandleStarted()
		{
			OnStarted?.Invoke();
		}

		private void HandleStopped()
		{
			OnStopped?.Invoke();
		}
	}

	/// <summary>
	/// Non-generic version of StateMachineController for state machines that don't use typed contexts.
	/// </summary>
	public class StateMachineController : StateMachineController<object>
	{
		/// <summary>
		/// Creates a new state machine controller without a typed context.
		/// </summary>
		/// <param name="graph">The state machine graph to run.</param>
		/// <param name="owner">The GameObject that owns this state machine.</param>
		public StateMachineController(StateMachineGraph graph, GameObject owner)
			: base(graph, new object(), owner)
		{
		}
	}
}

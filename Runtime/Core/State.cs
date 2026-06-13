using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Base class for all states in the state machine.
	/// States define behavior through lifecycle methods and expose
	/// input/output/transition ports via attributes.
	/// </summary>
	public abstract class State
	{
		/// <summary>
		/// The state machine instance this state belongs to.
		/// </summary>
		public StateMachineInstance StateMachine
		{
			get => _stateMachine;
			internal set => _stateMachine = value;
		}

		/// <summary>
		/// The context object providing shared data and references.
		/// </summary>
		public IStateMachineContext Context
		{
			get => _context;
			internal set => _context = value;
		}

		/// <summary>
		/// Direct access to the blackboard for reading/writing variables.
		/// </summary>
		public Blackboard.Blackboard Blackboard => _stateMachine?.Blackboard;

		/// <summary>
		/// Whether this state is currently active.
		/// </summary>
		public bool IsActive => _isActive;

		/// <summary>
		/// Convenience access to the owning GameObject.
		/// </summary>
		protected GameObject GameObject => _context?.GameObject;

		/// <summary>
		/// Convenience access to the owning Transform.
		/// </summary>
		protected Transform Transform => _context?.Transform;

		/// <summary>
		/// The node ID in the graph (used internally for connection lookups).
		/// </summary>
		internal string NodeId
		{
			get => _nodeId;
			set => _nodeId = value;
		}

		private StateMachineInstance _stateMachine;
		private IStateMachineContext _context;
		private string _nodeId;
		private bool _isActive;

		/// <summary>
		/// Called when entering this state.
		/// Input ports are populated before this is called.
		/// </summary>
		public virtual void OnEnter()
		{
		}

		/// <summary>
		/// Async version of OnEnter with cancellation support.
		/// The token is cancelled when this state exits or the state machine stops.
		/// Override this to use the cancellation token in async operations.
		/// </summary>
		/// <param name="ct">Token cancelled on state exit or state machine stop.</param>
		public virtual Task OnEnterAsync(CancellationToken ct)
		{
			return OnEnterAsync();
		}

		/// <summary>
		/// Async version of OnEnter for states that need to await operations.
		/// Override this instead of OnEnter for async entry logic.
		/// For cancellation support, override <see cref="OnEnterAsync(CancellationToken)"/> instead.
		/// </summary>
		public virtual Task OnEnterAsync()
		{
			OnEnter();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called every frame while this state is active.
		/// </summary>
		public virtual void OnUpdate()
		{
		}

		/// <summary>
		/// Async version of OnUpdate with cancellation support.
		/// The token is cancelled when this state exits or the state machine stops.
		/// Note: Use with caution as this is called every frame.
		/// </summary>
		/// <param name="ct">Token cancelled on state exit or state machine stop.</param>
		public virtual Task OnUpdateAsync(CancellationToken ct)
		{
			return OnUpdateAsync();
		}

		/// <summary>
		/// Async version of OnUpdate for states that need to await operations.
		/// Note: Use with caution as this is called every frame.
		/// For cancellation support, override <see cref="OnUpdateAsync(CancellationToken)"/> instead.
		/// </summary>
		public virtual Task OnUpdateAsync()
		{
			OnUpdate();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called at fixed intervals while this state is active.
		/// Use for physics-related updates.
		/// </summary>
		public virtual void OnFixedUpdate()
		{
		}

		/// <summary>
		/// Called at the end of each frame while this state is active.
		/// </summary>
		public virtual void OnLateUpdate()
		{
		}

		/// <summary>
		/// Called when exiting this state.
		/// Output port values are read after this returns.
		/// </summary>
		public virtual void OnExit()
		{
		}

		/// <summary>
		/// Async version of OnExit with cancellation support.
		/// The token is the instance-level token, cancelled when the state machine stops.
		/// State-level cancellation does not apply during exit since the state is already exiting.
		/// </summary>
		/// <param name="ct">Token cancelled on state machine stop.</param>
		public virtual Task OnExitAsync(CancellationToken ct)
		{
			return OnExitAsync();
		}

		/// <summary>
		/// Async version of OnExit for states that need to await operations.
		/// Override this instead of OnExit for async exit logic.
		/// For cancellation support, override <see cref="OnExitAsync(CancellationToken)"/> instead.
		/// </summary>
		public virtual Task OnExitAsync()
		{
			OnExit();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when the state machine instance is being returned to a pool.
		/// Override this to reset any persistent state held between activations.
		/// The base implementation does nothing.
		/// </summary>
		public virtual void OnReset()
		{
		}

		/// <summary>
		/// Gets a component from the context GameObject.
		/// </summary>
		protected T GetComponent<T>() where T : Component
		{
			return GameObject?.GetComponent<T>();
		}

		/// <summary>
		/// Gets or adds a component on the context GameObject.
		/// </summary>
		protected T GetOrAddComponent<T>() where T : Component
		{
			if (GameObject == null)
			{
				return null;
			}

			var component = GameObject.GetComponent<T>();
			if (component == null)
			{
				component = GameObject.AddComponent<T>();
			}

			return component;
		}

		/// <summary>
		/// Logs a message with the state name prefix.
		/// </summary>
		protected void Log(string message)
		{
			Debug.Log($"[{GetType().Name}] {message}");
		}

		/// <summary>
		/// Logs a warning with the state name prefix.
		/// </summary>
		protected void LogWarning(string message)
		{
			Debug.LogWarning($"[{GetType().Name}] {message}");
		}

		/// <summary>
		/// Logs an error with the state name prefix.
		/// </summary>
		protected void LogError(string message)
		{
			Debug.LogError($"[{GetType().Name}] {message}");
		}

		/// <summary>
		/// Called internally when the state is activated.
		/// </summary>
		internal void Activate()
		{
			_isActive = true;
		}

		/// <summary>
		/// Called internally when the state is deactivated.
		/// </summary>
		internal void Deactivate()
		{
			_isActive = false;
		}
	}
}

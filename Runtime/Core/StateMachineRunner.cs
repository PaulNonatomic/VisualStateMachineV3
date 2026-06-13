using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// MonoBehaviour component that runs a state machine instance.
	/// Attach this to a GameObject to execute a state machine graph.
	/// </summary>
	[AddComponentMenu("Visual State Machine/State Machine Runner")]
	public class StateMachineRunner : MonoBehaviour
	{
		/// <summary>
		/// Fired when a state transition occurs.
		/// </summary>
		public event Action<State, State> OnStateChanged;

		/// <summary>
		/// Per-instance overrides for exposed blackboard variables.
		/// </summary>
		public List<BlackboardOverride> VariableOverrides => _variableOverrides;

		/// <summary>
		/// The state machine graph to run.
		/// </summary>
		public StateMachineGraph Graph
		{
			get => _graph;
			set => _graph = value;
		}

		/// <summary>
		/// Whether to automatically start the state machine on Awake.
		/// </summary>
		public bool AutoStart
		{
			get => _autoStart;
			set => _autoStart = value;
		}

		/// <summary>
		/// Whether to use async lifecycle methods.
		/// </summary>
		public bool UseAsyncMethods
		{
			get => _useAsyncMethods;
			set => _useAsyncMethods = value;
		}

		/// <summary>
		/// When to call Update on the state machine.
		/// </summary>
		public UpdateMode UpdateMode
		{
			get => _updateMode;
			set => _updateMode = value;
		}

		/// <summary>
		/// The running state machine instance.
		/// </summary>
		public StateMachineInstance Instance => _instance;

		/// <summary>
		/// The context used by this runner.
		/// </summary>
		public IStateMachineContext Context => _context;

		/// <summary>
		/// Whether the state machine is currently running.
		/// </summary>
		public bool IsRunning => _instance?.IsRunning ?? false;

		/// <summary>
		/// The currently active state.
		/// </summary>
		public State CurrentState => _instance?.CurrentState;

		/// <summary>
		/// Direct access to the blackboard.
		/// </summary>
		public Blackboard.Blackboard Blackboard => _instance?.Blackboard;

		/// <summary>
		/// The explicitly assigned shared context object for State&lt;TContext&gt; states.
		/// When set, this object is provided to the state machine instance as typed context.
		/// </summary>
		public UnityEngine.Object SharedContextObject
		{
			get => _sharedContextObject;
			set => _sharedContextObject = value;
		}

		[SerializeField] private StateMachineGraph _graph;

		[SerializeField] private bool _autoStart = true;

		[SerializeField] private bool _useAsyncMethods;

		[SerializeField] private UpdateMode _updateMode = UpdateMode.Update;

		[SerializeField] private List<BlackboardOverride> _variableOverrides = new List<BlackboardOverride>();

		[SerializeField] private UnityEngine.Object _sharedContextObject;

		private Task _pendingUpdateTask;

		private StateMachineInstance _instance;
		private IStateMachineContext _context;

#if !UNITY_6000_0_OR_NEWER
		// Manual CancellationTokenSource for Unity 2022.3 LTS compatibility.
		// Unity 6+ provides MonoBehaviour.destroyCancellationToken natively;
		// on older versions we replicate this by creating our own CTS and
		// cancelling it in OnDestroy.
		private CancellationTokenSource _destroyCts;
#endif

		/// <summary>
		/// Returns a CancellationToken that is cancelled when this MonoBehaviour is destroyed.
		/// On Unity 6+ this delegates to the built-in destroyCancellationToken.
		/// On Unity 2022.3 LTS this returns a token from a manually managed CancellationTokenSource.
		/// </summary>
		protected CancellationToken GetDestroyCancellationToken()
		{
#if UNITY_6000_0_OR_NEWER
			return destroyCancellationToken;
#else
			_destroyCts ??= new CancellationTokenSource();
			return _destroyCts.Token;
#endif
		}

		/// <summary>
		/// Starts the state machine.
		/// </summary>
		public void StartStateMachine()
		{
			if (_instance == null)
			{
				Debug.LogError("Cannot start state machine: instance is null");
				return;
			}

			if (_useAsyncMethods)
			{
				SafeFireAndForget(_instance.StartAsync(), "StartAsync");
			}
			else
			{
				_instance.Start();
			}
		}

		/// <summary>
		/// Stops the state machine.
		/// </summary>
		public void StopStateMachine()
		{
			if (_instance == null)
			{
				return;
			}

			if (_useAsyncMethods)
			{
				SafeFireAndForget(_instance.StopAsync(), "StopAsync");
			}
			else
			{
				_instance.Stop();
			}
		}

		/// <summary>
		/// Restarts the state machine.
		/// </summary>
		public void RestartStateMachine()
		{
			StopStateMachine();
			StartStateMachine();
		}

		/// <summary>
		/// Pauses the state machine (stops updates but doesn't exit current state).
		/// </summary>
		public void PauseStateMachine()
		{
			_instance?.Pause();
		}

		/// <summary>
		/// Resumes a paused state machine.
		/// </summary>
		public void ResumeStateMachine()
		{
			_instance?.Resume();
		}

		/// <summary>
		/// Initializes the runner with a graph at runtime.
		/// </summary>
		public void Initialize(StateMachineGraph graph)
		{
			Initialize(graph, null);
		}

		/// <summary>
		/// Initializes the runner with a graph and context at runtime.
		/// </summary>
		public void Initialize(StateMachineGraph graph, IStateMachineContext context)
		{
			if (_instance != null)
			{
				_instance.OnStateChanged -= HandleStateChanged;
				_instance.Stop();
			}

			_graph = graph;

			if (_graph == null)
			{
				_instance = null;
				return;
			}

			_context = context ?? CreateContext();
			_instance = CreateInstance(_graph, _context);
			AutoResolveSharedContext();
			ApplyVariableOverrides();
			_instance.OnStateChanged += HandleStateChanged;
		}

		/// <summary>
		/// Reinitializes the state machine with the current graph.
		/// Useful when the graph has been changed at runtime.
		/// </summary>
		public void Reinitialize()
		{
			if (_instance != null)
			{
				_instance.OnStateChanged -= HandleStateChanged;
				_instance.Stop();
			}

			if (_graph == null)
			{
				_instance = null;
				return;
			}

			_context = CreateContext();
			_instance = CreateInstance(_graph, _context);
			AutoResolveSharedContext();
			ApplyVariableOverrides();
			_instance.OnStateChanged += HandleStateChanged;
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
		/// Creates the context for this runner.
		/// Override to provide custom context implementation.
		/// </summary>
		protected virtual IStateMachineContext CreateContext()
		{
			return new StateMachineContext(gameObject);
		}

		/// <summary>
		/// Creates the state machine instance.
		/// Override this to provide custom instance creation (e.g., with shared context).
		/// </summary>
		protected virtual StateMachineInstance CreateInstance(StateMachineGraph graph, IStateMachineContext context)
		{
			var instance = new StateMachineInstance(graph, context);
			instance.SetExternalCancellationToken(GetDestroyCancellationToken());
			return instance;
		}

		/// <summary>
		/// Resolves and applies the shared context to the instance.
		/// Prefers the serialized _sharedContextObject field; falls back to
		/// searching sibling components that match the required context type.
		/// Skipped when a shared context is already set (e.g., by StateMachineRunner&lt;T&gt;).
		/// </summary>
		private void AutoResolveSharedContext()
		{
			if (_instance == null || _instance.HasSharedContext) return;

			var contextType = _graph.ContextType ?? _graph.GetRequiredContextTypeFromStates();

			// Use the explicitly assigned context object if set
			if (_sharedContextObject != null)
			{
				_instance.SetSharedContext(_sharedContextObject, contextType);
				return;
			}

			// Fall back to searching sibling components
			if (contextType == null) return;
			if (!typeof(Component).IsAssignableFrom(contextType)) return;

			var sibling = GetComponent(contextType);
			if (sibling != null)
			{
				_instance.SetSharedContext(sibling, contextType);
			}
		}

		protected virtual void Awake()
		{
			if (_graph == null)
			{
				Debug.LogWarning($"StateMachineRunner on '{gameObject.name}' has no graph assigned");
				return;
			}

			_context = CreateContext();
			_instance = CreateInstance(_graph, _context);

			AutoResolveSharedContext();
			ApplyVariableOverrides();

			_instance.OnStateChanged += HandleStateChanged;

			// Notify editor tracker immediately so it can register before we start
			// This ensures state changes are tracked from the beginning
			StateMachineControllerCallbacks.RaiseControllerCreated(_instance, _graph, gameObject);

			if (_autoStart)
			{
				StartStateMachine();
			}
		}

		protected virtual void OnDestroy()
		{
			if (_instance != null)
			{
				// Notify editor tracker before disposing
				StateMachineControllerCallbacks.RaiseControllerDisposed(_instance);

				_instance.OnStateChanged -= HandleStateChanged;
				_instance.Stop();
			}

#if !UNITY_6000_0_OR_NEWER
			// Cancel and dispose the manual CTS that replicates
			// destroyCancellationToken for Unity 2022.3 LTS compatibility.
			_destroyCts?.Cancel();
			_destroyCts?.Dispose();
			_destroyCts = null;
#endif
		}

		protected virtual void Update()
		{
			if (_updateMode == UpdateMode.Update)
			{
				UpdateStateMachine();
			}
		}

		protected virtual void FixedUpdate()
		{
			if (_updateMode == UpdateMode.FixedUpdate)
			{
				UpdateStateMachine();
			}

			// Always forward FixedUpdate to the instance regardless of UpdateMode.
			// States may need physics-rate callbacks (OnFixedUpdate) even when the
			// main state machine update loop runs in Update or LateUpdate.
			_instance?.FixedUpdate();
		}

		protected virtual void LateUpdate()
		{
			if (_updateMode == UpdateMode.LateUpdate)
			{
				UpdateStateMachine();
			}

			// Always forward LateUpdate to the instance regardless of UpdateMode.
			// States may need late-frame callbacks (OnLateUpdate) for camera follow
			// or post-processing even when the main loop runs in Update or FixedUpdate.
			_instance?.LateUpdate();
		}

		private void ApplyVariableOverrides()
		{
			if (_instance?.Blackboard == null || _variableOverrides == null) return;

			foreach (var variableOverride in _variableOverrides)
			{
				if (string.IsNullOrEmpty(variableOverride.Key)) continue;

				var value = variableOverride.Value;
				if (value != null)
				{
					_instance.Blackboard.SetBoxed(variableOverride.Key, value, variableOverride.ValueType);
				}
			}
		}

		private void UpdateStateMachine()
		{
			if (_instance == null || !_instance.IsRunning)
			{
				return;
			}

			if (_useAsyncMethods)
			{
				if (_pendingUpdateTask == null || _pendingUpdateTask.IsCompleted)
				{
					_pendingUpdateTask = _instance.UpdateAsync();
					SafeFireAndForget(_pendingUpdateTask, "UpdateAsync");
				}
			}
			else
			{
				_instance.Update();
			}
		}

		private async void SafeFireAndForget(Task task, string context)
		{
			try
			{
				await task;
			}
			catch (OperationCanceledException)
			{
				// Expected during cleanup
			}
			catch (Exception ex)
			{
				Debug.LogError($"[StateMachineRunner] Unhandled exception in {context}: {ex}");
			}
		}

		private void HandleStateChanged(State previousState, State newState)
		{
			OnStateChanged?.Invoke(previousState, newState);
		}
	}

	/// <summary>
	/// Determines when the state machine Update method is called.
	/// </summary>
	public enum UpdateMode
	{
		/// <summary>
		/// Update is called in MonoBehaviour.Update().
		/// </summary>
		Update,

		/// <summary>
		/// Update is called in MonoBehaviour.FixedUpdate().
		/// </summary>
		FixedUpdate,

		/// <summary>
		/// Update is called in MonoBehaviour.LateUpdate().
		/// </summary>
		LateUpdate,

		/// <summary>
		/// Update is not called automatically. Call manually.
		/// </summary>
		Manual
	}
}

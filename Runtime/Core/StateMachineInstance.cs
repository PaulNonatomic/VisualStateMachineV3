using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Runtime instance of a state machine.
	/// Manages state lifecycle, transitions, and data binding.
	/// </summary>
	public class StateMachineInstance
	{
		/// <summary>
		/// Fired when a state transition occurs. Args: (previousState, newState)
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
		/// Fired when the state machine reaches an exit node.
		/// This is used by parent state machines to know when a child has completed.
		/// The string parameter is the exit node's ID (can be used to identify which exit was taken).
		/// </summary>
		public event Action<string> OnExited;

		/// <summary>
		/// Fired when a transition is triggered. Args: (fromNodeId, toNodeId)
		/// </summary>
		public event Action<string, string> OnTransitionTriggered;

		/// <summary>
		/// Fired when a transition is triggered with port details.
		/// Args: (fromNodeId, toNodeId, portName)
		/// </summary>
		public event Action<string, string, string> OnTransitionTriggeredDetailed;

		/// <summary>
		/// Fired when a child sub-state machine instance is created and started.
		/// Args: (childInstance, subGraph)
		/// </summary>
		public event Action<StateMachineInstance, StateMachineGraph> OnChildInstanceStarted;

		/// <summary>
		/// Fired when a child sub-state machine instance is stopped/completed.
		/// Args: (childInstance)
		/// </summary>
		public event Action<StateMachineInstance> OnChildInstanceStopped;

		private StateMachineGraph _graph;
		private Blackboard.Blackboard _blackboard;
		private IStateMachineContext _context;
		private StateMachineInstance _parent;
		private Dictionary<string, State> _stateInstances;
		private string _instanceId;

		// Shared context for State<TContext> states
		private object _sharedContext;
		private Type _sharedContextType;

		private State _currentState;
		private bool _isRunning;
		private bool _isPaused;
		private int _sameFrameTransitionCount;
		private int _lastTransitionFrame;

		// Sub-state machine tracking
		private StateMachineInstance _activeChildInstance;
		private string _activeSubMachineNodeId;
		private Action<string> _activeChildExitHandler;

		// Cached output values from completed SubStateMachineNodes
		// Populated during ApplyOutputMappings so subsequent states can read via Data connections
		private Dictionary<string, Dictionary<string, object>> _subMachineOutputCache;

		// Portal node lookup cache (ChannelId -> PortalInNode) for O(1) lookups
		private Dictionary<int, PortalInNode> _portalInNodeCache;

		// Reusable set for tracking bound fields during transitions (avoids per-transition allocation)
		private readonly HashSet<string> _boundFieldsCache = new HashSet<string>();

		// Tracks which transition port triggered the current transition
		private string _lastTransitionPortName;

		// Cached nesting depth to avoid parent chain traversal
		private int _nestingDepth;

		// Cancellation token sources for async lifecycle management
		private CancellationTokenSource _instanceCts;
		private CancellationTokenSource _stateCts;
		private CancellationToken _externalToken;

		/// <summary>
		/// Maximum number of same-frame transitions before protection kicks in.
		/// </summary>
		public int MaxSameFrameTransitions { get; set; } = 100;

		/// <summary>
		/// Maximum sub-state machine nesting depth before protection kicks in.
		/// </summary>
		public const int MaxSubStateMachineDepth = 16;

		/// <summary>
		/// When enabled, logs debug information about state transitions.
		/// Shows node name, graph name, and owner name for each transition.
		/// </summary>
		public static bool DebugMode { get; set; }

		/// <summary>
		/// The graph definition this instance is based on.
		/// </summary>
		public StateMachineGraph Graph => _graph;

		/// <summary>
		/// The blackboard for this instance.
		/// </summary>
		public Blackboard.Blackboard Blackboard => _blackboard;

		/// <summary>
		/// The context object providing shared data.
		/// </summary>
		public IStateMachineContext Context => _context;

		/// <summary>
		/// Parent state machine if this is a sub-state machine.
		/// </summary>
		public StateMachineInstance Parent => _parent;

		/// <summary>
		/// The currently active state.
		/// </summary>
		public State CurrentState => _currentState;

		/// <summary>
		/// The node ID of the current state.
		/// </summary>
		public string CurrentStateNodeId => _currentState?.NodeId;

		/// <summary>
		/// Whether the state machine is currently running.
		/// </summary>
		public bool IsRunning => _isRunning && !_isPaused;

		/// <summary>
		/// Whether the state machine is paused.
		/// </summary>
		public bool IsPaused => _isPaused;

		/// <summary>
		/// Unique identifier for this instance.
		/// </summary>
		public string InstanceId => _instanceId;

		/// <summary>
		/// The owning GameObject (from context).
		/// </summary>
		public GameObject Owner => _context?.GameObject;

		/// <summary>
		/// The shared context object for State&lt;TContext&gt; states.
		/// Null if no typed context was provided.
		/// </summary>
		public object SharedContext => _sharedContext;

		/// <summary>
		/// The type of the shared context, if one was provided.
		/// </summary>
		public Type SharedContextType => _sharedContextType;

		/// <summary>
		/// Whether this instance has a shared context for State&lt;TContext&gt; states.
		/// </summary>
		public bool HasSharedContext => _sharedContext != null;

		/// <summary>
		/// The currently active child state machine instance (if running a sub-state machine).
		/// </summary>
		public StateMachineInstance ActiveChildInstance => _activeChildInstance;

		/// <summary>
		/// Whether this instance is currently executing a sub-state machine.
		/// </summary>
		public bool IsInSubStateMachine => _activeChildInstance != null;

		/// <summary>
		/// The node ID of the currently active sub-state machine node (if running a sub-state machine).
		/// </summary>
		public string ActiveSubMachineNodeId => _activeSubMachineNodeId;

		/// <summary>
		/// Gets the current nesting depth (0 for root, 1 for first child, etc.).
		/// Cached at construction time to avoid parent chain traversal.
		/// </summary>
		public int NestingDepth => _nestingDepth;

		/// <summary>
		/// Sets an external cancellation token that will be linked with the instance's own token.
		/// When the external token is cancelled (e.g., when the owning MonoBehaviour is destroyed),
		/// all async operations in this instance will be cancelled.
		/// Must be called before Start().
		/// </summary>
		public void SetExternalCancellationToken(CancellationToken externalToken)
		{
			_externalToken = externalToken;
		}

		/// <summary>
		/// Creates a new state machine instance.
		/// </summary>
		public StateMachineInstance(
			StateMachineGraph graph,
			IStateMachineContext context = null,
			StateMachineInstance parent = null)
			: this(graph, context, null, parent)
		{
		}

		/// <summary>
		/// Creates a new state machine instance with a typed shared context.
		/// </summary>
		/// <param name="graph">The state machine graph definition.</param>
		/// <param name="context">The basic context (GameObject/Transform).</param>
		/// <param name="sharedContext">The typed shared context for State&lt;TContext&gt; states.</param>
		/// <param name="parent">Parent instance for sub-state machines.</param>
		public StateMachineInstance(
			StateMachineGraph graph,
			IStateMachineContext context,
			object sharedContext,
			StateMachineInstance parent = null)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			_context = context;
			_sharedContext = sharedContext;
			_sharedContextType = sharedContext?.GetType();
			_parent = parent;
			_instanceId = Guid.NewGuid().ToString();
			_stateInstances = new Dictionary<string, State>();

			// Cache nesting depth to avoid parent chain traversal on every access
			_nestingDepth = parent != null ? parent._nestingDepth + 1 : 0;

			// Initialize blackboard
			_blackboard = new Blackboard.Blackboard();

			// If we have a parent, set up blackboard hierarchy
			if (parent != null)
			{
				_blackboard.SetParent(parent.Blackboard);
			}

			// Initialize blackboard with definitions from graph
			InitializeBlackboard();

			// Create state instances
			CreateStateInstances();

			// Build portal node cache for O(1) lookups
			BuildPortalInNodeCache();
		}

		/// <summary>
		/// Sets the shared context for State&lt;TContext&gt; states.
		/// Must be called before Start() for proper initialization.
		/// </summary>
		/// <typeparam name="TContext">The context type.</typeparam>
		/// <param name="sharedContext">The shared context instance.</param>
		public void SetSharedContext<TContext>(TContext sharedContext) where TContext : class
		{
			_sharedContext = sharedContext;
			_sharedContextType = typeof(TContext);

			// Re-inject context into already created state instances
			foreach (var state in _stateInstances.Values)
			{
				InjectSharedContext(state);
			}
		}

		/// <summary>
		/// Sets the shared context using a runtime object.
		/// Use this when the context type is not known at compile time.
		/// </summary>
		/// <param name="sharedContext">The shared context instance.</param>
		/// <param name="contextType">
		/// Optional explicit type override. When null, the runtime type is inferred via GetType().
		/// Pass an explicit type when the declared variable type may differ from the desired context type
		/// (e.g., a field typed as UnityEngine.Object that actually holds a DoorController).
		/// </param>
		public void SetSharedContext(object sharedContext, Type contextType = null)
		{
			_sharedContext = sharedContext;
			_sharedContextType = contextType ?? sharedContext?.GetType();

			foreach (var state in _stateInstances.Values)
			{
				InjectSharedContext(state);
			}
		}


		/// <summary>
		/// Starts the state machine from the entry node.
		/// </summary>
		public void Start()
		{
			if (_isRunning)
			{
				Debug.LogWarning("State machine is already running");
				return;
			}

			// Validate context requirements before starting
			if (!ValidateContextRequirements(out var validationError))
			{
				Debug.LogError(validationError);
				return;
			}

			_isRunning = true;

			// Cancel any existing state CTS before disposing instance CTS to prevent leaks
			CancelStateCts();

			// Create instance-level CTS, linked to external token if provided
			_instanceCts?.Dispose();
			_instanceCts = _externalToken.CanBeCanceled
				? CancellationTokenSource.CreateLinkedTokenSource(_externalToken)
				: new CancellationTokenSource();

			var entryNodeId = _graph.EntryNodeId;
			if (string.IsNullOrEmpty(entryNodeId))
			{
				Debug.LogError("State machine has no entry node");
				_isRunning = false;
				return;
			}

			OnStarted?.Invoke();

			// Find the first connection from entry node (entry nodes only have transitions)
			var firstConnection = _graph.GetFirstConnectionFromNode(entryNodeId);
			if (firstConnection != null)
			{
				TransitionTo(firstConnection.TargetNodeId);
				return;
			}

			Debug.LogError("Entry node has no outgoing transition");
			_isRunning = false;
		}

		/// <summary>
		/// Starts the state machine asynchronously.
		/// </summary>
		public async Task StartAsync()
		{
			if (_isRunning)
			{
				Debug.LogWarning("State machine is already running");
				return;
			}

			// Validate context requirements before starting
			if (!ValidateContextRequirements(out var validationError))
			{
				Debug.LogError(validationError);
				return;
			}

			_isRunning = true;

			// Cancel any existing state CTS before disposing instance CTS to prevent leaks
			CancelStateCts();

			// Create instance-level CTS, linked to external token if provided
			_instanceCts?.Dispose();
			_instanceCts = _externalToken.CanBeCanceled
				? CancellationTokenSource.CreateLinkedTokenSource(_externalToken)
				: new CancellationTokenSource();

			var entryNodeId = _graph.EntryNodeId;
			if (string.IsNullOrEmpty(entryNodeId))
			{
				Debug.LogError("State machine has no entry node");
				_isRunning = false;
				return;
			}

			OnStarted?.Invoke();

			// Find the first connection from entry node (entry nodes only have transitions)
			var firstConnection = _graph.GetFirstConnectionFromNode(entryNodeId);
			if (firstConnection != null)
			{
				await TransitionToAsync(firstConnection.TargetNodeId);
				return;
			}

			Debug.LogError("Entry node has no outgoing transition");
			_isRunning = false;
		}

		/// <summary>
		/// Stops the state machine.
		/// </summary>
		public void Stop()
		{
			if (!_isRunning)
			{
				return;
			}

			// Set running to false first to prevent re-entry
			_isRunning = false;

			// Cancel all async operations
			CancelStateCts();
			CancelInstanceCts();

			// Stop any active child state machine
			if (_activeChildInstance != null)
			{
				if (_activeChildExitHandler != null)
				{
					_activeChildInstance.OnExited -= _activeChildExitHandler;
					_activeChildExitHandler = null;
				}

				_activeChildInstance.Stop();
				_activeChildInstance = null;
				_activeSubMachineNodeId = null;
			}

			if (_currentState != null)
			{
				_currentState.OnExit();
				_currentState.Deactivate();
				_currentState = null;
			}

			OnStopped?.Invoke();
		}

		/// <summary>
		/// Stops the state machine asynchronously.
		/// </summary>
		public async Task StopAsync()
		{
			if (!_isRunning)
			{
				return;
			}

			// Set running to false first to prevent re-entry
			_isRunning = false;

			// Cancel state-level operations first
			CancelStateCts();

			// Stop any active child state machine
			if (_activeChildInstance != null)
			{
				if (_activeChildExitHandler != null)
				{
					_activeChildInstance.OnExited -= _activeChildExitHandler;
					_activeChildExitHandler = null;
				}

				await _activeChildInstance.StopAsync();
				_activeChildInstance = null;
				_activeSubMachineNodeId = null;
			}

			// Exit current state with instance token (not yet cancelled)
			if (_currentState != null)
			{
				var instanceToken = _instanceCts?.Token ?? CancellationToken.None;
				try
				{
					await _currentState.OnExitAsync(instanceToken);
				}
				catch (OperationCanceledException)
				{
					// Expected if external token was cancelled during exit
				}
				catch (Exception ex)
				{
					Debug.LogError($"[{_currentState.GetType().Name}] Exception in OnExitAsync during Stop: {ex.Message}");
				}

				_currentState.Deactivate();
				_currentState = null;
			}

			// Cancel instance CTS after OnExit completes
			CancelInstanceCts();

			OnStopped?.Invoke();
		}

		/// <summary>
		/// Updates the current state. Call from MonoBehaviour.Update().
		/// </summary>
		public void Update()
		{
			if (!_isRunning || _isPaused)
			{
				return;
			}

			// If we're running a sub-state machine, update it instead
			if (_activeChildInstance != null)
			{
				_activeChildInstance.Update();
				return;
			}

			if (_currentState != null)
			{
				_currentState.OnUpdate();
			}
		}

		/// <summary>
		/// Updates the current state asynchronously.
		/// </summary>
		public async Task UpdateAsync()
		{
			if (!_isRunning || _isPaused || _currentState == null)
			{
				return;
			}

			// If we're running a sub-state machine, update it instead
			if (_activeChildInstance != null)
			{
				await _activeChildInstance.UpdateAsync();
				return;
			}

			var token = _stateCts?.Token ?? CancellationToken.None;

			try
			{
				await _currentState.OnUpdateAsync(token);
			}
			catch (OperationCanceledException)
			{
				// Expected when state exits or machine stops during update
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{_currentState?.GetType().Name}] Exception in OnUpdateAsync: {ex.Message}");
			}
		}

		/// <summary>
		/// Fixed update for the current state. Call from MonoBehaviour.FixedUpdate().
		/// </summary>
		public void FixedUpdate()
		{
			if (!_isRunning || _isPaused)
			{
				return;
			}

			// If we're running a sub-state machine, update it instead
			if (_activeChildInstance != null)
			{
				_activeChildInstance.FixedUpdate();
				return;
			}

			if (_currentState != null)
			{
				_currentState.OnFixedUpdate();
			}
		}

		/// <summary>
		/// Late update for the current state. Call from MonoBehaviour.LateUpdate().
		/// </summary>
		public void LateUpdate()
		{
			if (!_isRunning || _isPaused)
			{
				return;
			}

			// If we're running a sub-state machine, update it instead
			if (_activeChildInstance != null)
			{
				_activeChildInstance.LateUpdate();
				return;
			}

			if (_currentState != null)
			{
				_currentState.OnLateUpdate();
			}
		}

		/// <summary>
		/// Pauses the state machine. Updates will not be processed while paused.
		/// </summary>
		public void Pause()
		{
			if (!_isRunning || _isPaused)
			{
				return;
			}

			_isPaused = true;

			// Also pause any active child
			_activeChildInstance?.Pause();
		}

		/// <summary>
		/// Resumes a paused state machine.
		/// </summary>
		public void Resume()
		{
			if (!_isRunning || !_isPaused)
			{
				return;
			}

			_isPaused = false;

			// Also resume any active child
			_activeChildInstance?.Resume();
		}



		private enum TransitionValidationResult { Continue, Stop, Abort }

		private TransitionValidationResult ValidateAndRecordTransition(string targetNodeId, out GraphNode node)
		{
			node = null;
			if (!_isRunning) return TransitionValidationResult.Abort;

			var currentFrame = Time.frameCount;
			if (currentFrame == _lastTransitionFrame)
			{
				_sameFrameTransitionCount++;
				if (_sameFrameTransitionCount > MaxSameFrameTransitions)
				{
					Debug.LogError($"State machine exceeded {MaxSameFrameTransitions} same-frame transitions. " +
								   "Possible infinite loop detected. Stopping state machine.");
					return TransitionValidationResult.Stop;
				}
			}
			else
			{
				_lastTransitionFrame = currentFrame;
				_sameFrameTransitionCount = 1;
			}

			var fromNodeId = _currentState?.NodeId;
			OnTransitionTriggered?.Invoke(fromNodeId, targetNodeId);
			OnTransitionTriggeredDetailed?.Invoke(fromNodeId, targetNodeId, _lastTransitionPortName);
			_lastTransitionPortName = null;

			node = _graph.GetNode(targetNodeId);
			if (node == null)
			{
				Debug.LogError($"Target node '{targetNodeId}' not found");
				return TransitionValidationResult.Abort;
			}

			return TransitionValidationResult.Continue;
		}

		/// <summary>
		/// Transitions to a specific node by ID.
		/// </summary>
		internal void TransitionTo(string targetNodeId)
		{
			var result = ValidateAndRecordTransition(targetNodeId, out var node);
			if (result == TransitionValidationResult.Abort) return;
			if (result == TransitionValidationResult.Stop)
			{
				Stop();
				return;
			}

			switch (node.NodeType)
			{
				case GraphNodeType.State:
					TransitionToState(targetNodeId);
					break;
				case GraphNodeType.SubStateMachine:
					TransitionToSubStateMachine(targetNodeId);
					break;
				case GraphNodeType.Exit:
					HandleExitNode(targetNodeId);
					break;
				case GraphNodeType.PortalOut:
					HandlePortalOutNode(targetNodeId);
					break;
				default:
					Debug.LogError($"Cannot transition to node type: {node.NodeType}");
					break;
			}
		}

		/// <summary>
		/// Transitions to a specific node asynchronously.
		/// </summary>
		internal async Task TransitionToAsync(string targetNodeId)
		{
			var result = ValidateAndRecordTransition(targetNodeId, out var node);
			if (result == TransitionValidationResult.Abort) return;
			if (result == TransitionValidationResult.Stop)
			{
				await StopAsync();
				return;
			}

			switch (node.NodeType)
			{
				case GraphNodeType.State:
					await TransitionToStateAsync(targetNodeId);
					break;
				case GraphNodeType.SubStateMachine:
					await TransitionToSubStateMachineAsync(targetNodeId);
					break;
				case GraphNodeType.Exit:
					HandleExitNode(targetNodeId);
					break;
				case GraphNodeType.PortalOut:
					await HandlePortalOutNodeAsync(targetNodeId);
					break;
				default:
					Debug.LogError($"Cannot transition to node type: {node.NodeType}");
					break;
			}
		}

		private void TransitionToState(string stateNodeId)
		{
			if (!_stateInstances.TryGetValue(stateNodeId, out var newState))
			{
				Debug.LogError($"State instance not found for node '{stateNodeId}'");
				return;
			}

			var previousState = _currentState;
			var ownerName = _context?.GameObject?.name ?? "Unknown";

			// Cancel the current state's async operations
			CancelStateCts();

			// Exit current state and write outputs
			if (_currentState != null)
			{
				if (DebugMode)
				{
					Debug.Log($"[SM:{_graph.name}] [{ownerName}] EXIT: {_currentState.GetType().Name}");
				}
				_currentState.OnExit();
				WriteOutputsToBlackboard(_currentState);
				_currentState.Deactivate();
			}

			// Set new state
			_currentState = newState;
			_currentState.Activate();

			// Create new state-level CTS linked to instance CTS
			CreateStateCts();

			// Cache type for binding and debug logging
			var newStateType = _currentState.GetType();

			// Bind inputs and transitions before entering
			BindInputsFromBlackboard(_currentState, newStateType);
			BindTransitions(_currentState, newStateType);

			if (DebugMode)
			{
				Debug.Log($"[SM:{_graph.name}] [{ownerName}] ENTER: {newStateType.Name} (NodeId: {stateNodeId})");
			}

			// Enter new state
			_currentState.OnEnter();

			OnStateChanged?.Invoke(previousState, _currentState);
		}

		private async Task TransitionToStateAsync(string stateNodeId)
		{
			if (!_stateInstances.TryGetValue(stateNodeId, out var newState))
			{
				Debug.LogError($"State instance not found for node '{stateNodeId}'");
				return;
			}

			var previousState = _currentState;
			var instanceToken = _instanceCts?.Token ?? CancellationToken.None;

			// Cancel the current state's async operations
			CancelStateCts();

			// Exit current state and write outputs
			if (_currentState != null)
			{
				try
				{
					await _currentState.OnExitAsync(instanceToken);
				}
				catch (OperationCanceledException)
				{
					// Instance was stopped during exit - abort transition
					if (!_isRunning) return;
				}
				catch (Exception ex)
				{
					Debug.LogError($"[{_currentState.GetType().Name}] Exception in OnExitAsync: {ex.Message}");
				}

				WriteOutputsToBlackboard(_currentState);
				_currentState.Deactivate();
			}

			// Set new state
			_currentState = newState;
			_currentState.Activate();

			// Create new state-level CTS linked to instance CTS
			CreateStateCts();

			// Cache type for binding and debug logging
			var newStateType = _currentState.GetType();

			// Bind inputs and transitions before entering
			BindInputsFromBlackboard(_currentState, newStateType);
			BindTransitions(_currentState, newStateType);

			var stateToken = _stateCts?.Token ?? CancellationToken.None;

			// Enter new state
			try
			{
				await _currentState.OnEnterAsync(stateToken);
			}
			catch (OperationCanceledException)
			{
				// State was exited or instance stopped during enter - expected
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{newStateType.Name}] Exception in OnEnterAsync: {ex.Message}");
			}

			OnStateChanged?.Invoke(previousState, _currentState);
		}

		private bool ValidateSubStateMachineTransition(string nodeId, out SubStateMachineNode subMachineNode, out StateMachineGraph subGraph)
		{
			subMachineNode = _graph.GetNode<SubStateMachineNode>(nodeId);
			subGraph = null;

			if (subMachineNode == null)
			{
				Debug.LogError($"SubStateMachineNode not found: {nodeId}");
				return false;
			}

			subGraph = ResolveSubGraphFromConnections(nodeId) ?? subMachineNode.SubGraph;
			if (subGraph == null)
			{
				Debug.LogError($"SubStateMachineNode '{nodeId}' has no sub-graph assigned");
				return false;
			}

			if (NestingDepth >= MaxSubStateMachineDepth)
			{
				Debug.LogError($"SubStateMachine nesting depth exceeded maximum of {MaxSubStateMachineDepth}. " +
							   $"Possible circular reference or excessive nesting.");
				return false;
			}

			if (IsGraphInParentChain(subGraph))
			{
				Debug.LogError($"Circular sub-state machine reference detected: '{subGraph.name}' is already " +
							   $"in the parent chain. This would cause infinite recursion.");
				return false;
			}

			var subContextType = subGraph.ContextType;
			if (subContextType != null)
			{
				if (_sharedContext == null)
				{
					Debug.LogError($"SubStateMachine '{subMachineNode.DisplayName}' requires context type " +
								   $"'{subContextType.Name}' but no context was provided");
					return false;
				}

				if (!subContextType.IsAssignableFrom(_sharedContextType))
				{
					Debug.LogError($"SubStateMachine '{subMachineNode.DisplayName}' requires context type " +
								   $"'{subContextType.Name}' but provided context is '{_sharedContextType.Name}'");
					return false;
				}
			}

			return true;
		}

		private void TransitionToSubStateMachine(string nodeId)
		{
			if (!ValidateSubStateMachineTransition(nodeId, out var subMachineNode, out var subGraph))
			{
				ContinueFromSubStateMachineNode(nodeId);
				return;
			}

			if (DebugMode)
			{
				var ownerName = _context?.GameObject?.name ?? "Unknown";
				Debug.Log($"[SM:{_graph.name}] [{ownerName}] ENTER SubStateMachine: {subMachineNode.DisplayName} (NodeId: {nodeId})");
			}

			// Cancel the current state's async operations
			CancelStateCts();

			// Exit current state if any
			if (_currentState != null)
			{
				_currentState.OnExit();
				WriteOutputsToBlackboard(_currentState);
				_currentState.Deactivate();
				_currentState = null;
			}

			// Create child instance with appropriate blackboard handling
			_activeSubMachineNodeId = nodeId;
			_activeChildInstance = new StateMachineInstance(subGraph, _context, _sharedContext, this);

			// Pass our instance token to the child so it cancels when we stop
			if (_instanceCts != null)
			{
				_activeChildInstance.SetExternalCancellationToken(_instanceCts.Token);
			}

			// Apply input mappings if configured
			ApplyInputMappings(subMachineNode);

			// Subscribe to child's exit event
			_activeChildExitHandler = OnChildExited;
			_activeChildInstance.OnExited += _activeChildExitHandler;

			// Notify listeners that a child instance has started BEFORE starting
			// (so the child can be registered before it potentially completes)
			if (DebugMode)
			{
				Debug.Log($"[SM:{_graph.name}] Invoking OnChildInstanceStarted for {subGraph?.name}, subscribers={OnChildInstanceStarted?.GetInvocationList()?.Length ?? 0}");
			}
			OnChildInstanceStarted?.Invoke(_activeChildInstance, subGraph);

			// Start the child
			_activeChildInstance.Start();
		}

		private async Task TransitionToSubStateMachineAsync(string nodeId)
		{
			if (!ValidateSubStateMachineTransition(nodeId, out var subMachineNode, out var subGraph))
			{
				await ContinueFromSubStateMachineNodeAsync(nodeId);
				return;
			}

			// Cancel the current state's async operations
			CancelStateCts();

			// Exit current state if any
			if (_currentState != null)
			{
				var instanceToken = _instanceCts?.Token ?? CancellationToken.None;

				try
				{
					await _currentState.OnExitAsync(instanceToken);
				}
				catch (OperationCanceledException)
				{
					if (!_isRunning) return;
				}
				catch (Exception ex)
				{
					Debug.LogError($"[{_currentState.GetType().Name}] Exception in OnExitAsync: {ex.Message}");
				}

				WriteOutputsToBlackboard(_currentState);
				_currentState.Deactivate();
				_currentState = null;
			}

			// Create child instance with appropriate blackboard handling
			_activeSubMachineNodeId = nodeId;
			_activeChildInstance = new StateMachineInstance(subGraph, _context, _sharedContext, this);

			// Pass our instance token to the child so it cancels when we stop
			if (_instanceCts != null)
			{
				_activeChildInstance.SetExternalCancellationToken(_instanceCts.Token);
			}

			// Apply input mappings if configured
			ApplyInputMappings(subMachineNode);

			// Subscribe to child's exit event
			_activeChildExitHandler = OnChildExitedAsync;
			_activeChildInstance.OnExited += _activeChildExitHandler;

			// Notify listeners that a child instance has started BEFORE starting
			// (so the child can be registered before it potentially completes)
			if (DebugMode)
			{
				Debug.Log($"[SM:{_graph.name}] Invoking OnChildInstanceStarted for {subGraph?.name}, subscribers={OnChildInstanceStarted?.GetInvocationList()?.Length ?? 0}");
			}
			OnChildInstanceStarted?.Invoke(_activeChildInstance, subGraph);

			// Start the child
			await _activeChildInstance.StartAsync();
		}

		/// <summary>
		/// Resolves the SubGraph for a SubStateMachineNode from any incoming connections.
		/// Returns null if no connection provides a SubGraph.
		/// </summary>
		private StateMachineGraph ResolveSubGraphFromConnections(string nodeId)
		{
			// Look for a connection to the SubGraph port
			var subGraphConnections = _graph.GetConnectionsToNode(nodeId);
			for (var i = 0; i < subGraphConnections.Count; i++)
			{
				var connection = subGraphConnections[i];
				if (connection.TargetPortName != "SubGraph")
					continue;

				// Check if source is a BlackboardVariableNode
				var varNode = _graph.GetNode<BlackboardVariableNode>(connection.SourceNodeId);
				if (varNode != null)
				{
					var value = _blackboard.GetBoxed(varNode.VariableKey);
					if (value is StateMachineGraph graph)
					{
						return graph;
					}
				}

				// Check if source is a State's output port
				if (_stateInstances.TryGetValue(connection.SourceNodeId, out var sourceState))
				{
					var sourceType = sourceState.GetType();
					var sourceField = FindFieldByPortName(
						sourceType,
						GetCachedFields(sourceType),
						connection.SourcePortName);
					if (sourceField != null)
					{
						var value = sourceField.GetValue(sourceState);
						if (value is StateMachineGraph graph)
						{
							return graph;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Applies input variable mappings from parent to child blackboard.
		/// </summary>
		private void ApplyInputMappings(SubStateMachineNode subMachineNode)
		{
			HashSet<string> connectedKeys = null;

			// Explicit inspector-configured mappings
			foreach (var mapping in subMachineNode.InputMappings)
			{
				if (mapping.Direction == VariableMappingDirection.ParentToChild ||
					mapping.Direction == VariableMappingDirection.Bidirectional)
				{
					var value = _blackboard.GetBoxed(mapping.ParentVariableKey);
					if (value != null)
					{
						_activeChildInstance.Blackboard.SetBoxed(
							mapping.ChildVariableKey,
							value,
							value.GetType());

						connectedKeys ??= new HashSet<string>();
						connectedKeys.Add(mapping.ChildVariableKey);
					}
				}
			}

			// Connection-based mappings from exposed input ports
			// Resolves by source node type rather than ConnectionType to handle stale connections
			// that were serialized before DetermineConnectionType fixes
			var nodeId = subMachineNode.Id;
			var inputConns = _graph.GetConnectionsToNode(nodeId);
			for (var i = 0; i < inputConns.Count; i++)
			{
				var connection = inputConns[i];
				// Skip the Enter (transition) and SubGraph ports
				if (connection.TargetPortName == "Enter" || connection.TargetPortName == "SubGraph")
					continue;

				var childKey = connection.TargetPortName;
				object value = null;

				// Try source as BlackboardVariableNode
				var varNode = _graph.GetNode<BlackboardVariableNode>(connection.SourceNodeId);
				if (varNode != null)
				{
					value = _blackboard.GetBoxed(varNode.VariableKey);
				}
				// Try source as a State with a DataOut field
				else if (_stateInstances.TryGetValue(connection.SourceNodeId, out var sourceState))
				{
					var sourceType = sourceState.GetType();
					var sourceField = FindFieldByPortName(
						sourceType,
						GetCachedFields(sourceType),
						connection.SourcePortName);

					if (sourceField != null)
					{
						value = sourceField.GetValue(sourceState);
					}
				}
				// Try source as a completed SubStateMachineNode
				else if (_subMachineOutputCache != null &&
						 _subMachineOutputCache.TryGetValue(connection.SourceNodeId, out var portCache) &&
						 portCache.TryGetValue(connection.SourcePortName, out var cachedValue))
				{
					value = cachedValue;
				}

				if (value != null)
				{
					_activeChildInstance.Blackboard.SetBoxed(childKey, value, value.GetType());
					connectedKeys ??= new HashSet<string>();
					connectedKeys.Add(childKey);
				}
			}

			// Inline port values for unconnected exposed inputs
			foreach (var portValue in subMachineNode.ExposedPortValues)
			{
				if (connectedKeys != null && connectedKeys.Contains(portValue.FieldName))
					continue;

				var value = portValue.GetValue();
				if (value != null)
				{
					_activeChildInstance.Blackboard.SetBoxed(portValue.FieldName, value, value.GetType());
				}
			}
		}

		/// <summary>
		/// Applies output variable mappings from child to parent blackboard.
		/// </summary>
		private void ApplyOutputMappings(SubStateMachineNode subMachineNode)
		{
			// Explicit inspector-configured mappings
			foreach (var mapping in subMachineNode.OutputMappings)
			{
				if (mapping.Direction == VariableMappingDirection.ChildToParent ||
					mapping.Direction == VariableMappingDirection.Bidirectional)
				{
					var value = _activeChildInstance.Blackboard.GetBoxed(mapping.ChildVariableKey);
					if (value != null)
					{
						_blackboard.SetBoxed(mapping.ParentVariableKey, value, value.GetType());
					}
				}
			}

			// Connection-based mappings from exposed output ports
			var nodeId = subMachineNode.Id;
			var outputConns = _graph.GetConnectionsFromNode(nodeId);
			for (var i = 0; i < outputConns.Count; i++)
			{
				var connection = outputConns[i];
				// Skip the Exit (transition) port
				if (connection.SourcePortName == "Exit") continue;

				// BlackboardWrite: target is a BlackboardVariableNode (Set mode)
				if (connection.ConnectionType == ConnectionType.BlackboardWrite)
				{
					var varNode = _graph.GetNode<BlackboardVariableNode>(connection.TargetNodeId);
					if (varNode == null) continue;

					var childKey = connection.SourcePortName;
					var value = _activeChildInstance.Blackboard.GetBoxed(childKey);
					if (value != null)
					{
						_blackboard.SetBoxed(varNode.VariableKey, value, value.GetType());
					}
				}

				// Data: target is a State or SubStateMachineNode input port
				// Cache the value so BindInputsFromGraphConnections / ApplyInputMappings can read it
				if (connection.ConnectionType == ConnectionType.Data)
				{
					var childKey = connection.SourcePortName;
					var value = _activeChildInstance.Blackboard.GetBoxed(childKey);
					if (value != null)
					{
						_subMachineOutputCache ??= new Dictionary<string, Dictionary<string, object>>();
						if (!_subMachineOutputCache.TryGetValue(nodeId, out var portCache))
						{
							portCache = new Dictionary<string, object>();
							_subMachineOutputCache[nodeId] = portCache;
						}
						portCache[childKey] = value;
					}
				}
			}
		}

		/// <summary>
		/// Performs common cleanup when a child state machine instance exits.
		/// Unsubscribes from events, applies output mappings, clears child references,
		/// and fires the OnChildInstanceStopped event.
		/// Returns the sub-machine node ID for continuation.
		/// </summary>
		private string CleanupChildInstance()
		{
			var subMachineNodeId = _activeSubMachineNodeId;
			var subMachineNode = _graph.GetNode<SubStateMachineNode>(subMachineNodeId);

			if (_activeChildExitHandler != null)
			{
				_activeChildInstance.OnExited -= _activeChildExitHandler;
				_activeChildExitHandler = null;
			}

			if (subMachineNode != null)
			{
				ApplyOutputMappings(subMachineNode);
			}

			var stoppedChild = _activeChildInstance;
			_activeChildInstance = null;
			_activeSubMachineNodeId = null;

			// Fire event after cleanup so listeners can unregister
			OnChildInstanceStopped?.Invoke(stoppedChild);

			return subMachineNodeId;
		}

		/// <summary>
		/// Called when the child state machine exits.
		/// </summary>
		private void OnChildExited(string exitNodeId)
		{
			if (_activeChildInstance == null) return;

			var subMachineNodeId = CleanupChildInstance();

			// Continue from the sub-state machine's output
			ContinueFromSubStateMachineNode(subMachineNodeId);
		}

		/// <summary>
		/// Called when the child state machine exits (async version).
		/// Performs cleanup synchronously, then schedules async continuation via SafeFireAndForget.
		/// </summary>
		private void OnChildExitedAsync(string exitNodeId)
		{
			if (_activeChildInstance == null) return;

			var subMachineNodeId = CleanupChildInstance();

			// Continue from the sub-state machine's output using async transition
			SafeFireAndForget(
				ContinueFromSubStateMachineNodeAsync(subMachineNodeId),
				"ContinueFromSubStateMachineNodeAsync");
		}

		/// <summary>
		/// Finds the exit connection from a sub-state machine node.
		/// Returns null if no exit connection exists.
		/// </summary>
		private Connection FindSubMachineExitConnection(string subMachineNodeId)
		{
			var connections = _graph.GetConnectionsFromNode(subMachineNodeId);

			// First try to find a Transition connection (the correct type)
			Connection exitConnection = null;
			for (var i = 0; i < connections.Count; i++)
			{
				if (connections[i].ConnectionType == ConnectionType.Transition)
				{
					exitConnection = connections[i];
					break;
				}
			}

			// Fallback: if no Transition found, use the first connection from "Exit" port
			// This handles graphs created before the port type fix where connections were incorrectly typed as Data
			if (exitConnection == null)
			{
				for (var i = 0; i < connections.Count; i++)
				{
					if (connections[i].SourcePortName == "Exit")
					{
						exitConnection = connections[i];
						break;
					}
				}
			}

			if (exitConnection == null)
			{
				Debug.LogWarning($"SubStateMachine node '{subMachineNodeId}' has no exit connection");
			}

			return exitConnection;
		}

		/// <summary>
		/// Continues execution after a sub-state machine completes.
		/// </summary>
		private void ContinueFromSubStateMachineNode(string subMachineNodeId)
		{
			var exitConnection = FindSubMachineExitConnection(subMachineNodeId);
			if (exitConnection != null)
			{
				TransitionTo(exitConnection.TargetNodeId);
			}
		}

		/// <summary>
		/// Continues execution after a sub-state machine completes (async version).
		/// </summary>
		private async Task ContinueFromSubStateMachineNodeAsync(string subMachineNodeId)
		{
			var exitConnection = FindSubMachineExitConnection(subMachineNodeId);
			if (exitConnection != null)
			{
				await TransitionToAsync(exitConnection.TargetNodeId);
			}
		}

		/// <summary>
		/// Checks if the given graph is already being executed in the parent chain.
		/// Used to detect circular sub-state machine references.
		/// </summary>
		private bool IsGraphInParentChain(StateMachineGraph graph)
		{
			// Check current graph
			if (_graph == graph)
			{
				return true;
			}

			// Check parent chain
			var current = _parent;
			while (current != null)
			{
				if (current._graph == graph)
				{
					return true;
				}
				current = current._parent;
			}

			return false;
		}

		private void HandleExitNode(string nodeId)
		{
			if (DebugMode)
			{
				var ownerName = _context?.GameObject?.name ?? "Unknown";
				var exitNode = _graph.GetNode(nodeId) as ExitNode;
				var exitName = exitNode?.ExitName ?? "Exit";
				Debug.Log($"[SM:{_graph.name}] [{ownerName}] ENTER EXIT NODE: {exitName} (NodeId: {nodeId})");
			}

			// Fire the OnExited event before stopping
			// This allows the parent to handle continuation
			OnExited?.Invoke(nodeId);

			// Stop the state machine
			Stop();
		}

		/// <summary>
		/// Handles transitioning through a PortalOut node to its paired PortalIn.
		/// </summary>
		private void HandlePortalOutNode(string portalOutNodeId)
		{
			var portalOutNode = _graph.GetNode(portalOutNodeId) as PortalOutNode;
			if (portalOutNode == null)
			{
				Debug.LogError($"PortalOut node '{portalOutNodeId}' not found");
				return;
			}

			// Find the matching PortalIn node by channel (O(1) lookup using cache)
			if (!_portalInNodeCache.TryGetValue(portalOutNode.ChannelId, out var matchingPortalIn))
			{
				Debug.LogError($"No PortalIn node found for channel {portalOutNode.ChannelId}");
				return;
			}

			// Fire transition event for the PortalIn node so it gets highlighted
			OnTransitionTriggered?.Invoke(portalOutNodeId, matchingPortalIn.Id);

			// Follow the connection from the PortalIn node's output port
			var portalInConnections = _graph.GetConnectionsFromNode(matchingPortalIn.Id);
			Connection continueConnection = null;
			for (var i = 0; i < portalInConnections.Count; i++)
			{
				if (portalInConnections[i].ConnectionType == ConnectionType.Transition)
				{
					continueConnection = portalInConnections[i];
					break;
				}
			}

			if (continueConnection != null)
			{
				TransitionTo(continueConnection.TargetNodeId);
			}
			else
			{
				Debug.LogWarning($"PortalIn node (channel {matchingPortalIn.ChannelId}) has no outgoing connection");
			}
		}

		/// <summary>
		/// Handles transitioning through a PortalOut node to its paired PortalIn asynchronously.
		/// </summary>
		private async Task HandlePortalOutNodeAsync(string portalOutNodeId)
		{
			var portalOutNode = _graph.GetNode(portalOutNodeId) as PortalOutNode;
			if (portalOutNode == null)
			{
				Debug.LogError($"PortalOut node '{portalOutNodeId}' not found");
				return;
			}

			// Find the matching PortalIn node by channel (O(1) lookup using cache)
			if (!_portalInNodeCache.TryGetValue(portalOutNode.ChannelId, out var matchingPortalIn))
			{
				Debug.LogError($"No PortalIn node found for channel {portalOutNode.ChannelId}");
				return;
			}

			// Fire transition event for the PortalIn node so it gets highlighted
			OnTransitionTriggered?.Invoke(portalOutNodeId, matchingPortalIn.Id);

			// Follow the connection from the PortalIn node's output port
			var portalInConnections = _graph.GetConnectionsFromNode(matchingPortalIn.Id);
			Connection continueConnection = null;
			for (var i = 0; i < portalInConnections.Count; i++)
			{
				if (portalInConnections[i].ConnectionType == ConnectionType.Transition)
				{
					continueConnection = portalInConnections[i];
					break;
				}
			}

			if (continueConnection != null)
			{
				await TransitionToAsync(continueConnection.TargetNodeId);
			}
			else
			{
				Debug.LogWarning($"PortalIn node (channel {matchingPortalIn.ChannelId}) has no outgoing connection");
			}
		}

		/// <summary>
		/// Gets fields for a type using cached reflection results.
		/// </summary>
		private static FieldInfo[] GetCachedFields(Type type)
		{
			return ReflectionCache.GetFields(type);
		}

		private static readonly ConcurrentDictionary<string, string> LowerKeyCache = new();

		private static string GetCachedLowerKey(string key)
		{
			return LowerKeyCache.GetOrAdd(key, static k => k.ToLowerInvariant());
		}

		private void InitializeBlackboard()
		{
			foreach (var definition in _graph.BlackboardDefinitions)
			{
				_blackboard.DefineVariable(definition);
			}
		}

		private void CreateStateInstances()
		{
			foreach (var node in _graph.GetNodesOfType<StateNode>())
			{
				node.RestoreType();

				if (node.StateType == null)
				{
					Debug.LogError($"Could not resolve state type: {node.StateTypeAssemblyQualifiedName}");
					continue;
				}

				// Validate that the type actually inherits from State before instantiation
				// This prevents arbitrary type instantiation from corrupted/malicious asset data
				if (!typeof(State).IsAssignableFrom(node.StateType))
				{
					Debug.LogError($"Type '{node.StateType.FullName}' does not inherit from State. Skipping node '{node.DisplayName}'.");
					continue;
				}

				var state = (State)Activator.CreateInstance(node.StateType);
				state.StateMachine = this;
				state.Context = _context;
				state.NodeId = node.Id;

				// Inject shared context into State<TContext> states
				InjectSharedContext(state);

				_stateInstances[node.Id] = state;
			}
		}

		/// <summary>
		/// Builds a lookup cache of PortalIn nodes indexed by ChannelId for O(1) lookups.
		/// </summary>
		private void BuildPortalInNodeCache()
		{
			_portalInNodeCache = new Dictionary<int, PortalInNode>();

			foreach (var node in _graph.GetAllNodes())
			{
				if (node is PortalInNode portalIn)
				{
					_portalInNodeCache[portalIn.ChannelId] = portalIn;
				}
			}
		}

		/// <summary>
		/// Injects the shared context into a State&lt;TContext&gt; state instance.
		/// Uses IContextReceiver interface for fast injection without reflection.
		/// </summary>
		private void InjectSharedContext(State state)
		{
			if (_sharedContext == null) return;

			// Use interface for fast context injection (no reflection)
			if (state is IContextReceiver contextReceiver)
			{
				var requiredContextType = contextReceiver.RequiredContextType;
				if (requiredContextType == null) return;

				// Check if our shared context is compatible
				if (requiredContextType.IsAssignableFrom(_sharedContextType))
				{
					contextReceiver.SetContextUntyped(_sharedContext);
				}
				else
				{
					Debug.LogWarning(
						$"State '{state.GetType().Name}' requires context of type '{requiredContextType.Name}' " +
						$"but provided context is of type '{_sharedContextType.Name}'");
				}
			}
		}

		/// <summary>
		/// Gets the context type required by a state, if it inherits from State&lt;TContext&gt;.
		/// Returns null if the state doesn't require a typed context.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Type> RequiredContextTypeCache = new();

		internal static Type GetRequiredContextType(Type stateType)
		{
			return RequiredContextTypeCache.GetOrAdd(stateType, static t =>
			{
				var baseType = t.BaseType;
				while (baseType != null && baseType != typeof(object))
				{
					if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(State<>))
					{
						return baseType.GetGenericArguments()[0];
					}

					baseType = baseType.BaseType;
				}

				return null;
			});
		}

		/// <summary>
		/// Validates that all states requiring typed context have compatible context provided.
		/// </summary>
		/// <param name="error">Error message if validation fails.</param>
		/// <returns>True if validation passes, false otherwise.</returns>
		private bool ValidateContextRequirements(out string error)
		{
			error = null;
			var statesRequiringContext = new List<(string stateName, Type requiredType)>();

			foreach (var state in _stateInstances.Values)
			{
				var stateType = state.GetType();
				var requiredType = GetRequiredContextType(stateType);
				if (requiredType != null)
				{
					statesRequiringContext.Add((stateType.Name, requiredType));
				}
			}

			if (statesRequiringContext.Count == 0)
			{
				// No states require context
				return true;
			}

			if (_sharedContext == null)
			{
				// States require context but none was provided
				var stateNames = string.Join(", ", statesRequiringContext.Select(s => s.stateName));
				var requiredTypes = statesRequiringContext.Select(s => s.requiredType).Distinct().ToList();
				var typeNames = string.Join(" or ", requiredTypes.Select(t => t.Name));

				error = $"State machine '{_graph.name}' cannot start: States [{stateNames}] require context of type '{typeNames}' " +
						$"but no shared context was provided. Use StateMachineRunner<{requiredTypes[0].Name}> or call SetSharedContext() before Start().";
				return false;
			}

			// Check for type compatibility
			var incompatibleStates = new List<(string stateName, Type requiredType)>();
			foreach (var (stateName, requiredType) in statesRequiringContext)
			{
				if (!requiredType.IsAssignableFrom(_sharedContextType))
				{
					incompatibleStates.Add((stateName, requiredType));
				}
			}

			if (incompatibleStates.Count > 0)
			{
				var stateNames = string.Join(", ", incompatibleStates.Select(s => s.stateName));
				var requiredTypes = string.Join(", ", incompatibleStates.Select(s => s.requiredType.Name).Distinct());

				error = $"State machine '{_graph.name}' cannot start: States [{stateNames}] require context of type '{requiredTypes}' " +
						$"but provided context is of type '{_sharedContextType.Name}' which is not compatible.";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets information about context requirements for this state machine.
		/// Useful for editor tooling and debugging.
		/// </summary>
		/// <returns>List of state names and their required context types.</returns>
		public IReadOnlyList<(string StateName, Type RequiredContextType)> GetContextRequirements()
		{
			var requirements = new List<(string, Type)>();

			foreach (var state in _stateInstances.Values)
			{
				var stateType = state.GetType();
				var requiredType = GetRequiredContextType(stateType);
				if (requiredType != null)
				{
					requirements.Add((stateType.Name, requiredType));
				}
			}

			return requirements;
		}



		/// <summary>
		/// Binds input fields from graph connections, property overrides, and [DataIn] attributes.
		/// </summary>
		private void BindInputsFromBlackboard(State state, Type stateType = null)
		{
			if (state == null) return;

			stateType ??= state.GetType();
			var nodeId = state.NodeId;

			// Reuse cached set to avoid per-transition allocation
			_boundFieldsCache.Clear();

			// First, bind from graph connections (highest priority - explicit connections)
			BindInputsFromGraphConnections(state, stateType, nodeId, _boundFieldsCache);

			// Second, bind from property overrides (inline field values for unconnected ports)
			BindInputsFromPropertyOverrides(state, stateType, nodeId, _boundFieldsCache);

			// Third, bind from [DataIn] attributes (blackboard key matching - fallback)
			BindInputsFromAttributes(state, stateType, _boundFieldsCache);
		}

		/// <summary>
		/// Binds inputs from property overrides stored in the StateNode (inline field values).
		/// </summary>
		private void BindInputsFromPropertyOverrides(State state, Type stateType, string nodeId, HashSet<string> boundFields)
		{
			var stateNode = _graph.GetNode<StateNode>(nodeId);
			if (stateNode == null || stateNode.PropertyOverrides == null) return;

			foreach (var propertyOverride in stateNode.PropertyOverrides)
			{
				// Skip if already bound from a connection
				if (boundFields.Contains(propertyOverride.FieldName)) continue;

				// Find the target field using cached O(1) lookup
				var field = ReflectionCache.GetField(stateType, propertyOverride.FieldName);
				if (field == null) continue;

				// Get the value from the override
				var value = propertyOverride.GetValue();
				if (value != null && field.FieldType.IsAssignableFrom(value.GetType()))
				{
					field.SetValue(state, value);
					boundFields.Add(field.Name);
				}
			}
		}

		/// <summary>
		/// Binds inputs from graph connections (BlackboardVariableNode to State).
		/// </summary>
		private void BindInputsFromGraphConnections(State state, Type stateType, string nodeId, HashSet<string> boundFields)
		{
			var fields = GetCachedFields(stateType);

			// Resolves by source node type rather than ConnectionType to handle stale connections
			// that were serialized before DetermineConnectionType fixes
			var graphInputConnections = _graph.GetConnectionsToNode(nodeId);
			for (var i = 0; i < graphInputConnections.Count; i++)
			{
				var connection = graphInputConnections[i];
				// Find the target field - try by name first, then by DataIn display name
				var field = FindFieldByPortName(stateType, fields, connection.TargetPortName);
				if (field == null) continue;

				object value = null;

				// Try source as BlackboardVariableNode
				var varNode = _graph.GetNode<BlackboardVariableNode>(connection.SourceNodeId);
				if (varNode != null)
				{
					value = _blackboard.GetBoxed(varNode.VariableKey);
				}
				// Try source as a State with a DataOut field
				else if (_stateInstances.TryGetValue(connection.SourceNodeId, out var sourceState))
				{
					var sourceType = sourceState.GetType();
					var sourceField = FindFieldByPortName(
						sourceType,
						GetCachedFields(sourceType),
						connection.SourcePortName);
					if (sourceField != null)
					{
						value = sourceField.GetValue(sourceState);
					}
				}
				// Try source as a completed SubStateMachineNode
				else if (_subMachineOutputCache != null &&
						 _subMachineOutputCache.TryGetValue(connection.SourceNodeId, out var portCache) &&
						 portCache.TryGetValue(connection.SourcePortName, out var cachedValue))
				{
					value = cachedValue;
				}

				if (value != null && field.FieldType.IsAssignableFrom(value.GetType()))
				{
					field.SetValue(state, value);
					boundFields.Add(field.Name);
				}
			}
		}

		/// <summary>
		/// Finds a field by port name, checking both field name and DataIn/DataOut display names.
		/// </summary>
		private FieldInfo FindFieldByPortName(Type stateType, FieldInfo[] fields, string portName)
		{
			// First try exact field name match using cached O(1) lookup
			var field = ReflectionCache.GetField(stateType, portName);
			if (field != null) return field;

			// Try matching by DataIn attribute DisplayName
			foreach (var f in fields)
			{
				var dataInAttr = ReflectionCache.GetAttribute<DataInAttribute>(f);
				if (dataInAttr != null && dataInAttr.DisplayName == portName)
				{
					return f;
				}

				var dataOutAttr = ReflectionCache.GetAttribute<DataOutAttribute>(f);
				if (dataOutAttr != null && dataOutAttr.DisplayName == portName)
				{
					return f;
				}
			}

			return null;
		}

		private bool TryBindFieldFromBlackboard(FieldInfo field, string key, State state)
		{
			if (!_blackboard.TryGetBoxed(key, out var value)) return false;

			if (value != null && field.FieldType.IsAssignableFrom(value.GetType()))
			{
				field.SetValue(state, value);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Binds inputs from [DataIn] attributes for ports not connected in graph.
		/// </summary>
		private void BindInputsFromAttributes(State state, Type stateType, HashSet<string> boundFields)
		{
			var fields = GetCachedFields(stateType);

			foreach (var field in fields)
			{
				var inputAttr = ReflectionCache.GetAttribute<DataInAttribute>(field);
				if (inputAttr == null) continue;

				// Skip if already bound from graph connection
				if (boundFields.Contains(field.Name)) continue;

				// Try display name first, then field name
				var key = !string.IsNullOrEmpty(inputAttr.DisplayName)
					? inputAttr.DisplayName
					: field.Name;

				// Try to get value from blackboard
				if (TryBindFieldFromBlackboard(field, key, state)) continue;

				// Also try lowercase version of key (cached to avoid per-transition allocation)
				var lowerKey = GetCachedLowerKey(key);
				if (lowerKey != key && TryBindFieldFromBlackboard(field, lowerKey, state)) continue;

				// Also try field name if different from display name
				if (!string.IsNullOrEmpty(inputAttr.DisplayName))
				{
					TryBindFieldFromBlackboard(field, field.Name, state);
				}
			}
		}

		/// <summary>
		/// Binds transition Action fields to trigger state transitions.
		/// </summary>
		private void BindTransitions(State state, Type stateType = null)
		{
			if (state == null) return;

			var nodeId = state.NodeId;
			stateType ??= state.GetType();
			var fields = GetCachedFields(stateType);
			var nodeConnections = _graph.GetConnectionsFromNode(nodeId);

			foreach (var field in fields)
			{
				var transitionAttr = ReflectionCache.GetAttribute<TransitionAttribute>(field);
				if (transitionAttr == null) continue;

				if (field.FieldType != typeof(Action))
				{
					continue;
				}

				// Find the connection for this transition port
				// Try matching by field name first (new format), then by display name (old format)
				var fieldName = field.Name;
				var displayName = transitionAttr.DisplayName ?? StringFormatUtils.FormatTransitionName(fieldName);
				Connection targetConnection = null;

				for (var i = 0; i < nodeConnections.Count; i++)
				{
					var connection = nodeConnections[i];
					if (connection.ConnectionType == ConnectionType.Transition &&
						(connection.SourcePortName == fieldName || connection.SourcePortName == displayName))
					{
						targetConnection = connection;
						break;
					}
				}

				if (targetConnection != null)
				{
					var targetNodeId = targetConnection.TargetNodeId;
					var capturedFieldName = fieldName;
					field.SetValue(state, (Action)(() =>
					{
						_lastTransitionPortName = capturedFieldName;
						TransitionTo(targetNodeId);
					}));
				}
			}
		}

		/// <summary>
		/// Writes output fields to blackboard via graph connections first,
		/// then falls back to attribute-based key matching for unconnected outputs.
		/// </summary>
		private void WriteOutputsToBlackboard(State state, Type stateType = null)
		{
			if (state == null) return;

			var nodeId = state.NodeId;
			if (string.IsNullOrEmpty(nodeId)) return;

			stateType ??= state.GetType();
			var fields = ReflectionCache.GetFieldsWithAttribute<DataOutAttribute>(stateType);
			var allFields = GetCachedFields(stateType);
			var connectedFields = _boundFieldsCache;
			connectedFields.Clear();

			// First pass: write via BlackboardWrite graph connections
			var writeConnections = _graph.GetConnectionsFromNode(nodeId);
			for (var i = 0; i < writeConnections.Count; i++)
			{
				var connection = writeConnections[i];
				if (connection.ConnectionType != ConnectionType.BlackboardWrite)
				{
					continue;
				}

				var field = FindFieldByPortName(stateType, allFields, connection.SourcePortName);
				if (field == null) continue;

				var varNode = _graph.GetNode<BlackboardVariableNode>(connection.TargetNodeId);
				if (varNode == null) continue;

				var value = field.GetValue(state);
				_blackboard.SetBoxed(varNode.VariableKey, value, field.FieldType);
				connectedFields.Add(field.Name);
			}

			// Second pass: attribute-based fallback for unconnected DataOut fields
			for (var i = 0; i < fields.Length; i++)
			{
				var field = fields[i];
				if (connectedFields.Contains(field.Name)) continue;

				var outputAttr = ReflectionCache.GetAttribute<DataOutAttribute>(field);

				var key = !string.IsNullOrEmpty(outputAttr.DisplayName)
					? outputAttr.DisplayName
					: field.Name;

				var value = field.GetValue(state);
				_blackboard.SetBoxed(key, value, field.FieldType);
			}
		}



		/// <summary>
		/// Creates a new state-level CancellationTokenSource linked to the instance CTS.
		/// </summary>
		private void CreateStateCts()
		{
			_stateCts?.Dispose();
			_stateCts = _instanceCts != null
				? CancellationTokenSource.CreateLinkedTokenSource(_instanceCts.Token)
				: new CancellationTokenSource();
		}

		/// <summary>
		/// Cancels and disposes the state-level CancellationTokenSource.
		/// </summary>
		private void CancelStateCts()
		{
			if (_stateCts != null)
			{
				_stateCts.Cancel();
				_stateCts.Dispose();
				_stateCts = null;
			}
		}

		/// <summary>
		/// Cancels and disposes the instance-level CancellationTokenSource.
		/// </summary>
		private void CancelInstanceCts()
		{
			if (_instanceCts != null)
			{
				_instanceCts.Cancel();
				_instanceCts.Dispose();
				_instanceCts = null;
			}
		}

		/// <summary>
		/// Gets a state instance by node ID.
		/// </summary>
		public State GetState(string nodeId)
		{
			_stateInstances.TryGetValue(nodeId, out var state);
			return state;
		}

		/// <summary>
		/// Gets a typed state instance by node ID.
		/// </summary>
		public T GetState<T>(string nodeId) where T : State
		{
			return GetState(nodeId) as T;
		}

		/// <summary>
		/// Gets all state instances.
		/// </summary>
		public IEnumerable<State> GetAllStates()
		{
			return _stateInstances.Values;
		}

		/// <summary>
		/// Prepares this instance for return to a pool.
		/// Stops execution, resets all mutable state, and clears event subscribers.
		/// After calling this, the instance must be reinitialized via Reinitialize() before reuse.
		/// </summary>
		public void PrepareForReuse()
		{
			// Stop if running
			if (_isRunning)
			{
				Stop();
			}

			// Reset all state objects
			foreach (var state in _stateInstances.Values)
			{
				state.OnReset();
			}

			// Clear all event subscribers
			OnStateChanged = null;
			OnStarted = null;
			OnStopped = null;
			OnExited = null;
			OnTransitionTriggered = null;
			OnTransitionTriggeredDetailed = null;
			OnChildInstanceStarted = null;
			OnChildInstanceStopped = null;

			// Reset blackboard
			_blackboard.ClearSubscribers();
			_blackboard.ResetToDefaults(_graph.BlackboardDefinitions);

			// Clean up cancellation token sources
			CancelStateCts();
			CancelInstanceCts();
			_externalToken = CancellationToken.None;

			// Reset execution state
			_currentState = null;
			_isRunning = false;
			_isPaused = false;
			_sameFrameTransitionCount = 0;
			_lastTransitionFrame = 0;
			_activeChildInstance = null;
			_activeSubMachineNodeId = null;
			_lastTransitionPortName = null;

			// Clear external references
			_context = null;
			_parent = null;
			_sharedContext = null;
			_sharedContextType = null;
		}

		/// <summary>
		/// Reinitializes a pooled instance for reuse with a new context.
		/// Must be called after PrepareForReuse() before the instance can be used again.
		/// </summary>
		/// <param name="context">The new context for this instance.</param>
		/// <param name="sharedContext">Optional typed shared context for State&lt;TContext&gt; states.</param>
		/// <param name="parent">Optional parent instance for sub-state machines.</param>
		public void Reinitialize(
			IStateMachineContext context,
			object sharedContext = null,
			StateMachineInstance parent = null)
		{
			_context = context;
			_sharedContext = sharedContext;
			_sharedContextType = sharedContext?.GetType();
			_parent = parent;
			_instanceId = Guid.NewGuid().ToString();
			_nestingDepth = parent != null ? parent._nestingDepth + 1 : 0;

			// Reset blackboard parent hierarchy
			if (parent != null)
			{
				_blackboard.SetParent(parent.Blackboard);
			}
			else
			{
				_blackboard.SetParent(null);
			}

			// Reset blackboard to defaults
			_blackboard.ResetToDefaults(_graph.BlackboardDefinitions);

			// Reassign context to existing state instances
			foreach (var state in _stateInstances.Values)
			{
				state.StateMachine = this;
				state.Context = _context;
				InjectSharedContext(state);
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
				Debug.LogError($"[StateMachineInstance] Unhandled exception in {context}: {ex}");
			}
		}
	}
}

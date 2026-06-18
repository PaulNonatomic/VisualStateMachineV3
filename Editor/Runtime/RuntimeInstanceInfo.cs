using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Metadata about a running state machine instance.
	/// </summary>
	public class RuntimeInstanceInfo
	{
		public string InstanceId { get; }
		public StateMachineInstance Instance { get; }
		public StateMachineGraph Graph { get; }
		public GameObject Owner { get; }
		public DateTime StartTime { get; }

		private string _currentStateNodeId;
		private string _currentStateName;
		private string _lastVisitedNodeId;
		private string _pendingTriggerPortName;
		private string _pendingTriggerFromNodeId;

		/// <summary>
		/// The current node ID - returns SubStateMachine node ID if running a child instance.
		/// Falls back to last visited node if state machine is stuck.
		/// </summary>
		public string CurrentStateNodeId
		{
			get
			{
				// If running a SubStateMachine, report that node as current
				if (Instance?.IsInSubStateMachine == true && !string.IsNullOrEmpty(Instance.ActiveSubMachineNodeId))
				{
					return Instance.ActiveSubMachineNodeId;
				}
				// If we have a current state, return it
				if (!string.IsNullOrEmpty(_currentStateNodeId))
				{
					return _currentStateNodeId;
				}
				// Fall back to last visited node (useful when stuck after SubStateMachine with no exit)
				return _lastVisitedNodeId;
			}
			private set => _currentStateNodeId = value;
		}

		/// <summary>
		/// The current state name - returns SubStateMachine display name if running a child instance.
		/// </summary>
		public string CurrentStateName
		{
			get
			{
				// If running a SubStateMachine, report its name
				if (Instance?.IsInSubStateMachine == true && !string.IsNullOrEmpty(Instance.ActiveSubMachineNodeId))
				{
					var node = Graph?.GetNode(Instance.ActiveSubMachineNodeId);
					if (node is SubStateMachineNode subNode)
					{
						return subNode.DisplayName ?? "Sub State Machine";
					}
					return "Sub State Machine";
				}
				return _currentStateName;
			}
			private set => _currentStateName = value;
		}

		public RuntimeInstanceState Status { get; private set; }

		public IReadOnlyList<StateTransitionRecord> TransitionHistory => _transitionHistory;
		private readonly List<StateTransitionRecord> _transitionHistory = new List<StateTransitionRecord>();

		/// <summary>
		/// Trigger port name of the transition currently being processed. Set immediately
		/// before <see cref="OnNodeVisited"/> fires and cleared once the transition record is
		/// committed (on the subsequent OnStateChanged). Use this — not the last
		/// TransitionHistory entry — to inspect the in-flight transition at the moment a node
		/// is visited; the history record for the current transition is not added until later.
		/// </summary>
		public string PendingTriggerPortName => _pendingTriggerPortName;

		/// <summary>
		/// Source node of the in-flight transition (the node being exited), tracked alongside
		/// <see cref="PendingTriggerPortName"/> so edge-specific transition breakpoints can match
		/// the exact transition rather than any same-named port into the node.
		/// </summary>
		public string PendingTriggerFromNodeId => _pendingTriggerFromNodeId;

		private const int MaxHistoryEntries = 100;

		public event Action<RuntimeInstanceInfo> OnStateChanged;
		public event Action<RuntimeInstanceInfo> OnStatusChanged;

		/// <summary>
		/// Fired when any node is visited during a transition (including jump nodes).
		/// Args: (nodeId, isTransient) - isTransient is true for pass-through nodes like jumps.
		/// </summary>
		public event Action<RuntimeInstanceInfo, string, bool> OnNodeVisited;

		public RuntimeInstanceInfo(
			StateMachineInstance instance,
			StateMachineGraph graph,
			GameObject owner)
		{
			InstanceId = Guid.NewGuid().ToString();
			Instance = instance;
			Graph = graph;
			Owner = owner;
			StartTime = DateTime.Now;
			Status = RuntimeInstanceState.Running;

			// Subscribe to state changes and transitions
			if (instance != null)
			{
				instance.OnStateChanged += HandleStateChanged;
				instance.OnTransitionTriggered += HandleTransitionTriggered;
				instance.OnTransitionTriggeredDetailed += HandleTransitionTriggeredDetailed;
				instance.OnChildInstanceStarted += HandleChildInstanceStarted;
				instance.OnChildInstanceStopped += HandleChildInstanceStopped;
				instance.OnStopped += HandleStopped;
			}
		}

		/// <summary>
		/// Fires events to catch up with the current state of the instance.
		/// Call this AFTER subscribing to events, since the instance may have already
		/// transitioned before we registered.
		/// </summary>
		public void FireInitialStateEvents()
		{
			if (Instance == null) return;

			// Check if already in a state (state machine started before we registered)
			if (Instance.CurrentState != null)
			{
				HandleStateChanged(null, Instance.CurrentState);
			}
			// Check if currently running a sub-state machine
			else if (Instance.IsInSubStateMachine && !string.IsNullOrEmpty(Instance.ActiveSubMachineNodeId))
			{
				// Fire node visited for the SubStateMachine node
				OnNodeVisited?.Invoke(this, Instance.ActiveSubMachineNodeId, false);

				// Also register the child instance that's already running
				var childInstance = Instance.ActiveChildInstance;
				if (childInstance != null)
				{
					var childGraph = childInstance.Graph;
					if (childGraph != null)
					{
						RuntimeInstanceTracker.Register(childInstance, childGraph, Owner);
					}
				}
			}
		}

		private void HandleStateChanged(State previousState, State newState)
		{
			var previousNodeId = _currentStateNodeId;
			var previousStateName = _currentStateName;

			// Get the current state node ID from the instance (uses internal access within Runtime assembly)
			var instanceNodeId = Instance?.CurrentStateNodeId;

			// Use the instance's current state node ID (most reliable)
			if (!string.IsNullOrEmpty(instanceNodeId))
			{
				_currentStateNodeId = instanceNodeId;
				_currentStateName = newState?.GetType().Name ?? "Unknown";

				// Try to get display name from graph
				if (Graph != null)
				{
					var node = Graph.GetNode(instanceNodeId);
					if (node is StateNode stateNode)
					{
						_currentStateName = stateNode.DisplayName ?? stateNode.StateType?.Name ?? _currentStateName;
					}
				}
			}
			// Fallback: Find node by type matching
			else if (Graph != null && newState != null)
			{
				foreach (var node in Graph.GetAllNodes())
				{
					if (node is StateNode stateNode)
					{
						stateNode.RestoreType();
						if (stateNode.StateType == newState.GetType())
						{
							_currentStateNodeId = stateNode.Id;
							_currentStateName = stateNode.DisplayName ?? stateNode.StateType?.Name ?? "Unknown";
							break;
						}
					}
				}
			}

			// Record transition with blackboard snapshot
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromNodeId = previousNodeId,
				FromStateName = previousStateName,
				ToNodeId = _currentStateNodeId,
				ToStateName = _currentStateName,
				TriggerPortName = _pendingTriggerPortName,
				BlackboardSnapshot = CaptureBlackboardSnapshot(),
				BlackboardTypes = CaptureBlackboardTypes()
			};

			_pendingTriggerPortName = null;
			_pendingTriggerFromNodeId = null;
			_transitionHistory.Add(record);

			// Trim history if needed
			while (_transitionHistory.Count > MaxHistoryEntries)
			{
				_transitionHistory.RemoveAt(0);
			}

			OnStateChanged?.Invoke(this);
		}

		private void HandleTransitionTriggeredDetailed(string fromNodeId, string toNodeId, string portName)
		{
			if (!string.IsNullOrEmpty(portName))
			{
				_pendingTriggerPortName = portName;
				_pendingTriggerFromNodeId = fromNodeId;
			}
		}

		private void HandleTransitionTriggered(string fromNodeId, string toNodeId)
		{
			if (string.IsNullOrEmpty(toNodeId)) return;

			// Track the last visited node for fallback when state machine is stuck
			_lastVisitedNodeId = toNodeId;

			// Capture the trigger port name from the graph connection
			if (Graph != null && !string.IsNullOrEmpty(fromNodeId))
			{
				var connection = Graph.GetConnectionsFromNode(fromNodeId)
					.FirstOrDefault(c => c.TargetNodeId == toNodeId &&
						c.ConnectionType == ConnectionType.Transition);

				if (connection != null)
				{
					_pendingTriggerPortName = connection.SourcePortName;
					_pendingTriggerFromNodeId = fromNodeId;
				}
			}

			// Check if the target is a transient node (portal nodes flash briefly)
			// Exit nodes are NOT transient - they stay highlighted to show where the state machine ended
			var node = Graph?.GetNode(toNodeId);
			var isTransient = node != null &&
				(node.NodeType == GraphNodeType.PortalOut ||
				 node.NodeType == GraphNodeType.PortalIn);

			// If entering a SubStateMachine, clear the current state tracking
			// (the SubStateMachine will be reported via the dynamic property)
			if (node?.NodeType == GraphNodeType.SubStateMachine)
			{
				_currentStateNodeId = null;
				_currentStateName = null;
			}
			// If entering an Exit node, track it as the current "state" so it persists
			else if (node?.NodeType == GraphNodeType.Exit)
			{
				_currentStateNodeId = toNodeId;
				_currentStateName = "Exit";
				if (node is ExitNode exitNode && !string.IsNullOrEmpty(exitNode.ExitName))
				{
					_currentStateName = exitNode.ExitName;
				}
			}

			OnNodeVisited?.Invoke(this, toNodeId, isTransient);
		}

		private void HandleChildInstanceStarted(StateMachineInstance childInstance, StateMachineGraph childGraph)
		{
			if (childInstance == null || childGraph == null) return;

			if (Nonatomic.VSM.Editor.Graph.StateMachineGraphView.DebugMode)
			{
				UnityEngine.Debug.Log($"[RuntimeInstanceInfo] HandleChildInstanceStarted: childGraph={childGraph.name}, Owner={Owner?.name}");
			}

			// Register the child instance with the tracker
			var childInfo = RuntimeInstanceTracker.Register(childInstance, childGraph, Owner);

			// Notify that a child instance started
			if (childInfo != null)
			{
				RuntimeInstanceTracker.NotifyChildInstanceStarted(this, childInfo, childGraph);
			}
		}

		private void HandleChildInstanceStopped(StateMachineInstance childInstance)
		{
			if (childInstance == null) return;

			// Don't unregister the child instance - just mark it as stopped
			// This allows the Exit node to remain highlighted when viewing the child graph
			var childInfo = RuntimeInstanceTracker.GetInstanceForStateMachine(childInstance);
			if (childInfo != null)
			{
				childInfo.SetStatus(RuntimeInstanceState.Stopped);

				// Notify that a child instance stopped
				RuntimeInstanceTracker.NotifyChildInstanceStopped(this, childInfo);
			}

			// Fire state changed event since CurrentStateNodeId may have changed
			// (SubStateMachine node -> next state or null)
			OnStateChanged?.Invoke(this);
		}

		private void HandleStopped()
		{
			// Mark this instance as stopped - don't unregister
			// This allows the Exit node to remain highlighted
			SetStatus(RuntimeInstanceState.Stopped);
		}

		public void SetStatus(RuntimeInstanceState status)
		{
			if (Status != status)
			{
				Status = status;
				OnStatusChanged?.Invoke(this);
			}
		}

		public Dictionary<string, object> GetBlackboardSnapshot()
		{
			var snapshot = new Dictionary<string, object>();

			if (Instance?.Blackboard != null)
			{
				foreach (var key in Instance.Blackboard.GetAllKeys())
				{
					snapshot[key] = Instance.Blackboard.Get<object>(key);
				}
			}

			return snapshot;
		}

		private Dictionary<string, string> CaptureBlackboardSnapshot()
		{
			var snapshot = new Dictionary<string, string>();
			if (Instance?.Blackboard == null) return snapshot;

			foreach (var key in Instance.Blackboard.GetAllKeys())
			{
				var variable = Instance.Blackboard.GetVariable(key);
				if (variable == null) continue;

				try
				{
					var serialized = BlackboardTypeRegistry.Serialize(variable.BoxedValue, variable.ValueType);
					snapshot[key] = serialized ?? "null";
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"[VSM] Failed to serialize blackboard variable '{key}': {ex.Message}");
					snapshot[key] = variable.BoxedValue?.ToString() ?? "null";
				}
			}

			return snapshot;
		}

		private Dictionary<string, string> CaptureBlackboardTypes()
		{
			var types = new Dictionary<string, string>();
			if (Instance?.Blackboard == null) return types;

			foreach (var key in Instance.Blackboard.GetAllKeys())
			{
				var variable = Instance.Blackboard.GetVariable(key);
				if (variable?.ValueType != null)
				{
					types[key] = variable.ValueType.Name;
				}
			}

			return types;
		}

		public void Dispose()
		{
			if (Instance != null)
			{
				Instance.OnStateChanged -= HandleStateChanged;
				Instance.OnTransitionTriggered -= HandleTransitionTriggered;
				Instance.OnTransitionTriggeredDetailed -= HandleTransitionTriggeredDetailed;
				Instance.OnChildInstanceStarted -= HandleChildInstanceStarted;
				Instance.OnChildInstanceStopped -= HandleChildInstanceStopped;
				Instance.OnStopped -= HandleStopped;
			}
		}
	}

	/// <summary>
	/// Represents the current state of a runtime instance.
	/// </summary>
	public enum RuntimeInstanceState
	{
		Running,
		Paused,
		Stopped,
		Error
	}

	/// <summary>
	/// Record of a state transition for history tracking.
	/// </summary>
	public class StateTransitionRecord
	{
		public DateTime Timestamp { get; set; }
		public string FromNodeId { get; set; }
		public string FromStateName { get; set; }
		public string ToNodeId { get; set; }
		public string ToStateName { get; set; }

		/// <summary>
		/// Serialized blackboard variable values at the moment of this transition.
		/// </summary>
		public Dictionary<string, string> BlackboardSnapshot { get; set; }

		/// <summary>
		/// Type names for each blackboard variable, for display purposes.
		/// </summary>
		public Dictionary<string, string> BlackboardTypes { get; set; }

		/// <summary>
		/// The transition port name that triggered this transition.
		/// </summary>
		public string TriggerPortName { get; set; }

		public TimeSpan GetDuration(StateTransitionRecord previousRecord)
		{
			if (previousRecord == null)
			{
				return TimeSpan.Zero;
			}

			return Timestamp - previousRecord.Timestamp;
		}
	}
}

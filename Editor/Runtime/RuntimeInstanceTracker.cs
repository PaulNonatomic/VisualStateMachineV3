using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Static tracker for all running state machine instances.
	/// Provides centralized access to runtime state machine monitoring.
	/// </summary>
	[InitializeOnLoad]
	public static class RuntimeInstanceTracker
	{
		private static readonly Dictionary<string, RuntimeInstanceInfo> _instances =
			new Dictionary<string, RuntimeInstanceInfo>();

		// Use asset path as key instead of object reference to handle runtime/editor object differences
		private static readonly Dictionary<string, List<string>> _instancesByGraphPath =
			new Dictionary<string, List<string>>();

		public static event Action<RuntimeInstanceInfo> OnInstanceRegistered;
		public static event Action<RuntimeInstanceInfo> OnInstanceUnregistered;
		public static event Action<RuntimeInstanceInfo> OnInstanceStateChanged;
		public static event Action<RuntimeInstanceInfo, string, bool> OnNodeVisited;
		public static event Action OnInstancesCleared;

		/// <summary>
		/// Fired when a child (sub-state machine) instance starts.
		/// Args: (parentInfo, childInfo, childGraph)
		/// </summary>
		public static event Action<RuntimeInstanceInfo, RuntimeInstanceInfo, StateMachineGraph> OnChildInstanceStarted;

		/// <summary>
		/// Fired when a child (sub-state machine) instance stops.
		/// Args: (parentInfo, childInfo)
		/// </summary>
		public static event Action<RuntimeInstanceInfo, RuntimeInstanceInfo> OnChildInstanceStopped;

		public static IReadOnlyCollection<RuntimeInstanceInfo> AllInstances => _instances.Values;
		public static int InstanceCount => _instances.Count;

		/// <summary>
		/// Gets the asset path for a graph, handling both runtime and editor references.
		/// </summary>
		private static string GetGraphAssetPath(StateMachineGraph graph)
		{
			if (graph == null) return null;
			var path = AssetDatabase.GetAssetPath(graph);
			// If path is empty, try to find by name (handles runtime-instantiated graphs)
			if (string.IsNullOrEmpty(path))
			{
				// Use graph name as fallback key
				return $"runtime:{graph.name}";
			}
			return path;
		}

		static RuntimeInstanceTracker()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				ClearAll();
			}
		}

		/// <summary>
		/// Register a new state machine instance for tracking.
		/// </summary>
		public static RuntimeInstanceInfo Register(
			StateMachineInstance instance,
			StateMachineGraph graph,
			GameObject owner)
		{
			if (instance == null)
			{
				Debug.LogWarning("Cannot register null state machine instance.");
				return null;
			}

			// Check if this StateMachineInstance is already registered (prevent duplicate registrations)
			var existing = GetInstanceForStateMachine(instance);
			if (existing != null)
			{
				if (Graph.StateMachineGraphView.DebugMode)
				{
					Debug.Log($"[RuntimeTracker] Register: graph={graph?.name} already registered, returning existing");
				}
				return existing;
			}

			var graphPath = GetGraphAssetPath(graph);

			if (Graph.StateMachineGraphView.DebugMode)
			{
				Debug.Log($"[RuntimeTracker] Register: graph={graph?.name}, path={graphPath}, owner={owner?.name}");
			}

			var info = new RuntimeInstanceInfo(instance, graph, owner);
			info.OnStateChanged += HandleInstanceStateChanged;
			info.OnNodeVisited += HandleNodeVisited;

			_instances[info.InstanceId] = info;

			// Track by graph path (asset path ensures consistent lookups between runtime/editor references)
			if (!string.IsNullOrEmpty(graphPath))
			{
				if (!_instancesByGraphPath.ContainsKey(graphPath))
				{
					_instancesByGraphPath[graphPath] = new List<string>();
				}

				_instancesByGraphPath[graphPath].Add(info.InstanceId);

				if (Graph.StateMachineGraphView.DebugMode)
				{
					Debug.Log($"[RuntimeTracker] Registered under path key. Total graphs tracked: {_instancesByGraphPath.Count}");
					foreach (var kvp in _instancesByGraphPath)
					{
						Debug.Log($"[RuntimeTracker]   Path: {kvp.Key}, instances={kvp.Value.Count}");
					}
				}
			}

			OnInstanceRegistered?.Invoke(info);

			// Fire initial state events AFTER registration and event subscription
			// This catches up with state machines that started before we registered
			info.FireInitialStateEvents();

			return info;
		}

		/// <summary>
		/// Unregister a state machine instance.
		/// </summary>
		public static void Unregister(string instanceId)
		{
			if (!_instances.TryGetValue(instanceId, out var info))
			{
				return;
			}

			info.OnStateChanged -= HandleInstanceStateChanged;
			info.OnNodeVisited -= HandleNodeVisited;
			info.Dispose();

			_instances.Remove(instanceId);

			// Remove from graph path tracking
			var graphPath = GetGraphAssetPath(info.Graph);
			if (!string.IsNullOrEmpty(graphPath) && _instancesByGraphPath.ContainsKey(graphPath))
			{
				_instancesByGraphPath[graphPath].Remove(instanceId);
				if (_instancesByGraphPath[graphPath].Count == 0)
				{
					_instancesByGraphPath.Remove(graphPath);
				}
			}

			OnInstanceUnregistered?.Invoke(info);
		}

		/// <summary>
		/// Unregister a state machine instance by reference.
		/// </summary>
		public static void Unregister(StateMachineInstance instance)
		{
			var info = _instances.Values.FirstOrDefault(i => i.Instance == instance);
			if (info != null)
			{
				Unregister(info.InstanceId);
			}
		}

		/// <summary>
		/// Get all instances running a specific graph.
		/// </summary>
		public static IEnumerable<RuntimeInstanceInfo> GetInstancesForGraph(StateMachineGraph graph)
		{
			var graphPath = GetGraphAssetPath(graph);
			if (string.IsNullOrEmpty(graphPath) || !_instancesByGraphPath.ContainsKey(graphPath))
			{
				yield break;
			}

			foreach (var instanceId in _instancesByGraphPath[graphPath])
			{
				if (_instances.TryGetValue(instanceId, out var info))
				{
					yield return info;
				}
			}
		}

		/// <summary>
		/// Get an instance by ID.
		/// </summary>
		public static RuntimeInstanceInfo GetInstance(string instanceId)
		{
			_instances.TryGetValue(instanceId, out var info);
			return info;
		}

		/// <summary>
		/// Get instances owned by a specific GameObject.
		/// </summary>
		public static IEnumerable<RuntimeInstanceInfo> GetInstancesForOwner(GameObject owner)
		{
			return _instances.Values.Where(i => i.Owner == owner);
		}

		/// <summary>
		/// Get the RuntimeInstanceInfo for a specific StateMachineInstance.
		/// </summary>
		public static RuntimeInstanceInfo GetInstanceForStateMachine(StateMachineInstance instance)
		{
			return _instances.Values.FirstOrDefault(i => i.Instance == instance);
		}

		/// <summary>
		/// Get the currently active instance for a graph (most recently changed state).
		/// Falls back to stopped instances if no running instances are found.
		/// </summary>
		public static RuntimeInstanceInfo GetActiveInstanceForGraph(StateMachineGraph graph)
		{
			var graphPath = GetGraphAssetPath(graph);
			var instances = GetInstancesForGraph(graph).ToList();

			if (Graph.StateMachineGraphView.DebugMode)
			{
				UnityEngine.Debug.Log($"[RuntimeTracker] GetActiveInstanceForGraph: graph={graph?.name}, path={graphPath}, found {instances.Count} instances");
				UnityEngine.Debug.Log($"[RuntimeTracker] All tracked graphs ({_instancesByGraphPath.Count}):");
				foreach (var kvp in _instancesByGraphPath)
				{
					UnityEngine.Debug.Log($"[RuntimeTracker]   Path: {kvp.Key}, instances={kvp.Value.Count}");
				}
				foreach (var inst in instances)
				{
					UnityEngine.Debug.Log($"[RuntimeTracker]   - Instance: Status={inst.Status}, CurrentNodeId={inst.CurrentStateNodeId}");
				}
			}

			// First try to find a running instance
			var running = instances
				.Where(i => i.Status == RuntimeInstanceState.Running)
				.OrderByDescending(i => i.TransitionHistory.LastOrDefault()?.Timestamp ?? i.StartTime)
				.FirstOrDefault();

			if (running != null) return running;

			// Fall back to the most recently stopped instance (to show Exit node)
			return instances
				.Where(i => i.Status == RuntimeInstanceState.Stopped)
				.OrderByDescending(i => i.TransitionHistory.LastOrDefault()?.Timestamp ?? i.StartTime)
				.FirstOrDefault();
		}

		/// <summary>
		/// Clear all tracked instances.
		/// </summary>
		public static void ClearAll()
		{
			foreach (var info in _instances.Values)
			{
				info.OnStateChanged -= HandleInstanceStateChanged;
				info.OnNodeVisited -= HandleNodeVisited;
				info.Dispose();
			}

			_instances.Clear();
			_instancesByGraphPath.Clear();

			OnInstancesCleared?.Invoke();
		}

		private static void HandleInstanceStateChanged(RuntimeInstanceInfo info)
		{
			OnInstanceStateChanged?.Invoke(info);
		}

		private static void HandleNodeVisited(RuntimeInstanceInfo info, string nodeId, bool isTransient)
		{
			OnNodeVisited?.Invoke(info, nodeId, isTransient);
		}

		/// <summary>
		/// Notify that a child instance has started (called from RuntimeInstanceInfo).
		/// </summary>
		internal static void NotifyChildInstanceStarted(RuntimeInstanceInfo parentInfo, RuntimeInstanceInfo childInfo, StateMachineGraph childGraph)
		{
			OnChildInstanceStarted?.Invoke(parentInfo, childInfo, childGraph);
		}

		/// <summary>
		/// Notify that a child instance has stopped (called from RuntimeInstanceInfo).
		/// </summary>
		internal static void NotifyChildInstanceStopped(RuntimeInstanceInfo parentInfo, RuntimeInstanceInfo childInfo)
		{
			OnChildInstanceStopped?.Invoke(parentInfo, childInfo);
		}

		/// <summary>
		/// Check if there are any running instances.
		/// </summary>
		public static bool HasRunningInstances()
		{
			return _instances.Values.Any(i => i.Status == RuntimeInstanceState.Running);
		}

		/// <summary>
		/// Get summary statistics for monitoring UI.
		/// </summary>
		public static RuntimeTrackerStats GetStats()
		{
			var running = 0;
			var paused = 0;
			var stopped = 0;
			var error = 0;

			foreach (var info in _instances.Values)
			{
				switch (info.Status)
				{
					case RuntimeInstanceState.Running:
						running++;
						break;
					case RuntimeInstanceState.Paused:
						paused++;
						break;
					case RuntimeInstanceState.Stopped:
						stopped++;
						break;
					case RuntimeInstanceState.Error:
						error++;
						break;
				}
			}

			return new RuntimeTrackerStats
			{
				TotalInstances = _instances.Count,
				RunningInstances = running,
				PausedInstances = paused,
				StoppedInstances = stopped,
				ErrorInstances = error,
				UniqueGraphs = _instancesByGraphPath.Count
			};
		}
	}

	/// <summary>
	/// Summary statistics for the runtime tracker.
	/// </summary>
	public struct RuntimeTrackerStats
	{
		public int TotalInstances;
		public int RunningInstances;
		public int PausedInstances;
		public int StoppedInstances;
		public int ErrorInstances;
		public int UniqueGraphs;
	}
}

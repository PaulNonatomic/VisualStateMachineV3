using System.Collections.Generic;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Automatically finds and tracks StateMachineRunner instances during play mode.
	/// </summary>
	[InitializeOnLoad]
	public static class RuntimeInstanceAutoTracker
	{
		private static readonly HashSet<StateMachineRunner> _trackedRunners = new HashSet<StateMachineRunner>();
		private static bool _isTracking;

		static RuntimeInstanceAutoTracker()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			// Subscribe to StateMachineController callbacks
			StateMachineControllerCallbacks.OnControllerCreated += OnControllerCreated;
			StateMachineControllerCallbacks.OnControllerDisposed += OnControllerDisposed;
		}

		private static void OnControllerCreated(StateMachineInstance instance, StateMachineGraph graph, GameObject owner)
		{
			if (instance != null && graph != null)
			{
				RuntimeInstanceTracker.Register(instance, graph, owner);
			}
		}

		private static void OnControllerDisposed(StateMachineInstance instance)
		{
			if (instance != null)
			{
				RuntimeInstanceTracker.Unregister(instance);
			}
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.EnteredPlayMode:
					StartTracking();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					StopTracking();
					break;
			}
		}

		private static void StartTracking()
		{
			_isTracking = true;
			_trackedRunners.Clear();
			ScanForExistingRunners();
		}

		private static void StopTracking()
		{
			_isTracking = false;
			_trackedRunners.Clear();
		}

		private static void ScanForExistingRunners()
		{
			var runners = Object.FindObjectsByType<StateMachineRunner>(FindObjectsSortMode.None);

			foreach (var runner in runners)
			{
				if (runner == null || _trackedRunners.Contains(runner)) continue;

				if (runner.Instance != null && runner.Graph != null)
				{
					_trackedRunners.Add(runner);
					RuntimeInstanceTracker.Register(runner.Instance, runner.Graph, runner.gameObject);
				}
			}
		}
	}
}

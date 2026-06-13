using System;
using System.Collections.Generic;
using System.Globalization;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Debugging
{
	[InitializeOnLoad]
	public static class BreakpointManager
	{
		private static bool _globalEnabled = true;

		public static bool GlobalEnabled
		{
			get => _globalEnabled;
			set
			{
				if (_globalEnabled == value) return;
				_globalEnabled = value;
				EditorPrefs.SetBool("VSM.Breakpoints.GlobalEnabled", value);
				OnGlobalEnabledChanged?.Invoke(value);
			}
		}

		public static event Action<bool> OnGlobalEnabledChanged;
		public static event Action<string, string> OnBreakpointHit;
		public static event Action<StateMachineGraph, string> OnBreakpointToggled;

		static BreakpointManager()
		{
			_globalEnabled = EditorPrefs.GetBool("VSM.Breakpoints.GlobalEnabled", true);
			RuntimeInstanceTracker.OnNodeVisited += HandleNodeVisited;
			RuntimeInstanceTracker.OnInstanceStateChanged += HandleInstanceStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				ResetAllHitCounts();
			}
		}

		private static void HandleNodeVisited(RuntimeInstanceInfo info, string nodeId, bool isTransient)
		{
			if (!_globalEnabled || isTransient) return;
			if (info?.Graph == null) return;

			var breakpoint = info.Graph.GraphViewData.GetBreakpointForNode(nodeId);
			if (breakpoint == null || !breakpoint.Enabled) return;

			// Conditional breakpoints are evaluated by HandleInstanceStateChanged
			if (breakpoint.Type == BreakpointType.Conditional) return;

			if (ShouldBreak(breakpoint, info))
			{
				breakpoint.HitCount++;
				var nodeName = GetNodeName(info.Graph, nodeId);
				OnBreakpointHit?.Invoke(nodeId, nodeName);
				Debug.Log($"[VSM Breakpoint] Hit breakpoint on '{nodeName}' (node {nodeId})");
				EditorApplication.isPaused = true;
			}
		}

		private static void HandleInstanceStateChanged(RuntimeInstanceInfo info)
		{
			if (!_globalEnabled) return;
			if (info?.Graph == null) return;

			var nodeId = info.CurrentStateNodeId;
			if (string.IsNullOrEmpty(nodeId)) return;

			var breakpoint = info.Graph.GraphViewData.GetBreakpointForNode(nodeId);
			if (breakpoint == null || !breakpoint.Enabled) return;
			if (breakpoint.Type != BreakpointType.Conditional) return;

			if (EvaluateCondition(breakpoint.Condition, info))
			{
				breakpoint.HitCount++;
				var nodeName = GetNodeName(info.Graph, nodeId);
				OnBreakpointHit?.Invoke(nodeId, nodeName);
				Debug.Log($"[VSM Breakpoint] Conditional breakpoint hit on '{nodeName}': {breakpoint.Condition}");
				EditorApplication.isPaused = true;
			}
		}

		private static bool ShouldBreak(Breakpoint breakpoint, RuntimeInstanceInfo info)
		{
			switch (breakpoint.Type)
			{
				case BreakpointType.OnEntry:
					return true;

				case BreakpointType.OnTransition:
					// Empty port name means "break on any transition into this node".
					if (string.IsNullOrEmpty(breakpoint.TransitionPortName)) return true;
					// Use the in-flight trigger port, set just before OnNodeVisited fires. The
					// TransitionHistory record for the current transition isn't committed until
					// the later OnStateChanged, so reading history here would lag by one and the
					// breakpoint would never match the transition that just occurred.
					return info.PendingTriggerPortName == breakpoint.TransitionPortName;

				case BreakpointType.Conditional:
					return EvaluateCondition(breakpoint.Condition, info);

				default:
					return false;
			}
		}

		internal static bool EvaluateCondition(string condition, RuntimeInstanceInfo info)
		{
			if (string.IsNullOrWhiteSpace(condition)) return true;
			if (info?.Instance?.Blackboard == null) return false;

			var trimmed = condition.Trim();

			// Handle negation: !variableName
			if (trimmed.StartsWith("!"))
			{
				var varName = trimmed.Substring(1).Trim();
				return !GetBlackboardBoolValue(varName, info);
			}

			// Try to parse as comparison: variableName operator value
			var operators = new[] { "!=", ">=", "<=", "==", ">", "<" };
			foreach (var op in operators)
			{
				var opIndex = trimmed.IndexOf(op, StringComparison.Ordinal);
				if (opIndex <= 0) continue;

				var varName = trimmed.Substring(0, opIndex).Trim();
				var valueStr = trimmed.Substring(opIndex + op.Length).Trim();

				return EvaluateComparison(varName, op, valueStr, info);
			}

			// Simple truthy check: variableName
			return GetBlackboardBoolValue(trimmed, info);
		}

		private static bool EvaluateComparison(string variableName, string op, string valueStr, RuntimeInstanceInfo info)
		{
			var variable = info.Instance.Blackboard.GetVariable(variableName);
			if (variable == null) return false;

			var boxedValue = variable.BoxedValue;
			if (boxedValue == null) return op == "==" && (valueStr == "null" || valueStr == "");

			// Numeric comparison
			if (boxedValue is int intVal && float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var targetFloat))
			{
				return CompareNumeric(intVal, op, targetFloat);
			}

			if (boxedValue is float floatVal && float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var targetFloat2))
			{
				return CompareNumeric(floatVal, op, targetFloat2);
			}

			if (boxedValue is double doubleVal && double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var targetDouble))
			{
				return CompareNumeric((float)doubleVal, op, (float)targetDouble);
			}

			// Boolean comparison
			if (boxedValue is bool boolVal && bool.TryParse(valueStr, out var targetBool))
			{
				return op switch
				{
					"==" => boolVal == targetBool,
					"!=" => boolVal != targetBool,
					_ => false
				};
			}

			// String comparison
			var strVal = boxedValue.ToString();
			return op switch
			{
				"==" => strVal == valueStr,
				"!=" => strVal != valueStr,
				_ => false
			};
		}

		private static bool CompareNumeric(float actual, string op, float target)
		{
			return op switch
			{
				"==" => Mathf.Approximately(actual, target),
				"!=" => !Mathf.Approximately(actual, target),
				">" => actual > target,
				"<" => actual < target,
				">=" => actual >= target,
				"<=" => actual <= target,
				_ => false
			};
		}

		private static bool GetBlackboardBoolValue(string variableName, RuntimeInstanceInfo info)
		{
			var variable = info.Instance.Blackboard.GetVariable(variableName);
			if (variable == null) return false;

			var value = variable.BoxedValue;
			if (value is bool b) return b;
			if (value is int i) return i != 0;
			if (value is float f) return f != 0f;
			if (value is string s) return !string.IsNullOrEmpty(s);
			return value != null;
		}

		public static Breakpoint ToggleBreakpoint(StateMachineGraph graph, string nodeId)
		{
			if (graph == null || string.IsNullOrEmpty(nodeId)) return null;

			Undo.RecordObject(graph, "Toggle Breakpoint");
			var result = graph.GraphViewData.ToggleBreakpoint(nodeId);
			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, nodeId);
			return result;
		}

		/// <summary>
		/// Toggles an OnTransition breakpoint on the target node of a transition. It fires when
		/// that node is entered via the given source port. If the node already has an
		/// OnTransition breakpoint for the same port it is removed; otherwise any existing
		/// breakpoint on the node is replaced with this one (a node holds a single breakpoint).
		/// </summary>
		public static Breakpoint ToggleTransitionBreakpoint(StateMachineGraph graph, string targetNodeId, string transitionPortName)
		{
			if (graph == null || string.IsNullOrEmpty(targetNodeId)) return null;

			Undo.RecordObject(graph, "Toggle Transition Breakpoint");
			var data = graph.GraphViewData;
			var existing = data.GetBreakpointForNode(targetNodeId);

			Breakpoint result = null;
			if (existing != null
				&& existing.Type == BreakpointType.OnTransition
				&& existing.TransitionPortName == transitionPortName)
			{
				// Same transition breakpoint already present -> toggle it off.
				data.RemoveBreakpoint(existing.Id);
			}
			else
			{
				// Replace any existing breakpoint on this node with an OnTransition one.
				if (existing != null) data.RemoveBreakpoint(existing.Id);
				result = new Breakpoint(targetNodeId, BreakpointType.OnTransition)
				{
					TransitionPortName = transitionPortName
				};
				data.AddBreakpoint(result);
			}

			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, targetNodeId);
			return result;
		}

		public static void RemoveBreakpoint(StateMachineGraph graph, string breakpointId)
		{
			if (graph == null || string.IsNullOrEmpty(breakpointId)) return;

			Undo.RecordObject(graph, "Remove Breakpoint");
			graph.GraphViewData.RemoveBreakpoint(breakpointId);
			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, null);
		}

		public static void ClearAllBreakpoints(StateMachineGraph graph)
		{
			if (graph == null) return;

			Undo.RecordObject(graph, "Clear All Breakpoints");
			graph.GraphViewData.ClearAllBreakpoints();
			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, null);
		}

		public static void SetBreakpointCondition(StateMachineGraph graph, string nodeId, string condition)
		{
			if (graph == null || string.IsNullOrEmpty(nodeId)) return;

			var breakpoint = graph.GraphViewData.GetBreakpointForNode(nodeId);
			if (breakpoint == null) return;

			Undo.RecordObject(graph, "Set Breakpoint Condition");
			breakpoint.Condition = condition;
			breakpoint.Type = string.IsNullOrWhiteSpace(condition)
				? BreakpointType.OnEntry
				: BreakpointType.Conditional;
			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, nodeId);
		}

		public static void SetBreakpointEnabled(StateMachineGraph graph, string nodeId, bool enabled)
		{
			if (graph == null || string.IsNullOrEmpty(nodeId)) return;

			var breakpoint = graph.GraphViewData.GetBreakpointForNode(nodeId);
			if (breakpoint == null) return;

			Undo.RecordObject(graph, "Toggle Breakpoint Enabled");
			breakpoint.Enabled = enabled;
			EditorUtility.SetDirty(graph);
			OnBreakpointToggled?.Invoke(graph, nodeId);
		}

		private static void ResetAllHitCounts()
		{
			var processedGraphs = new HashSet<StateMachineGraph>();
			foreach (var instance in RuntimeInstanceTracker.AllInstances)
			{
				var graph = instance?.Graph;
				if (graph == null || !processedGraphs.Add(graph)) continue;

				foreach (var breakpoint in graph.GraphViewData.Breakpoints)
				{
					breakpoint.HitCount = 0;
				}
			}
		}

		private static string GetNodeName(StateMachineGraph graph, string nodeId)
		{
			var node = graph?.GetNode(nodeId);
			if (node is StateNode stateNode)
			{
				return stateNode.DisplayName ?? stateNode.StateType?.Name ?? nodeId;
			}
			return nodeId;
		}
	}
}

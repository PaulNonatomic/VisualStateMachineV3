using System.Collections.Generic;
using Nonatomic.VSM.Editor.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Extension methods for StateMachineGraphView to support runtime monitoring visualization.
	/// </summary>
	public static class RuntimeGraphViewExtensions
	{
		private static readonly Dictionary<StateMachineGraphView, RuntimeVisualizationState> _visualizationStates =
			new Dictionary<StateMachineGraphView, RuntimeVisualizationState>();

		/// <summary>
		/// Highlight a node as the currently active state.
		/// </summary>
		public static void HighlightActiveState(this StateMachineGraphView graphView, string nodeId)
		{
			if (graphView == null || string.IsNullOrEmpty(nodeId))
			{
				return;
			}

			var state = GetOrCreateState(graphView);

			// Clear previous highlight
			if (!string.IsNullOrEmpty(state.CurrentHighlightedNodeId))
			{
				var previousNode = graphView.GetNodeByGuid(state.CurrentHighlightedNodeId);
				if (previousNode != null)
				{
					previousNode.RemoveFromClassList("active-state");
					previousNode.RemoveFromClassList("active-state-highlight");
				}
			}

			// Apply new highlight
			var node = graphView.GetNodeByGuid(nodeId);
			if (node != null)
			{
				node.AddToClassList("active-state");
				node.AddToClassList("active-state-highlight");
				state.CurrentHighlightedNodeId = nodeId;
			}
		}

		/// <summary>
		/// Clear all runtime highlights from the graph.
		/// </summary>
		public static void ClearRuntimeHighlights(this StateMachineGraphView graphView)
		{
			if (graphView == null)
			{
				return;
			}

			var state = GetOrCreateState(graphView);

			if (!string.IsNullOrEmpty(state.CurrentHighlightedNodeId))
			{
				var node = graphView.GetNodeByGuid(state.CurrentHighlightedNodeId);
				if (node != null)
				{
					node.RemoveFromClassList("active-state");
					node.RemoveFromClassList("active-state-highlight");
				}
			}

			state.CurrentHighlightedNodeId = null;

			// Clear any transition highlights
			foreach (var edgeId in state.HighlightedEdges)
			{
				// Clear edge highlight
			}

			state.HighlightedEdges.Clear();
		}

		/// <summary>
		/// Center the view on a specific node with optional animation.
		/// </summary>
		public static void CenterOnState(this StateMachineGraphView graphView, string nodeId, bool animate = true)
		{
			if (graphView == null || string.IsNullOrEmpty(nodeId))
			{
				return;
			}

			var node = graphView.GetNodeByGuid(nodeId);
			if (node == null)
			{
				return;
			}

			var nodeRect = node.GetPosition();
			var nodeCenter = nodeRect.center;

			if (animate && graphView.UseLerpAnimation)
			{
				StartLerpAnimation(graphView, nodeCenter);
			}
			else
			{
				CenterViewOnPosition(graphView, nodeCenter);
			}
		}

		/// <summary>
		/// Highlight a transition edge as currently active.
		/// </summary>
		public static void HighlightTransition(this StateMachineGraphView graphView, string fromNodeId, string toNodeId)
		{
			if (graphView == null)
			{
				return;
			}

			var state = GetOrCreateState(graphView);

			// Find the edge connecting these nodes
			foreach (var edge in graphView.edges)
			{
				if (edge is TransitionEdgeView transitionEdge)
				{
					if (edge.output?.node?.viewDataKey == fromNodeId &&
						edge.input?.node?.viewDataKey == toNodeId)
					{
						transitionEdge.SetTransitioning(true, 0f);
						state.HighlightedEdges.Add($"{fromNodeId}->{toNodeId}");
					}
				}
			}
		}

		/// <summary>
		/// Clear all transition highlights.
		/// </summary>
		public static void ClearTransitionHighlights(this StateMachineGraphView graphView)
		{
			if (graphView == null)
			{
				return;
			}

			var state = GetOrCreateState(graphView);

			foreach (var edge in graphView.edges)
			{
				if (edge is TransitionEdgeView transitionEdge)
				{
					transitionEdge.SetTransitioning(false);
				}
			}

			state.HighlightedEdges.Clear();
		}

		/// <summary>
		/// Whether the specified graph view has an active lerp animation.
		/// </summary>
		public static bool IsAnimating(StateMachineGraphView graphView)
		{
			return graphView != null &&
				   _visualizationStates.TryGetValue(graphView, out var state) &&
				   state.IsLerping;
		}

		private static void StartLerpAnimation(StateMachineGraphView graphView, Vector2 targetPosition)
		{
			var state = GetOrCreateState(graphView);

			var translate = graphView.contentViewContainer.resolvedStyle.translate;
			state.LerpStartPosition = new Vector3(translate.x, translate.y, 0);
			state.LerpTargetPosition = CalculateTargetViewPosition(graphView, targetPosition);
			state.LerpProgress = 0f;
			state.IsLerping = true;

			if (!state.IsUpdating)
			{
				state.IsUpdating = true;
				state.UpdateDelegate = () => UpdateLerpAnimation(graphView);
				EditorApplication.update += state.UpdateDelegate;
			}
		}

		private static void UpdateLerpAnimation(StateMachineGraphView graphView)
		{
			if (!_visualizationStates.TryGetValue(graphView, out var state) || !state.IsLerping)
			{
				return;
			}

			state.LerpProgress += Time.deltaTime / graphView.LerpDuration;

			var completed = state.LerpProgress >= 1f;
			if (completed)
			{
				state.LerpProgress = 1f;
			}

			var easedProgress = EaseOutCubic(state.LerpProgress);
			var newPosition = Vector3.Lerp(state.LerpStartPosition, state.LerpTargetPosition, easedProgress);

			var currentScale = graphView.contentViewContainer.resolvedStyle.scale;
			graphView.UpdateViewTransform(newPosition, new Vector3(currentScale.value.x, currentScale.value.y, 1));

			if (completed)
			{
				state.IsLerping = false;
				state.IsUpdating = false;

				if (state.UpdateDelegate != null)
				{
					EditorApplication.update -= state.UpdateDelegate;
					state.UpdateDelegate = null;
				}
			}
		}

		private static Vector3 CalculateTargetViewPosition(StateMachineGraphView graphView, Vector2 targetNodeCenter)
		{
			var viewCenter = graphView.contentRect.size / 2f;
			var scale = graphView.contentViewContainer.resolvedStyle.scale.value.x;

			return new Vector3(
				viewCenter.x - targetNodeCenter.x * scale,
				viewCenter.y - targetNodeCenter.y * scale,
				0);
		}

		private static void CenterViewOnPosition(StateMachineGraphView graphView, Vector2 position)
		{
			var targetPosition = CalculateTargetViewPosition(graphView, position);
			var currentScale = graphView.contentViewContainer.resolvedStyle.scale;
			graphView.UpdateViewTransform(targetPosition, new Vector3(currentScale.value.x, currentScale.value.y, 1));
		}

		private static float EaseOutCubic(float t)
		{
			return 1f - Mathf.Pow(1f - t, 3f);
		}

		private static RuntimeVisualizationState GetOrCreateState(StateMachineGraphView graphView)
		{
			if (!_visualizationStates.TryGetValue(graphView, out var state))
			{
				state = new RuntimeVisualizationState();
				_visualizationStates[graphView] = state;
			}

			return state;
		}

		/// <summary>
		/// Clean up state when a graph view is disposed.
		/// </summary>
		public static void CleanupVisualizationState(this StateMachineGraphView graphView)
		{
			if (_visualizationStates.TryGetValue(graphView, out var state))
			{
				if (state.UpdateDelegate != null)
				{
					EditorApplication.update -= state.UpdateDelegate;
					state.UpdateDelegate = null;
				}

				_visualizationStates.Remove(graphView);
			}
		}
	}

	/// <summary>
	/// Internal state for runtime visualization animations.
	/// </summary>
	internal class RuntimeVisualizationState
	{
		public string CurrentHighlightedNodeId;
		public HashSet<string> HighlightedEdges = new HashSet<string>();

		public bool IsLerping;
		public bool IsUpdating;
		public float LerpProgress;
		public Vector3 LerpStartPosition;
		public Vector3 LerpTargetPosition;
		public EditorApplication.CallbackFunction UpdateDelegate;
	}
}

using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Handles edge connection events and shows the state search window when an edge is dropped without connecting.
	/// </summary>
	public class EdgeDropListener : IEdgeConnectorListener
	{
		private readonly StateMachineGraphView _graphView;
		private readonly StateMachineGraphWindow _window;
		private GraphViewChange _graphViewChange;

		public EdgeDropListener(StateMachineGraphView graphView, StateMachineGraphWindow window)
		{
			_graphView = graphView;
			_window = window;
			_graphViewChange = new GraphViewChange();
		}

		public void OnDropOutsidePort(Edge edge, Vector2 position)
		{
			// Determine which port the drag started from
			var sourcePort = edge.output ?? edge.input;
			if (sourcePort == null) return;

			// Convert screen position to graph position
			var windowRoot = _graphView.contentViewContainer;
			var graphPosition = windowRoot.WorldToLocal(position);
			var screenPosition = GUIUtility.GUIToScreenPoint(position);

			// Show the state search window
			var searchWindow = ScriptableObject.CreateInstance<EdgeDropStateSearchWindow>();
			searchWindow.Initialize(_graphView, _window, graphPosition, sourcePort);
			SearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
		}

		public void OnDrop(GraphView graphView, Edge edge)
		{
			// Check if these ports are already connected (Unity's internal handling may have already created an edge)
			// If so, skip adding to avoid duplicates
			foreach (var existingEdge in edge.input.connections)
			{
				if (existingEdge.output == edge.output)
				{
					// Already connected - skip
					return;
				}
			}

			_graphViewChange.edgesToCreate = new System.Collections.Generic.List<Edge> { edge };

			edge.input.Connect(edge);
			edge.output.Connect(edge);
			graphView.AddElement(edge);

			if (graphView.graphViewChanged != null)
			{
				graphView.graphViewChanged(_graphViewChange);
			}
		}
	}
}

using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Editor.Debugging;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Provides common context menu functionality for node views.
	/// </summary>
	public static class NodeContextMenuHelper
	{
		/// <summary>
		/// Registers a context menu handler that displays the node ID, copy option, and breakpoint actions.
		/// </summary>
		/// <param name="node">The node view to add the context menu to.</param>
		/// <param name="nodeDataProvider">The node data provider to get the node ID from.</param>
		/// <param name="graph">The graph asset for breakpoint persistence. Pass null to disable breakpoint menu items.</param>
		public static void RegisterContextMenu(Node node, INodeDataProvider nodeDataProvider, StateMachineGraph graph = null)
		{
			node.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
			{
				OnContextMenuPopulate(evt, node, nodeDataProvider, graph);
			});
		}

		private static void OnContextMenuPopulate(
			ContextualMenuPopulateEvent evt,
			Node node,
			INodeDataProvider nodeDataProvider,
			StateMachineGraph graph)
		{
			var nodeId = nodeDataProvider?.NodeData?.Id;
			if (string.IsNullOrEmpty(nodeId)) return;

			var graphView = node.GetFirstAncestorOfType<StateMachineGraphView>();
			var containingRegion = graphView?.FindContainingRegion(nodeId);

			if (containingRegion != null)
			{
				evt.menu.AppendAction(
					"Remove from Group",
					_ => graphView.RemoveNodeFromGroup(node, nodeId),
					DropdownMenuAction.AlwaysEnabled);
			}

			var displayId = TruncateId(nodeId);

			// Breakpoint actions (only for state nodes with a graph)
			if (graph != null && nodeDataProvider?.NodeData is StateNode)
			{
				var breakpoint = graph.GraphViewData.GetNodeEntryBreakpoint(nodeId);
				var hasBreakpoint = breakpoint != null;
				var breakpointLabel = hasBreakpoint ? "Remove Breakpoint" : "Add Breakpoint";

				evt.menu.AppendSeparator();
				evt.menu.AppendAction(
					breakpointLabel,
					_ => BreakpointManager.ToggleBreakpoint(graph, nodeId),
					DropdownMenuAction.AlwaysEnabled);

				if (hasBreakpoint)
				{
					var enableLabel = breakpoint.Enabled ? "Disable Breakpoint" : "Enable Breakpoint";
					evt.menu.AppendAction(
						enableLabel,
						_ => BreakpointManager.SetBreakpointEnabled(graph, breakpoint.Id, !breakpoint.Enabled),
						DropdownMenuAction.AlwaysEnabled);

					evt.menu.AppendAction(
						"Edit Breakpoint Condition...",
						_ => ShowConditionDialog(graph, nodeId, breakpoint.Condition),
						DropdownMenuAction.AlwaysEnabled);
				}
			}

			// Open documentation if the state declares a [StateInfo(HelpUrl = ...)]
			if (nodeDataProvider?.NodeData is StateNode docState && docState.StateType != null)
			{
				var helpUrl = docState.StateType.GetCustomAttribute<StateInfoAttribute>()?.HelpUrl;
				if (!string.IsNullOrEmpty(helpUrl))
				{
					evt.menu.AppendSeparator();
					evt.menu.AppendAction(
						"Open Documentation",
						_ => Application.OpenURL(helpUrl),
						DropdownMenuAction.AlwaysEnabled);
				}
			}

			evt.menu.AppendSeparator();
			evt.menu.AppendAction(
				$"Node ID: {displayId}",
				_ => { },
				DropdownMenuAction.AlwaysDisabled);
			evt.menu.AppendAction(
				"Copy Full Node ID",
				_ => CopyToClipboard(nodeId),
				DropdownMenuAction.AlwaysEnabled);
		}

		private static void ShowConditionDialog(StateMachineGraph graph, string nodeId, string currentCondition)
		{
			BreakpointConditionWindow.Show(graph, nodeId, currentCondition);
		}

		internal static string TruncateId(string id)
		{
			if (string.IsNullOrEmpty(id)) return string.Empty;
			return id.Length <= 12 ? id : id.Substring(0, 8) + "...";
		}

		private static void CopyToClipboard(string text)
		{
			GUIUtility.systemCopyBuffer = text;
		}
	}
}

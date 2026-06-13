using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Layout
{
	/// <summary>
	/// Initializes the StateMachineBuilder layout callback when the editor loads.
	/// </summary>
	[InitializeOnLoad]
	public static class StateMachineBuilderLayoutInitializer
	{
		static StateMachineBuilderLayoutInitializer()
		{
			StateMachineBuilder.LayoutCallback = ApplyLayout;
		}

		private static void ApplyLayout(StateMachineGraph graph, GraphLayoutMode mode)
		{
			switch (mode)
			{
				case GraphLayoutMode.Hierarchical:
					GraphLayoutService.AutoLayout(graph);
					break;
				case GraphLayoutMode.Grid:
					GraphLayoutService.GridLayout(graph);
					break;
				case GraphLayoutMode.None:
				default:
					// No layout applied
					break;
			}
		}
	}
}

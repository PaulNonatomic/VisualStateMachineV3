using UnityEditor;

namespace Nonatomic.VSM.Editor
{
	/// <summary>
	/// Provides menu items for creating StateMachineGraph assets.
	/// </summary>
	public static class StateMachineGraphAssetCreator
	{
		[MenuItem("Assets/Create/Visual State Machine/State Machine Graph", priority = 100)]
		public static void CreateStateMachineGraph()
		{
			AssetCreationUtils.CreateStateMachineGraph();
		}
	}
}

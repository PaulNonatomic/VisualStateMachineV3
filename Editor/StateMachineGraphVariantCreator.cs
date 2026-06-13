using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Editor
{
	/// <summary>
	/// Provides menu items for creating StateMachineGraphVariant assets.
	/// </summary>
	public static class StateMachineGraphVariantCreator
	{
		[MenuItem("Assets/Create/Visual State Machine/State Machine Graph Variant", priority = 101)]
		public static void CreateStateMachineGraphVariant()
		{
			var baseGraph = Selection.activeObject as StateMachineGraph;
			AssetCreationUtils.CreateStateMachineGraphVariant(baseGraph);
		}

		[MenuItem("Assets/Create Variant", true)]
		private static bool CreateVariantFromBaseValidation()
		{
			return Selection.activeObject is StateMachineGraph && Selection.activeObject is not StateMachineGraphVariant;
		}

		[MenuItem("Assets/Create Variant", priority = 200)]
		private static void CreateVariantFromBase()
		{
			var baseGraph = Selection.activeObject as StateMachineGraph;
			if (baseGraph == null) return;

			AssetCreationUtils.CreateStateMachineGraphVariant(baseGraph);
		}
	}
}

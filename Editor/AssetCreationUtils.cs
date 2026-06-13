using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Nonatomic.VSM.Editor
{
	/// <summary>
	/// EndNameEditAction that creates and initializes a StateMachineGraph asset.
	/// </summary>
	internal class CreateStateMachineGraphAction : EndNameEditAction
	{
		public override void Action(int instanceId, string pathName, string resourceFile)
		{
			var asset = CreateInstance<StateMachineGraph>();
			asset.InitializeNewGraph();
			AssetDatabase.CreateAsset(asset, pathName);
			AssetDatabase.SaveAssets();
			Selection.activeObject = asset;
		}
	}

	/// <summary>
	/// EndNameEditAction that creates a StateMachineGraphVariant asset.
	/// </summary>
	internal class CreateStateMachineGraphVariantAction : EndNameEditAction
	{
		public StateMachineGraph BaseGraph { get; set; }

		public override void Action(int instanceId, string pathName, string resourceFile)
		{
			var asset = CreateInstance<StateMachineGraphVariant>();

			if (BaseGraph != null)
			{
				asset.SetBaseGraph(BaseGraph);
			}

			AssetDatabase.CreateAsset(asset, pathName);
			AssetDatabase.SaveAssets();
			Selection.activeObject = asset;
		}
	}

	/// <summary>
	/// Utilities for creating assets in the correct Project window folder.
	/// Uses ProjectWindowUtil which respects the active folder in the Project window.
	/// </summary>
	internal static class AssetCreationUtils
	{
		public static void CreateStateMachineGraph()
		{
			var action = ScriptableObject.CreateInstance<CreateStateMachineGraphAction>();
			var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;

			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
				0,
				action,
				"NewStateMachine.asset",
				icon,
				null);
		}

		public static void CreateStateMachineGraphVariant(StateMachineGraph baseGraph)
		{
			var action = ScriptableObject.CreateInstance<CreateStateMachineGraphVariantAction>();
			action.BaseGraph = baseGraph;

			var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
			var assetName = baseGraph != null
				? $"{baseGraph.name}_Variant"
				: "NewStateMachineVariant";

			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
				0,
				action,
				$"{assetName}.asset",
				icon,
				null);
		}
	}
}

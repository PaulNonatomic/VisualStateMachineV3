using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Nonatomic.VSM.Editor
{
	/// <summary>
	/// EndNameEditAction that creates and initializes a StateMachineGraph asset.
	/// </summary>
	#if UNITY_6000_6_OR_NEWER
	internal class CreateStateMachineGraphAction : AssetCreationEndAction
#else
	internal class CreateStateMachineGraphAction : EndNameEditAction
#endif
	{
#if UNITY_6000_6_OR_NEWER
		public override void Action(EntityId entityId, string pathName, string resourceFile)
#else
		public override void Action(int instanceId, string pathName, string resourceFile)
#endif
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
	#if UNITY_6000_6_OR_NEWER
	internal class CreateStateMachineGraphVariantAction : AssetCreationEndAction
#else
	internal class CreateStateMachineGraphVariantAction : EndNameEditAction
#endif
	{
		public StateMachineGraph BaseGraph { get; set; }

#if UNITY_6000_6_OR_NEWER
		public override void Action(EntityId entityId, string pathName, string resourceFile)
#else
		public override void Action(int instanceId, string pathName, string resourceFile)
#endif
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
#if UNITY_6000_6_OR_NEWER
				default(EntityId),
#else
				0,
#endif
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
#if UNITY_6000_6_OR_NEWER
				default(EntityId),
#else
				0,
#endif
				action,
				$"{assetName}.asset",
				icon,
				null);
		}
	}
}

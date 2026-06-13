using System;
using Nonatomic.VSM.Utils;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Services
{
	/// <summary>
	/// Registers GUID-based type resolution delegates at Editor startup.
	/// </summary>
	[InitializeOnLoad]
	public static class ScriptGuidResolverInit
	{
		static ScriptGuidResolverInit()
		{
			ScriptGuidResolver.ResolveTypeFromGuid = GuidToType;
			ScriptGuidResolver.ResolveGuidFromType = TypeToGuid;
		}

		public static Type GuidToType(string guid)
		{
			if (string.IsNullOrEmpty(guid)) return null;

			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) return null;

			var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
			return script?.GetClass();
		}

		public static string TypeToGuid(Type type)
		{
			if (type == null) return null;

			var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (script != null && script.GetClass() == type)
				{
					return guid;
				}
			}

			return null;
		}
	}
}

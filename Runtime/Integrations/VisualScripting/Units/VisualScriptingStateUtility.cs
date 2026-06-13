using Unity.VisualScripting;
using UnityEngine;

namespace Nonatomic.VSM.VisualScripting.Units
{
	/// <summary>
	/// Shared utility for custom VS units to access the parent VisualScriptingState.
	/// </summary>
	internal static class VisualScriptingStateUtility
	{
		/// <summary>
		/// Retrieves the VisualScriptingState that owns the graph currently executing the flow.
		/// Returns null if the graph is not running inside a VisualScriptingState.
		/// </summary>
		public static VisualScriptingState GetState(Flow flow)
		{
			var go = flow.stack?.gameObject;
			if (go == null) return null;

			var vars = Variables.Object(go);
			if (!vars.IsDefined("_vsmState")) return null;

			return vars.Get("_vsmState") as VisualScriptingState;
		}
	}
}

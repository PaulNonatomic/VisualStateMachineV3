using System;

namespace Nonatomic.VSM.Utils
{
	/// <summary>
	/// Provides utility methods for checking Action delegate types.
	/// Used by port binding and discovery services to identify transition ports.
	/// </summary>
	public static class ActionTypeUtils
	{
		/// <summary>
		/// Checks whether the given type is an Action delegate (non-generic or generic with up to 4 type parameters).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the type is Action, Action&lt;T&gt;, Action&lt;T1,T2&gt;, Action&lt;T1,T2,T3&gt;, or Action&lt;T1,T2,T3,T4&gt;.</returns>
		public static bool IsActionType(Type type)
		{
			if (type == null) return false;
			if (type == typeof(Action)) return true;
			if (!type.IsGenericType) return false;

			var genericDef = type.GetGenericTypeDefinition();
			return genericDef == typeof(Action<>) ||
				   genericDef == typeof(Action<,>) ||
				   genericDef == typeof(Action<,,>) ||
				   genericDef == typeof(Action<,,,>);
		}
	}
}

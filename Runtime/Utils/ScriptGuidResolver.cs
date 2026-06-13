using System;

namespace Nonatomic.VSM.Utils
{
	/// <summary>
	/// Provides GUID-based type resolution via delegates registered by Editor code.
	/// At runtime (in builds), the delegates remain null and callers fall back to
	/// assembly-qualified name resolution.
	/// </summary>
	public static class ScriptGuidResolver
	{
		/// <summary>
		/// Resolves a MonoScript GUID to its Type. Registered by Editor at startup.
		/// </summary>
		public static Func<string, Type> ResolveTypeFromGuid;

		/// <summary>
		/// Resolves a Type to its MonoScript GUID. Registered by Editor at startup.
		/// </summary>
		public static Func<Type, string> ResolveGuidFromType;
	}
}

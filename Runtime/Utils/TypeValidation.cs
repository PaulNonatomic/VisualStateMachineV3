using System;
using UnityEngine;

namespace Nonatomic.VSM.Utils
{
	/// <summary>
	/// Provides type validation for deserialized types to prevent loading of potentially dangerous types.
	/// Used when resolving types from assembly-qualified names stored in asset files.
	/// Uses a denylist approach to block known-dangerous namespaces while allowing user-defined types.
	/// </summary>
	public static class TypeValidation
	{
		/// <summary>
		/// Validates that a type is allowed for use in the state machine.
		/// Blocks types from known-dangerous namespaces and individually dangerous types
		/// while allowing all others, including user-defined types in custom namespaces.
		/// </summary>
		/// <param name="type">The type to validate</param>
		/// <returns>True if the type is allowed, false otherwise</returns>
		public static bool IsAllowedType(Type type)
		{
			if (type == null) return false;

			// Block individually dangerous types regardless of namespace
			if (IsDeniedType(type)) return false;

			var ns = type.Namespace;

			// Allow types without a namespace (global namespace, arrays, etc.)
			if (string.IsNullOrEmpty(ns)) return true;

			// Block known-dangerous namespaces
			if (IsDeniedNamespace(ns)) return false;

			// Allow everything else (user types, VSM types, Unity types, System types, etc.)
			return true;
		}

		/// <summary>
		/// Checks if a namespace is on the deny list.
		/// These namespaces contain types that should not be deserialized from asset files.
		/// </summary>
		private static bool IsDeniedNamespace(string ns)
		{
			if (ns.StartsWith("System.Diagnostics", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.IO", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Net", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Reflection", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Runtime.InteropServices", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Runtime.Serialization", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Runtime.Remoting", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Security", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.CodeDom", StringComparison.Ordinal)) return true;
			if (ns.StartsWith("System.Threading", StringComparison.Ordinal)) return true;

			return false;
		}

		/// <summary>
		/// Checks if an individual type is on the deny list.
		/// These are dangerous types in otherwise-allowed namespaces (e.g. System).
		/// </summary>
		private static bool IsDeniedType(Type type)
		{
			var fullName = type.FullName;
			if (fullName == null) return false;

			if (fullName == "System.AppDomain") return true;
			if (fullName == "System.Environment") return true;

			return false;
		}

		/// <summary>
		/// Safely resolves a type from an assembly-qualified name with validation.
		/// </summary>
		/// <param name="assemblyQualifiedName">The assembly-qualified type name</param>
		/// <param name="logWarnings">If true, logs warnings when types are blocked</param>
		/// <returns>The resolved type if valid and allowed, null otherwise</returns>
		public static Type SafeGetType(string assemblyQualifiedName, bool logWarnings = true)
		{
			if (string.IsNullOrEmpty(assemblyQualifiedName)) return null;

			var type = Type.GetType(assemblyQualifiedName);
			if (type == null) return null;

			if (!IsAllowedType(type))
			{
				if (logWarnings)
				{
					Debug.LogWarning($"[VSM] Type '{type.FullName}' is in a denied namespace and will not be loaded.");
				}
				return null;
			}

			return type;
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Nonatomic.VSM.Utils
{
	/// <summary>
	/// Provides cached reflection lookups for FieldInfo, individual field-by-name,
	/// and attribute discovery. Eliminates repeated reflection calls across
	/// StateMachineInstance, PortBindingService, and other runtime services.
	/// Uses ConcurrentDictionary for thread-safe access from async operations.
	/// </summary>
	public static class ReflectionCache
	{
		public const BindingFlags FieldBindingFlags =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldsByType = new();
		private static readonly ConcurrentDictionary<(Type, string), FieldInfo> FieldByName = new();
		private static readonly ConcurrentDictionary<(FieldInfo, Type), Attribute> AttributeByField = new();
		private static readonly ConcurrentDictionary<(Type, Type), FieldInfo[]> FieldsWithAttribute = new();

		/// <summary>
		/// Gets all instance fields (public and non-public) for a type, cached after first lookup.
		/// </summary>
		public static FieldInfo[] GetFields(Type type)
		{
			return FieldsByType.GetOrAdd(type, t => t.GetFields(FieldBindingFlags));
		}

		/// <summary>
		/// Gets a single field by name from a type, cached after first lookup.
		/// Returns null if the field does not exist.
		/// </summary>
		public static FieldInfo GetField(Type type, string fieldName)
		{
			var key = (type, fieldName);
			return FieldByName.GetOrAdd(key, k => k.Item1.GetField(k.Item2, FieldBindingFlags));
		}

		/// <summary>
		/// Gets a custom attribute from a FieldInfo, cached after first lookup.
		/// Returns null if the attribute is not present.
		/// </summary>
		public static T GetAttribute<T>(FieldInfo field) where T : Attribute
		{
			var key = ((FieldInfo)field, typeof(T));
			var cached = AttributeByField.GetOrAdd(key, k => k.Item1.GetCustomAttribute(k.Item2));
			return (T)cached;
		}

		/// <summary>
		/// Gets all fields on a type that have a specific attribute, cached after first lookup.
		/// </summary>
		public static FieldInfo[] GetFieldsWithAttribute<T>(Type type) where T : Attribute
		{
			var key = (type, typeof(T));

			return FieldsWithAttribute.GetOrAdd(key, k =>
			{
				var allFields = GetFields(k.Item1);
				var matching = new List<FieldInfo>();

				foreach (var field in allFields)
				{
					if (GetAttribute<T>(field) != null)
					{
						matching.Add(field);
					}
				}

				return matching.ToArray();
			});
		}

		/// <summary>
		/// Clears all cached reflection data. Useful when assemblies reload in the editor.
		/// </summary>
		public static void Clear()
		{
			FieldsByType.Clear();
			FieldByName.Clear();
			AttributeByField.Clear();
			FieldsWithAttribute.Clear();
		}
	}
}

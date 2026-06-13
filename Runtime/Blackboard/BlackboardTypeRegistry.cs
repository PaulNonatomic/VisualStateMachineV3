using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Handles serialization and deserialization for a specific blackboard type.
	/// </summary>
	public interface IBlackboardTypeHandler
	{
		/// <summary>
		/// The type this handler manages.
		/// </summary>
		Type HandledType { get; }

		/// <summary>
		/// Display name for the type in menus and UI.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Category for organizing in menus (e.g., "Primitives", "Unity Types", "Math").
		/// </summary>
		string Category { get; }

		/// <summary>
		/// Order within the category for menu display.
		/// </summary>
		int Order { get; }

		/// <summary>
		/// Serializes a value to a string representation.
		/// </summary>
		string Serialize(object value);

		/// <summary>
		/// Deserializes a string back to the typed value.
		/// </summary>
		object Deserialize(string json);

		/// <summary>
		/// Gets the default value for this type.
		/// </summary>
		object GetDefaultValue();
	}

	/// <summary>
	/// Base implementation for primitive type handlers using ToString/Parse pattern.
	/// </summary>
	public abstract class PrimitiveTypeHandler<T> : IBlackboardTypeHandler
	{
		public Type HandledType => typeof(T);
		public abstract string DisplayName { get; }
		public virtual string Category => "Primitives";
		public abstract int Order { get; }

		public string Serialize(object value)
		{
			return value?.ToString();
		}

		public abstract object Deserialize(string json);

		public virtual object GetDefaultValue()
		{
			return default(T);
		}
	}

	/// <summary>
	/// Handler for types that use Unity's JsonUtility for serialization.
	/// </summary>
	public class JsonSerializableTypeHandler<T> : IBlackboardTypeHandler
	{
		private readonly string _displayName;
		private readonly string _category;
		private readonly int _order;
		private readonly T _defaultValue;

		public JsonSerializableTypeHandler(string displayName, string category, int order, T defaultValue = default)
		{
			_displayName = displayName;
			_category = category;
			_order = order;
			_defaultValue = defaultValue;
		}

		public Type HandledType => typeof(T);
		public string DisplayName => _displayName;
		public string Category => _category;
		public int Order => _order;

		public string Serialize(object value)
		{
			if (value == null) return null;
			return JsonUtility.ToJson(value);
		}

		public object Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json)) return _defaultValue;
			try
			{
				return JsonUtility.FromJson<T>(json);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[BlackboardTypeRegistry] Failed to deserialize type '{typeof(T).Name}': {e.Message}. Using default value.");
				return _defaultValue;
			}
		}

		public object GetDefaultValue()
		{
			return _defaultValue;
		}
	}

	/// <summary>
	/// Centralized registry for blackboard type handling.
	/// Provides serialization, deserialization, and type metadata for all supported types.
	/// </summary>
	public static class BlackboardTypeRegistry
	{
		private static readonly ConcurrentDictionary<Type, IBlackboardTypeHandler> _handlers = new();
		private static readonly object _initLock = new();
		private static bool _initialized;

		/// <summary>
		/// All registered type handlers.
		/// </summary>
		public static IEnumerable<IBlackboardTypeHandler> Handlers
		{
			get
			{
				EnsureInitialized();
				return _handlers.Values;
			}
		}

		/// <summary>
		/// Registers a type handler.
		/// </summary>
		public static void Register(IBlackboardTypeHandler handler)
		{
			if (handler == null) return;

			_handlers[handler.HandledType] = handler;
		}

		/// <summary>
		/// Registers an enum type with a default EnumTypeHandler.
		/// Use this for enum types from immutable packages where you cannot apply [BlackboardType].
		/// </summary>
		public static void RegisterEnum(Type enumType, string displayName = null, string category = null, int order = 0)
		{
			if (enumType == null || !enumType.IsEnum) return;
			if (HasHandler(enumType)) return;

			Register(new EnumTypeHandler(enumType, displayName, category, order));
		}

		/// <summary>
		/// Registers an enum type with a default EnumTypeHandler.
		/// Use this for enum types from immutable packages where you cannot apply [BlackboardType].
		/// </summary>
		public static void RegisterEnum<T>(string displayName = null, string category = null, int order = 0) where T : Enum
		{
			RegisterEnum(typeof(T), displayName, category, order);
		}

		/// <summary>
		/// Unregisters a type handler.
		/// Primarily used for test cleanup to ensure test isolation.
		/// </summary>
		public static bool Unregister(Type type)
		{
			if (type == null) return false;

			return _handlers.TryRemove(type, out _);
		}

		/// <summary>
		/// Gets the handler for a specific type.
		/// </summary>
		public static IBlackboardTypeHandler GetHandler(Type type)
		{
			EnsureInitialized();

			if (type == null) return null;

			if (_handlers.TryGetValue(type, out var handler))
			{
				return handler;
			}

			return null;
		}

		/// <summary>
		/// Checks if a type has a registered handler.
		/// </summary>
		public static bool HasHandler(Type type)
		{
			EnsureInitialized();
			return type != null && _handlers.ContainsKey(type);
		}

		/// <summary>
		/// Serializes a value using the appropriate type handler.
		/// Falls back to JsonUtility for unregistered types.
		/// </summary>
		public static string Serialize(object value, Type type)
		{
			if (value == null) return null;

			var handler = GetHandler(type);
			if (handler != null)
			{
				return handler.Serialize(value);
			}

			// Fallback: primitives use ToString, complex types use JsonUtility
			if (type.IsPrimitive)
			{
				return value.ToString();
			}

			if (type == typeof(string))
			{
				return value as string;
			}

			return JsonUtility.ToJson(value);
		}

		/// <summary>
		/// Deserializes a value using the appropriate type handler.
		/// Falls back to JsonUtility for unregistered types.
		/// </summary>
		public static object Deserialize(string json, Type type)
		{
			if (string.IsNullOrEmpty(json)) return null;
			if (type == null) return null;

			var handler = GetHandler(type);
			if (handler != null)
			{
				return handler.Deserialize(json);
			}

			// Fallback for string type
			if (type == typeof(string))
			{
				return json;
			}

			// Fallback: use JsonUtility
			try
			{
				return JsonUtility.FromJson(json, type);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[BlackboardTypeRegistry] Failed to deserialize type '{type?.Name ?? "null"}' using JsonUtility: {e.Message}");
				return null;
			}
		}

		/// <summary>
		/// Gets the default value for a type.
		/// </summary>
		public static object GetDefaultValue(Type type)
		{
			if (type == null) return null;

			var handler = GetHandler(type);
			if (handler != null)
			{
				return handler.GetDefaultValue();
			}

			// Fallback
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}

			return null;
		}

		/// <summary>
		/// Ensures the registry is initialized with default handlers.
		/// Uses double-checked locking for thread-safe initialization.
		/// </summary>
		private static void EnsureInitialized()
		{
			if (_initialized) return;

			lock (_initLock)
			{
				if (_initialized) return;

				RegisterDefaultHandlers();
				RegisterAttributeTypes();
				RegisterListHandlers();
				_initialized = true;
			}
		}

		/// <summary>
		/// Scans all loaded assemblies for types marked with [BlackboardType]
		/// and registers appropriate handlers.
		/// </summary>
		private static void RegisterAttributeTypes()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException e)
				{
					types = e.Types;
				}

				foreach (var type in types)
				{
					if (type == null) continue;
					if (_handlers.ContainsKey(type)) continue;

					var attribute = type.GetCustomAttribute<BlackboardTypeAttribute>();
					if (attribute == null) continue;

					if (type.IsEnum)
					{
						Register(new EnumTypeHandler(
							type,
							attribute.DisplayName,
							attribute.Category,
							attribute.Order));
					}
					else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
					{
						Register(new UnityObjectTypeHandler(
							type,
							attribute.DisplayName,
							attribute.Category,
							attribute.Order));
					}
					else
					{
						RegisterSerializableType(type, attribute);
					}
				}
			}
		}

		/// <summary>
		/// Registers a plain serializable struct/class marked with [BlackboardType], backed by
		/// JsonUtility serialization. The attribute targets Struct and Class, so honouring those
		/// here (not just enum/UnityEngine.Object) completes the attribute's documented contract.
		/// </summary>
		private static void RegisterSerializableType(Type type, BlackboardTypeAttribute attribute)
		{
			if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) return;
			if (typeof(Delegate).IsAssignableFrom(type)) return;

			var displayName = string.IsNullOrEmpty(attribute.DisplayName) ? type.Name : attribute.DisplayName;
			var category = string.IsNullOrEmpty(attribute.Category) ? "Custom" : attribute.Category;
			var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;

			var handlerType = typeof(JsonSerializableTypeHandler<>).MakeGenericType(type);
			var handler = (IBlackboardTypeHandler)Activator.CreateInstance(
				handlerType, displayName, category, attribute.Order, defaultValue);

			Register(handler);
		}

		/// <summary>
		/// Registers List&lt;T&gt; handlers for all currently registered element types.
		/// Called after default and attribute type registration.
		/// </summary>
		private static void RegisterListHandlers()
		{
			var elementTypes = _handlers.Values
				.Select(h => h.HandledType)
				.ToList();

			foreach (var elementType in elementTypes)
			{
				var listType = typeof(List<>).MakeGenericType(elementType);
				if (_handlers.ContainsKey(listType)) continue;

				var elementHandler = _handlers[elementType];
				var displayName = Pluralize(elementHandler.DisplayName);
				Register(new ListTypeHandler(elementType, displayName));
			}
		}

		private static string Pluralize(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			if (name.EndsWith("s", StringComparison.Ordinal)) return name + "es";
			if (name.EndsWith("y", StringComparison.Ordinal)) return name.Substring(0, name.Length - 1) + "ies";
			return name + "s";
		}

		/// <summary>
		/// Registers all default type handlers.
		/// </summary>
		private static void RegisterDefaultHandlers()
		{
			// Primitives
			Register(new IntHandler());
			Register(new FloatHandler());
			Register(new BoolHandler());
			Register(new StringHandler());
			Register(new DoubleHandler());
			Register(new LongHandler());

			// Unity Math types
			Register(new JsonSerializableTypeHandler<Vector2>("Vector2", "Math", 0, Vector2.zero));
			Register(new JsonSerializableTypeHandler<Vector3>("Vector3", "Math", 1, Vector3.zero));
			Register(new JsonSerializableTypeHandler<Vector4>("Vector4", "Math", 2, Vector4.zero));
			Register(new JsonSerializableTypeHandler<Quaternion>("Quaternion", "Math", 3, Quaternion.identity));
			Register(new JsonSerializableTypeHandler<Color>("Color", "Math", 4, Color.white));
			Register(new JsonSerializableTypeHandler<Rect>("Rect", "Math", 5, Rect.zero));
			Register(new JsonSerializableTypeHandler<Bounds>("Bounds", "Math", 6, new Bounds()));
			Register(new JsonSerializableTypeHandler<Vector2Int>("Vector2Int", "Math", 7, Vector2Int.zero));
			Register(new JsonSerializableTypeHandler<Vector3Int>("Vector3Int", "Math", 8, Vector3Int.zero));
			Register(new JsonSerializableTypeHandler<RectInt>("RectInt", "Math", 9, new RectInt()));
			Register(new JsonSerializableTypeHandler<BoundsInt>("BoundsInt", "Math", 10, new BoundsInt()));
		}


		private class IntHandler : PrimitiveTypeHandler<int>
		{
			public override string DisplayName => "Int";
			public override int Order => 0;

			public override object Deserialize(string json)
			{
				return int.TryParse(json, out var result) ? result : 0;
			}
		}

		private class FloatHandler : PrimitiveTypeHandler<float>
		{
			public override string DisplayName => "Float";
			public override int Order => 1;

			public override object Deserialize(string json)
			{
				return float.TryParse(json, out var result) ? result : 0f;
			}
		}

		private class BoolHandler : PrimitiveTypeHandler<bool>
		{
			public override string DisplayName => "Bool";
			public override int Order => 2;

			public override object Deserialize(string json)
			{
				return bool.TryParse(json, out var result) && result;
			}
		}

		private class StringHandler : IBlackboardTypeHandler
		{
			public Type HandledType => typeof(string);
			public string DisplayName => "String";
			public string Category => "Primitives";
			public int Order => 3;

			public string Serialize(object value)
			{
				return value as string;
			}

			public object Deserialize(string json)
			{
				return json;
			}

			public object GetDefaultValue()
			{
				return string.Empty;
			}
		}

		private class DoubleHandler : PrimitiveTypeHandler<double>
		{
			public override string DisplayName => "Double";
			public override int Order => 4;

			public override object Deserialize(string json)
			{
				return double.TryParse(json, out var result) ? result : 0.0;
			}
		}

		private class LongHandler : PrimitiveTypeHandler<long>
		{
			public override string DisplayName => "Long";
			public override int Order => 5;

			public override object Deserialize(string json)
			{
				return long.TryParse(json, out var result) ? result : 0L;
			}
		}

	}
}

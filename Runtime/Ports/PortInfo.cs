using System;
using System.Collections.Generic;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// Base class for port metadata discovered via reflection.
	/// Contains information about a port's identity, type, and display properties.
	/// </summary>
	[Serializable]
	public abstract class PortInfo
	{
		private static readonly Dictionary<Type, string> FriendlyTypeNames = new()
		{
			{ typeof(int), "int" },
			{ typeof(float), "float" },
			{ typeof(bool), "bool" },
			{ typeof(string), "string" },
			{ typeof(double), "double" },
			{ typeof(long), "long" },
		};

		[SerializeField] private string _fieldName;

		[SerializeField] private string _displayName;

		[SerializeField] private string _tooltip;

		[SerializeField] private string _dataTypeAssemblyQualifiedName;

		[NonSerialized] private Type _dataType;

		/// <summary>
		/// The name of the field on the state class.
		/// </summary>
		public string FieldName
		{
			get => _fieldName;
			set => _fieldName = value;
		}

		/// <summary>
		/// Display name shown in the graph editor.
		/// </summary>
		public string DisplayName
		{
			get => _displayName;
			set => _displayName = value;
		}

		/// <summary>
		/// Tooltip shown when hovering over the port.
		/// </summary>
		public string Tooltip
		{
			get => _tooltip;
			set => _tooltip = value;
		}

		/// <summary>
		/// Assembly-qualified name of the data type for serialization.
		/// </summary>
		public string DataTypeAssemblyQualifiedName
		{
			get => _dataTypeAssemblyQualifiedName;
			set => _dataTypeAssemblyQualifiedName = value;
		}

		/// <summary>
		/// The data type handled by this port.
		/// </summary>
		public Type DataType
		{
			get
			{
				if (_dataType == null && !string.IsNullOrEmpty(_dataTypeAssemblyQualifiedName))
				{
					_dataType = TypeValidation.SafeGetType(_dataTypeAssemblyQualifiedName);
				}

				return _dataType;
			}
			set
			{
				_dataType = value;
				_dataTypeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// The direction of this port.
		/// </summary>
		public abstract PortDirection Direction { get; }

		/// <summary>
		/// Caches the type name for serialization.
		/// </summary>
		public void CacheTypeName()
		{
			if (_dataType != null)
			{
				_dataTypeAssemblyQualifiedName = _dataType.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Restores the type from the cached name after deserialization.
		/// </summary>
		public void RestoreType()
		{
			if (!string.IsNullOrEmpty(_dataTypeAssemblyQualifiedName))
			{
				_dataType = TypeValidation.SafeGetType(_dataTypeAssemblyQualifiedName);
			}
		}

		/// <summary>
		/// Gets a user-friendly type name for display.
		/// </summary>
		public string GetFriendlyTypeName()
		{
			var type = DataType;
			if (type == null) return "Unknown";
			if (FriendlyTypeNames.TryGetValue(type, out var name)) return name;

			if (type.IsGenericType)
			{
				var genericDef = type.GetGenericTypeDefinition();
				var genericArgs = type.GetGenericArguments();
				var argNames = new string[genericArgs.Length];

				for (var i = 0; i < genericArgs.Length; i++)
				{
					argNames[i] = GetFriendlyTypeNameForType(genericArgs[i]);
				}

				var baseName = genericDef.Name;
				var tickIndex = baseName.IndexOf('`');
				if (tickIndex > 0)
				{
					baseName = baseName.Substring(0, tickIndex);
				}

				return $"{baseName}<{string.Join(", ", argNames)}>";
			}

			return type.Name;
		}

		private static string GetFriendlyTypeNameForType(Type type)
		{
			return FriendlyTypeNames.TryGetValue(type, out var name) ? name : type.Name;
		}
	}
}

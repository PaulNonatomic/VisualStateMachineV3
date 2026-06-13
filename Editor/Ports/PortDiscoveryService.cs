using System;
using System.Collections.Generic;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Ports;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Ports
{
	/// <summary>
	/// Discovers ports on State types using reflection.
	/// Results are cached for performance.
	/// </summary>
	public static class PortDiscoveryService
	{
		private static readonly Dictionary<Type, StatePortManifest> _cache =
			new Dictionary<Type, StatePortManifest>();


		/// <summary>
		/// Gets or creates the port manifest for a state type.
		/// </summary>
		public static StatePortManifest GetPortManifest(Type stateType)
		{
			if (stateType == null)
			{
				throw new ArgumentNullException(nameof(stateType));
			}

			if (!typeof(State).IsAssignableFrom(stateType))
			{
				throw new ArgumentException($"Type {stateType.Name} is not a State");
			}

			if (_cache.TryGetValue(stateType, out var cached))
			{
				return cached;
			}

			var manifest = DiscoverPorts(stateType);
			_cache[stateType] = manifest;
			return manifest;
		}

		/// <summary>
		/// Clears the cache. Call when assemblies are reloaded.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
			ReflectionCache.Clear();
		}

		/// <summary>
		/// Gets all state types in the current domain that can be used in the graph.
		/// </summary>
		public static IEnumerable<Type> GetAllStateTypes()
		{
			var stateBaseType = typeof(State);

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
					if (type != null &&
						type != stateBaseType &&
						!type.IsAbstract &&
						stateBaseType.IsAssignableFrom(type))
					{
						yield return type;
					}
				}
			}
		}

		private static StatePortManifest DiscoverPorts(Type stateType)
		{
			var manifest = new StatePortManifest
			{
				StateType = stateType
			};

			var fields = ReflectionCache.GetFields(stateType);

			foreach (var field in fields)
			{
				// Skip backing fields
				if (field.Name.StartsWith("<"))
				{
					continue;
				}

				// Check for DataIn attribute
				var inputAttr = ReflectionCache.GetAttribute<DataInAttribute>(field);
				if (inputAttr != null)
				{
					manifest.InputPorts.Add(CreateInputPortInfo(field, inputAttr));
					continue;
				}

				// Check for DataOut attribute
				var outputAttr = ReflectionCache.GetAttribute<DataOutAttribute>(field);
				if (outputAttr != null)
				{
					manifest.OutputPorts.Add(CreateOutputPortInfo(field, outputAttr));
					continue;
				}

				// Check for Transition attribute
				var transitionAttr = ReflectionCache.GetAttribute<TransitionAttribute>(field);
				if (transitionAttr != null)
				{
					if (!ActionTypeUtils.IsActionType(field.FieldType))
					{
						Debug.LogWarning(
							$"[Transition] attribute on {stateType.Name}.{field.Name} requires Action type, " +
							$"found {field.FieldType.Name}");
						continue;
					}

					manifest.TransitionPorts.Add(CreateTransitionPortInfo(field, transitionAttr));
				}
			}

			// Sort transition ports by priority
			manifest.TransitionPorts.Sort((a, b) => a.Priority.CompareTo(b.Priority));

			return manifest;
		}

		private static InputPortInfo CreateInputPortInfo(FieldInfo field, DataInAttribute attr)
		{
			var info = new InputPortInfo
			{
				FieldName = field.Name,
				DisplayName = attr.DisplayName ?? StringFormatUtils.FormatFieldName(field.Name),
				Tooltip = attr.Tooltip,
				DataType = field.FieldType,
				IsRequired = attr.Required,
				DefaultValue = GetDefaultValue(field)
			};

			return info;
		}

		private static OutputPortInfo CreateOutputPortInfo(FieldInfo field, DataOutAttribute attr)
		{
			var info = new OutputPortInfo
			{
				FieldName = field.Name,
				DisplayName = attr.DisplayName ?? StringFormatUtils.FormatFieldName(field.Name),
				Tooltip = attr.Tooltip,
				DataType = field.FieldType
			};

			return info;
		}

		private static TransitionPortInfo CreateTransitionPortInfo(FieldInfo field, TransitionAttribute attr)
		{
			var info = new TransitionPortInfo
			{
				FieldName = field.Name,
				DisplayName = attr.DisplayName ?? StringFormatUtils.FormatTransitionName(field.Name),
				Tooltip = attr.Tooltip,
				DataType = field.FieldType,
				ColorHint = attr.Color,
				Priority = attr.Priority
			};

			return info;
		}

		private static object GetDefaultValue(FieldInfo field)
		{
			if (field.FieldType.IsValueType)
			{
				return Activator.CreateInstance(field.FieldType);
			}

			return null;
		}

	}
}

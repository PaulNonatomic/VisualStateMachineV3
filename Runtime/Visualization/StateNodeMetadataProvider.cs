using System;
using System.Collections.Generic;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Kind of port shown on a runtime node, used to pick a dot colour.
	/// </summary>
	public enum RuntimePortKind
	{
		Enter,
		Transition,
		DataIn,
		DataOut
	}

	/// <summary>
	/// A single port row on a runtime node (the Enter trigger, a transition, or a data in/out field).
	/// </summary>
	public sealed class RuntimePortInfo
	{
		public string FieldName { get; }
		public string DisplayName { get; }
		public RuntimePortKind Kind { get; }

		public RuntimePortInfo(string fieldName, string displayName, RuntimePortKind kind)
		{
			FieldName = fieldName;
			DisplayName = displayName;
			Kind = kind;
		}
	}

	/// <summary>
	/// Immutable, reflection-derived description of a state type's ports and styling.
	/// Contains only data that depends on the <see cref="Type"/> — never on the active theme —
	/// so it is safe to cache process-wide. The themed header-colour fallback is applied by the
	/// view at build time, not stored here.
	/// </summary>
	public sealed class StateNodeMetadata
	{
		/// <summary>Shared empty instance for unresolved/null state types.</summary>
		public static readonly StateNodeMetadata Empty = new StateNodeMetadata(
			Array.Empty<RuntimePortInfo>(), Array.Empty<RuntimePortInfo>(), null);

		public IReadOnlyList<RuntimePortInfo> Inputs { get; }
		public IReadOnlyList<RuntimePortInfo> Outputs { get; }

		/// <summary>
		/// The colour parsed from the state's <see cref="StateStyleAttribute"/>, or null when the
		/// attribute is absent or its colour string is invalid. When null, the view substitutes the
		/// theme's default header colour.
		/// </summary>
		public Color? StyleColor { get; }

		public StateNodeMetadata(IReadOnlyList<RuntimePortInfo> inputs, IReadOnlyList<RuntimePortInfo> outputs, Color? styleColor)
		{
			Inputs = inputs;
			Outputs = outputs;
			StyleColor = styleColor;
		}
	}

	/// <summary>
	/// Resolves and caches <see cref="StateNodeMetadata"/> for state types via reflection.
	/// The cache holds only type-immutable data, so a single shared cache is safe across every
	/// <see cref="RuntimeGraphView"/> regardless of their themes.
	/// </summary>
	public static class StateNodeMetadataProvider
	{
		private static readonly Dictionary<Type, StateNodeMetadata> _cache = new Dictionary<Type, StateNodeMetadata>();

		/// <summary>
		/// Returns the metadata for a state type, building and caching it on first request.
		/// Returns <see cref="StateNodeMetadata.Empty"/> for a null type.
		/// </summary>
		public static StateNodeMetadata Get(Type stateType)
		{
			if (stateType == null)
			{
				return StateNodeMetadata.Empty;
			}

			if (_cache.TryGetValue(stateType, out var cached))
			{
				return cached;
			}

			var metadata = Build(stateType);
			_cache[stateType] = metadata;
			return metadata;
		}

		/// <summary>
		/// Clears the cache. Primarily useful for tests; not needed in normal runtime flow.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}

		private static StateNodeMetadata Build(Type stateType)
		{
			// Discovery mirrors the editor's PortDiscoveryService so runtime port labels and
			// ordering match the graph editor: shared display-name formatters, Action-type
			// validation for transitions, and priority sorting of transition ports.
			var inputs = new List<RuntimePortInfo>
			{
				new RuntimePortInfo("Enter", "Enter", RuntimePortKind.Enter)
			};
			var dataOutputs = new List<RuntimePortInfo>();
			var transitions = new List<(RuntimePortInfo port, int priority)>();

			foreach (var field in stateType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				// Skip compiler-generated backing fields, matching the editor.
				if (field.Name.StartsWith("<"))
				{
					continue;
				}

				var dataIn = field.GetCustomAttribute<DataInAttribute>();
				if (dataIn != null)
				{
					var name = string.IsNullOrEmpty(dataIn.DisplayName) ? StringFormatUtils.FormatFieldName(field.Name) : dataIn.DisplayName;
					inputs.Add(new RuntimePortInfo(field.Name, name, RuntimePortKind.DataIn));
					continue;
				}

				var dataOut = field.GetCustomAttribute<DataOutAttribute>();
				if (dataOut != null)
				{
					var name = string.IsNullOrEmpty(dataOut.DisplayName) ? StringFormatUtils.FormatFieldName(field.Name) : dataOut.DisplayName;
					dataOutputs.Add(new RuntimePortInfo(field.Name, name, RuntimePortKind.DataOut));
					continue;
				}

				var transition = field.GetCustomAttribute<TransitionAttribute>();
				if (transition != null && ActionTypeUtils.IsActionType(field.FieldType))
				{
					var name = string.IsNullOrEmpty(transition.DisplayName) ? StringFormatUtils.FormatTransitionName(field.Name) : transition.DisplayName;
					transitions.Add((new RuntimePortInfo(field.Name, name, RuntimePortKind.Transition), transition.Priority));
				}
			}

			// Transitions sorted by priority (matching the editor), then data-out ports.
			transitions.Sort((a, b) => a.priority.CompareTo(b.priority));
			var outputs = new List<RuntimePortInfo>(transitions.Count + dataOutputs.Count);
			foreach (var t in transitions)
			{
				outputs.Add(t.port);
			}

			outputs.AddRange(dataOutputs);

			return new StateNodeMetadata(inputs, outputs, ResolveStyleColor(stateType));
		}

		private static Color? ResolveStyleColor(Type stateType)
		{
			var styleAttr = stateType.GetCustomAttribute<StateStyleAttribute>();
			if (styleAttr == null || string.IsNullOrEmpty(styleAttr.Color))
			{
				return null;
			}

			return ColorUtility.TryParseHtmlString(styleAttr.Color, out var color) ? color : (Color?)null;
		}
	}
}

using System;
using System.Collections.Generic;
using Nonatomic.VSM.Editor.Blackboard;
using UnityEngine;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Ports
{
	/// <summary>
	/// Service providing type-specific colors and icons for ports.
	/// </summary>
	public static class PortTypeStyleService
	{

		private static readonly Dictionary<string, string> TypeIconNames = new()
		{
			{ "int", "d_Grid.MoveTool" },
			{ "float", "d_Grid.MoveTool" },
			{ "bool", "d_Toggle Icon" },
			{ "string", "d_Font Icon" },
			{ "Vector2", "d_MoveTool" },
			{ "Vector3", "d_MoveTool" },
			{ "Vector4", "d_MoveTool" },
			{ "Quaternion", "d_RotateTool" },
			{ "Color", "d_ColorPicker.ColorCycle" },
			{ "LayerMask", "d_FilterByType" },
			{ "GameObject", "d_GameObject Icon" },
			{ "Transform", "d_Transform Icon" },
			{ "object", "d_Prefab Icon" },
		};

		private static readonly Color DefaultTypeColor = new Color(0.6f, 0.6f, 0.6f); // Gray
		private static readonly Color TransitionColor = new Color(0.8f, 0.8f, 0.8f);  // Light gray
		private static readonly Color FlowColor = new Color(0.9f, 0.9f, 0.9f);        // White-ish

		/// <summary>
		/// Gets the color associated with a data type.
		/// </summary>
		public static Color GetTypeColor(Type type)
		{
			if (type == null)
				return DefaultTypeColor;

			// Check for Action type (transitions) - special case not in registry
			if (type == typeof(Action) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Action<>)))
				return TransitionColor;

			// Use the centralized registry for type colors
			return BlackboardTypeEditorRegistry.GetTypeColor(type);
		}

		/// <summary>
		/// Gets the color for transition ports.
		/// </summary>
		public static Color GetTransitionColor()
		{
			return TransitionColor;
		}

		/// <summary>
		/// Gets the color for flow ports (Enter port).
		/// </summary>
		public static Color GetFlowColor()
		{
			return FlowColor;
		}

		/// <summary>
		/// Gets the CSS class name for a data type.
		/// </summary>
		public static string GetTypeCssClass(Type type)
		{
			if (type == null)
				return "port-type-object";

			if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
				return "port-type-int";

			if (type == typeof(float) || type == typeof(double))
				return "port-type-float";

			if (type == typeof(bool))
				return "port-type-bool";

			if (type == typeof(string))
				return "port-type-string";

			if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Quaternion))
				return "port-type-vector";

			return "port-type-object";
		}

		/// <summary>
		/// Gets a Unity built-in icon for a data type.
		/// Returns null if no suitable icon is found.
		/// </summary>
		public static Texture2D GetTypeIcon(Type type)
		{
			if (type == null)
				return null;

			var typeName = GetFriendlyTypeName(type);

			// Try to get icon by friendly name
			if (TypeIconNames.TryGetValue(typeName, out var iconName))
			{
				var content = EditorGUIUtility.IconContent(iconName);
				return content?.image as Texture2D;
			}

			// Special handling for Unity Object types
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				// Try to get the type-specific icon
				var content = EditorGUIUtility.ObjectContent(null, type);
				if (content?.image != null)
					return content.image as Texture2D;

				// Fall back to generic prefab icon
				content = EditorGUIUtility.IconContent("d_Prefab Icon");
				return content?.image as Texture2D;
			}

			// For enums, use dropdown icon
			if (type.IsEnum)
			{
				var content = EditorGUIUtility.IconContent("d_FilterByLabel");
				return content?.image as Texture2D;
			}

			return null;
		}

		/// <summary>
		/// Gets a friendly display name for a type.
		/// </summary>
		private static string GetFriendlyTypeName(Type type)
		{
			if (type == null) return "object";

			if (type == typeof(int)) return "int";
			if (type == typeof(float)) return "float";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(string)) return "string";
			if (type == typeof(double)) return "double";
			if (type == typeof(long)) return "long";

			if (type == typeof(Vector2)) return "Vector2";
			if (type == typeof(Vector3)) return "Vector3";
			if (type == typeof(Vector4)) return "Vector4";
			if (type == typeof(Quaternion)) return "Quaternion";
			if (type == typeof(Color)) return "Color";

			if (type == typeof(GameObject)) return "GameObject";
			if (type == typeof(Transform)) return "Transform";

			return type.Name;
		}
	}
}

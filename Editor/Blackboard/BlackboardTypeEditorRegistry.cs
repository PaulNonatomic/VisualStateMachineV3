using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Blackboard
{
	/// <summary>
	/// Editor-specific type information for the blackboard.
	/// </summary>
	public class BlackboardTypeEditorInfo
	{
		/// <summary>
		/// The type this info describes.
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// Display name for menus and UI.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Category for organizing in menus.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Order within the category.
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// Color for ports and UI elements.
		/// </summary>
		public Color Color { get; set; }

		/// <summary>
		/// Factory function to create the default value editor field.
		/// </summary>
		public Func<object, Action<object>, VisualElement> FieldFactory { get; set; }

		/// <summary>
		/// Whether this type should appear in the "Add Variable" menu.
		/// </summary>
		public bool ShowInMenu { get; set; } = true;
	}

	/// <summary>
	/// Centralized registry for blackboard type editor support.
	/// Provides field factories, colors, and menu organization for the editor UI.
	/// </summary>
	public static class BlackboardTypeEditorRegistry
	{
		private static readonly Dictionary<Type, BlackboardTypeEditorInfo> _typeInfos = new();
		private static bool _initialized;

		/// <summary>
		/// All registered type infos.
		/// </summary>
		public static IEnumerable<BlackboardTypeEditorInfo> TypeInfos
		{
			get
			{
				EnsureInitialized();
				return _typeInfos.Values;
			}
		}

		/// <summary>
		/// Registers editor info for a type.
		/// </summary>
		public static void Register(BlackboardTypeEditorInfo info)
		{
			if (info?.Type == null) return;
			_typeInfos[info.Type] = info;
		}

		/// <summary>
		/// Registers an enum type with default editor support (EnumField, green color).
		/// Use this for enum types from immutable packages where you cannot apply [BlackboardType].
		/// </summary>
		public static void RegisterEnum(Type enumType, string displayName = null, string category = null, int order = 0)
		{
			if (enumType == null || !enumType.IsEnum) return;
			if (_typeInfos.ContainsKey(enumType)) return;

			Register(new BlackboardTypeEditorInfo
			{
				Type = enumType,
				DisplayName = displayName ?? enumType.Name,
				Category = category ?? "Enums",
				Order = order,
				Color = new Color(0.6f, 0.8f, 0.6f),
				FieldFactory = (value, onChange) => CreateEnumField(enumType, value, onChange)
			});
		}

		/// <summary>
		/// Registers an enum type with default editor support (EnumField, green color).
		/// Use this for enum types from immutable packages where you cannot apply [BlackboardType].
		/// </summary>
		public static void RegisterEnum<T>(string displayName = null, string category = null, int order = 0) where T : Enum
		{
			RegisterEnum(typeof(T), displayName, category, order);
		}

		/// <summary>
		/// Gets the editor info for a type.
		/// </summary>
		public static BlackboardTypeEditorInfo GetInfo(Type type)
		{
			EnsureInitialized();

			if (type == null) return null;

			if (_typeInfos.TryGetValue(type, out var info))
			{
				return info;
			}

			return null;
		}

		/// <summary>
		/// Gets the color for a type.
		/// </summary>
		public static Color GetTypeColor(Type type)
		{
			var info = GetInfo(type);
			if (info != null)
			{
				return info.Color;
			}

			// Fallback colors for unregistered types
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				return new Color(0.4f, 0.7f, 1f); // Light blue for Unity objects
			}

			if (type?.IsEnum == true)
			{
				return new Color(0.6f, 0.8f, 0.6f); // Soft green for enums
			}

			return new Color(0.5f, 0.5f, 0.5f); // Gray default
		}

		/// <summary>
		/// Gets the display name for a type.
		/// </summary>
		public static string GetDisplayName(Type type)
		{
			if (type == null) return "?";

			var info = GetInfo(type);
			if (info != null)
			{
				return info.DisplayName;
			}

			// Fallback
			return type.Name;
		}

		/// <summary>
		/// Creates a default value field for a type.
		/// </summary>
		public static VisualElement CreateDefaultValueField(Type type, object currentValue, Action<object> onValueChanged)
		{
			if (type == null) return null;

			var info = GetInfo(type);
			if (info?.FieldFactory != null)
			{
				return info.FieldFactory(currentValue, onValueChanged);
			}

			// Fallback for Unity Object types
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				return CreateObjectField(type, currentValue, onValueChanged);
			}

			// Fallback for enums
			if (type.IsEnum)
			{
				return CreateEnumField(type, currentValue, onValueChanged);
			}

			return null;
		}

		/// <summary>
		/// Gets types organized by category for menu building.
		/// </summary>
		public static IEnumerable<IGrouping<string, BlackboardTypeEditorInfo>> GetMenuCategories()
		{
			EnsureInitialized();

			return _typeInfos.Values
				.Where(info => info.ShowInMenu)
				.OrderBy(info => GetCategoryOrder(info.Category))
				.ThenBy(info => info.Order)
				.GroupBy(info => info.Category);
		}

		private static int GetCategoryOrder(string category)
		{
			return category switch
			{
				"Primitives" => 0,
				"Math" => 1,
				"Unity Objects" => 2,
				"Enums" => 3,
				"Custom" => 4,
				"Collections" => 5,
				_ => 99
			};
		}

		private static void EnsureInitialized()
		{
			if (_initialized) return;
			_initialized = true;

			RegisterDefaultTypes();
			RegisterAttributeTypes();
			RegisterListEditors();
		}

		/// <summary>
		/// Scans all loaded assemblies for enum types marked with [BlackboardType]
		/// and registers editor info for each.
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
					if (_typeInfos.ContainsKey(type)) continue;

					var attribute = type.GetCustomAttribute<BlackboardTypeAttribute>();
					if (attribute == null) continue;

					if (type.IsEnum)
					{
						RegisterEnum(
							type,
							attribute.DisplayName,
							attribute.Category,
							attribute.Order);
					}
					else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
					{
						Register(new BlackboardTypeEditorInfo
						{
							Type = type,
							DisplayName = attribute.DisplayName ?? type.Name,
							Category = attribute.Category ?? "Custom",
							Order = attribute.Order,
							Color = new Color(0.4f, 0.7f, 1f),
							FieldFactory = (value, onChange) => CreateObjectField(type, value, onChange)
						});
					}
					else if (!type.IsAbstract && !type.IsGenericTypeDefinition)
					{
						var serializableType = type;
						Register(new BlackboardTypeEditorInfo
						{
							Type = serializableType,
							DisplayName = attribute.DisplayName ?? serializableType.Name,
							Category = attribute.Category ?? "Custom",
							Order = attribute.Order,
							Color = new Color(0.72f, 0.6f, 0.9f),
							FieldFactory = (value, onChange) => CreateSerializableTypeField(serializableType, value, onChange)
						});
					}
				}
			}
		}

		private static void RegisterDefaultTypes()
		{
			// Primitives
			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(int),
				DisplayName = "Int",
				Category = "Primitives",
				Order = 0,
				Color = new Color(0.2f, 0.6f, 0.8f),
				FieldFactory = (value, onChange) =>
				{
					var field = new IntegerField("Default");
					field.value = value != null ? (int)value : 0;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(float),
				DisplayName = "Float",
				Category = "Primitives",
				Order = 1,
				Color = new Color(0.4f, 0.8f, 0.4f),
				FieldFactory = (value, onChange) =>
				{
					var field = new FloatField("Default");
					field.value = value != null ? (float)value : 0f;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(bool),
				DisplayName = "Bool",
				Category = "Primitives",
				Order = 2,
				Color = new Color(0.8f, 0.2f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new Toggle("Default");
					field.value = value != null && (bool)value;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(string),
				DisplayName = "String",
				Category = "Primitives",
				Order = 3,
				Color = new Color(0.8f, 0.6f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new TextField("Default");
					field.value = value as string ?? string.Empty;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(double),
				DisplayName = "Double",
				Category = "Primitives",
				Order = 4,
				Color = new Color(0.4f, 0.8f, 0.4f),
				ShowInMenu = false, // Less commonly used, hide from menu
				FieldFactory = (value, onChange) =>
				{
					var field = new DoubleField("Default");
					field.value = value != null ? (double)value : 0.0;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(long),
				DisplayName = "Long",
				Category = "Primitives",
				Order = 5,
				Color = new Color(0.2f, 0.6f, 0.8f),
				ShowInMenu = false, // Less commonly used, hide from menu
				FieldFactory = (value, onChange) =>
				{
					var field = new LongField("Default");
					field.value = value != null ? (long)value : 0L;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			// Math types
			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Vector2),
				DisplayName = "Vector2",
				Category = "Math",
				Order = 0,
				Color = new Color(0.8f, 0.8f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new Vector2Field("Default");
					field.value = value != null ? (Vector2)value : Vector2.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Vector3),
				DisplayName = "Vector3",
				Category = "Math",
				Order = 1,
				Color = new Color(0.8f, 0.8f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new Vector3Field("Default");
					field.value = value != null ? (Vector3)value : Vector3.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Vector4),
				DisplayName = "Vector4",
				Category = "Math",
				Order = 2,
				Color = new Color(0.8f, 0.8f, 0.2f),
				ShowInMenu = false, // Less commonly used
				FieldFactory = (value, onChange) =>
				{
					var field = new Vector4Field("Default");
					field.value = value != null ? (Vector4)value : Vector4.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Vector2Int),
				DisplayName = "Vector2Int",
				Category = "Math",
				Order = 3,
				Color = new Color(0.8f, 0.8f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new Vector2IntField("Default");
					field.value = value != null ? (Vector2Int)value : Vector2Int.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Vector3Int),
				DisplayName = "Vector3Int",
				Category = "Math",
				Order = 4,
				Color = new Color(0.8f, 0.8f, 0.2f),
				FieldFactory = (value, onChange) =>
				{
					var field = new Vector3IntField("Default");
					field.value = value != null ? (Vector3Int)value : Vector3Int.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Quaternion),
				DisplayName = "Quaternion",
				Category = "Math",
				Order = 5,
				Color = new Color(0.6f, 0.4f, 0.8f),
				ShowInMenu = false, // Quaternions are tricky to edit directly
				FieldFactory = (value, onChange) =>
				{
					// Display as Euler angles for easier editing
					var container = new VisualElement();
					container.style.flexDirection = FlexDirection.Column;

					var quat = value != null ? (Quaternion)value : Quaternion.identity;
					var euler = quat.eulerAngles;

					var field = new Vector3Field("Default (Euler)");
					field.value = euler;
					field.RegisterValueChangedCallback(evt =>
					{
						onChange(Quaternion.Euler(evt.newValue));
					});

					container.Add(field);
					return container;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Color),
				DisplayName = "Color",
				Category = "Math",
				Order = 6,
				Color = new Color(1f, 1f, 1f),
				FieldFactory = (value, onChange) =>
				{
					var field = new ColorField("Default");
					field.value = value != null ? (Color)value : Color.white;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Rect),
				DisplayName = "Rect",
				Category = "Math",
				Order = 7,
				Color = new Color(0.7f, 0.7f, 0.5f),
				ShowInMenu = false,
				FieldFactory = (value, onChange) =>
				{
					var field = new RectField("Default");
					field.value = value != null ? (Rect)value : Rect.zero;
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Bounds),
				DisplayName = "Bounds",
				Category = "Math",
				Order = 8,
				Color = new Color(0.7f, 0.5f, 0.7f),
				ShowInMenu = false,
				FieldFactory = (value, onChange) =>
				{
					var field = new BoundsField("Default");
					field.value = value != null ? (Bounds)value : new Bounds();
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(AnimationCurve),
				DisplayName = "AnimationCurve",
				Category = "Math",
				Order = 9,
				Color = new Color(0.2f, 0.8f, 0.8f),
				FieldFactory = (value, onChange) =>
				{
					var field = new CurveField("Default");
					field.value = value as AnimationCurve ?? new AnimationCurve();
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Gradient),
				DisplayName = "Gradient",
				Category = "Math",
				Order = 10,
				Color = new Color(0.9f, 0.5f, 0.9f),
				FieldFactory = (value, onChange) =>
				{
					var field = new GradientField("Default");
					field.value = value as Gradient ?? new Gradient();
					field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
					return field;
				}
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(LayerMask),
				DisplayName = "LayerMask",
				Category = "Math",
				Order = 11,
				Color = new Color(0.5f, 0.7f, 0.5f),
				FieldFactory = (value, onChange) =>
				{
					var field = new LayerMaskField("Default");
					field.value = value != null ? (LayerMask)value : default;
					field.RegisterValueChangedCallback(evt => onChange((LayerMask)evt.newValue));
					return field;
				}
			});

			// Unity Object types
			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(GameObject),
				DisplayName = "GameObject",
				Category = "Unity Objects",
				Order = 0,
				Color = new Color(0.2f, 0.8f, 0.6f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(GameObject), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Transform),
				DisplayName = "Transform",
				Category = "Unity Objects",
				Order = 1,
				Color = new Color(0.2f, 0.6f, 0.4f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(Transform), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(StateMachineGraph),
				DisplayName = "State Machine Graph",
				Category = "Unity Objects",
				Order = 2,
				Color = new Color(0.4f, 0.3f, 0.5f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(StateMachineGraph), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(ScriptableObject),
				DisplayName = "ScriptableObject",
				Category = "Unity Objects",
				Order = 3,
				Color = new Color(0.4f, 0.6f, 0.6f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(ScriptableObject), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Sprite),
				DisplayName = "Sprite",
				Category = "Unity Objects",
				Order = 4,
				Color = new Color(0.6f, 0.7f, 0.9f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(Sprite), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Texture2D),
				DisplayName = "Texture2D",
				Category = "Unity Objects",
				Order = 5,
				Color = new Color(0.5f, 0.6f, 0.8f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(Texture2D), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(Material),
				DisplayName = "Material",
				Category = "Unity Objects",
				Order = 6,
				Color = new Color(0.7f, 0.5f, 0.6f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(Material), value, onChange)
			});

			Register(new BlackboardTypeEditorInfo
			{
				Type = typeof(AudioClip),
				DisplayName = "AudioClip",
				Category = "Unity Objects",
				Order = 7,
				Color = new Color(0.9f, 0.7f, 0.3f),
				FieldFactory = (value, onChange) => CreateObjectField(typeof(AudioClip), value, onChange)
			});
		}

		private static void RegisterListEditors()
		{
			var elementInfos = _typeInfos.Values.ToList();

			foreach (var elementInfo in elementInfos)
			{
				var elementType = elementInfo.Type;
				var listType = typeof(List<>).MakeGenericType(elementType);
				if (_typeInfos.ContainsKey(listType)) continue;

				var displayName = Pluralize(elementInfo.DisplayName);
				var capturedElementType = elementType;
				var elementColor = elementInfo.Color;
				var desaturatedColor = new Color(
					elementColor.r * 0.7f + 0.15f,
					elementColor.g * 0.7f + 0.15f,
					elementColor.b * 0.7f + 0.15f);

				Register(new BlackboardTypeEditorInfo
				{
					Type = listType,
					DisplayName = displayName,
					Category = "Collections",
					Order = elementInfo.Order,
					Color = desaturatedColor,
					FieldFactory = (value, onChange) => CreateListField(capturedElementType, value, onChange)
				});
			}
		}

		private static VisualElement CreateListField(Type elementType, object value, Action<object> onChange)
		{
			var listType = typeof(List<>).MakeGenericType(elementType);
			var list = value as IList ?? (IList)Activator.CreateInstance(listType);

			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Column;

			var foldout = new Foldout();
			foldout.text = $"Default ({list.Count} elements)";
			foldout.value = false;
			container.Add(foldout);

			var elementsContainer = new VisualElement();
			elementsContainer.style.flexDirection = FlexDirection.Column;
			elementsContainer.style.maxHeight = 300;
			elementsContainer.style.overflow = Overflow.Hidden;
			foldout.Add(elementsContainer);

			void RebuildElements()
			{
				elementsContainer.Clear();
				foldout.text = $"Default ({list.Count} elements)";

				for (var i = 0; i < list.Count; i++)
				{
					var index = i;
					var row = new VisualElement();
					row.style.flexDirection = FlexDirection.Row;
					row.style.alignItems = Align.Center;
					row.style.marginBottom = 2;

					var indexLabel = new Label($"[{index}]");
					indexLabel.style.width = 30;
					indexLabel.style.minWidth = 30;
					indexLabel.style.unityTextAlign = TextAnchor.MiddleRight;
					indexLabel.style.marginRight = 4;
					indexLabel.style.fontSize = 11;
					indexLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
					row.Add(indexLabel);

					var elementValue = list[index];
					var elementField = CreateDefaultValueField(elementType, elementValue, newVal =>
					{
						if (index < list.Count)
						{
							list[index] = newVal;
							onChange(list);
						}
					});

					if (elementField != null)
					{
						elementField.style.flexGrow = 1;
						row.Add(elementField);
					}

					var removeButton = new Button(() =>
					{
						if (index < list.Count)
						{
							list.RemoveAt(index);
							onChange(list);
							RebuildElements();
						}
					});
					removeButton.text = "-";
					removeButton.style.width = 24;
					removeButton.style.minWidth = 24;
					removeButton.style.height = 20;
					row.Add(removeButton);

					elementsContainer.Add(row);
				}
			}

			var addButton = new Button(() =>
			{
				var defaultValue = BlackboardTypeRegistry.GetDefaultValue(elementType);
				list.Add(defaultValue);
				onChange(list);
				RebuildElements();
			});
			addButton.text = "+ Add Element";
			addButton.style.marginTop = 4;
			foldout.Add(addButton);

			RebuildElements();

			return container;
		}

		private static string Pluralize(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			if (name.EndsWith("s", StringComparison.Ordinal)) return name + "es";
			if (name.EndsWith("y", StringComparison.Ordinal)) return name.Substring(0, name.Length - 1) + "ies";
			return name + "s";
		}

		private static VisualElement CreateObjectField(Type type, object value, Action<object> onChange)
		{
			var field = new ObjectField("Default");
			field.objectType = type;
			field.value = value as UnityEngine.Object;
			field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
			return field;
		}

		private static VisualElement CreateEnumField(Type type, object value, Action<object> onChange)
		{
			var enumValue = value != null ? (Enum)value : (Enum)Enum.GetValues(type).GetValue(0);
			var field = new EnumField("Default", enumValue);
			field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
			return field;
		}

		/// <summary>
		/// Builds a default-value editor for a [BlackboardType] struct/class by reflecting over its
		/// public instance fields and creating a sub-field per supported member type. Members whose
		/// type has no built-in field are shown as a disabled label. For full control over the field,
		/// register a custom FieldFactory via BlackboardTypeEditorRegistry.Register.
		/// </summary>
		private static VisualElement CreateSerializableTypeField(Type type, object value, Action<object> onChange)
		{
			var container = new VisualElement();

			object boxed;
			if (value != null && type.IsInstanceOfType(value))
			{
				boxed = value;
			}
			else if (type.IsValueType)
			{
				boxed = Activator.CreateInstance(type);
			}
			else
			{
				boxed = TryCreateInstance(type);
			}

			if (boxed == null)
			{
				var unavailable = new Label($"{type.Name} (no parameterless constructor)");
				unavailable.SetEnabled(false);
				container.Add(unavailable);
				return container;
			}

			var members = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
			if (members.Length == 0)
			{
				var empty = new Label($"{type.Name} (no public fields)");
				empty.SetEnabled(false);
				container.Add(empty);
			}

			foreach (var member in members)
			{
				container.Add(CreateMemberField(member, boxed, () => onChange(boxed)));
			}

			return container;
		}

		private static VisualElement CreateMemberField(FieldInfo member, object boxed, Action notifyChanged)
		{
			var label = ObjectNames.NicifyVariableName(member.Name);
			var current = member.GetValue(boxed);
			var t = member.FieldType;

			if (t == typeof(int))
			{
				var f = new IntegerField(label) { value = current is int i ? i : 0 };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(float))
			{
				var f = new FloatField(label) { value = current is float fl ? fl : 0f };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(bool))
			{
				var f = new Toggle(label) { value = current is bool b && b };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(string))
			{
				var f = new TextField(label) { value = current as string ?? string.Empty };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t.IsEnum)
			{
				var enumValue = current as Enum ?? (Enum)Enum.GetValues(t).GetValue(0);
				var f = new EnumField(label, enumValue);
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(Vector2))
			{
				var f = new Vector2Field(label) { value = current is Vector2 v ? v : Vector2.zero };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(Vector3))
			{
				var f = new Vector3Field(label) { value = current is Vector3 v ? v : Vector3.zero };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(Vector4))
			{
				var f = new Vector4Field(label) { value = current is Vector4 v ? v : Vector4.zero };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}
			if (t == typeof(Color))
			{
				var f = new ColorField(label) { value = current is Color c ? c : Color.white };
				f.RegisterValueChangedCallback(evt => { member.SetValue(boxed, evt.newValue); notifyChanged(); });
				return f;
			}

			var unsupported = new Label($"{label} ({t.Name})");
			unsupported.SetEnabled(false);
			return unsupported;
		}

		private static object TryCreateInstance(Type type)
		{
			try
			{
				return Activator.CreateInstance(type);
			}
			catch
			{
				return null;
			}
		}
	}
}

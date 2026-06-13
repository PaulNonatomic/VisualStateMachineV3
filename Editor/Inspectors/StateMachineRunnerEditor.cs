using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Inspectors
{
	/// <summary>
	/// Custom editor for StateMachineRunner using UI Toolkit.
	/// Draws graph field, shared context, base properties, derived properties,
	/// exposed variable overrides, and runtime controls.
	/// </summary>
	[CustomEditor(typeof(StateMachineRunner), true)]
	[CanEditMultipleObjects]
	public class StateMachineRunnerEditor : UnityEditor.Editor
	{
		private static readonly HashSet<string> BasePropertyNames = new()
		{
			"_graph",
			"_autoStart",
			"_useAsyncMethods",
			"_updateMode",
			"_variableOverrides",
			"_sharedContextObject",
			"m_Script"
		};

		private VisualElement _root;
		private VisualElement _sharedContextContainer;
		private VisualElement _derivedPropertiesContainer;
		private VisualElement _exposedVariablesContainer;
		private VisualElement _runtimeContainer;

		private SerializedProperty _graphProperty;
		private SerializedProperty _sharedContextObjectProperty;

		private bool _isGenericRunner;
		private bool _needsSync;
		private bool _exposedVariablesFoldout = true;
		private IVisualElementScheduledItem _runtimeUpdateSchedule;

		private bool _runtimeInfoBuilt;
		private Toggle _isRunningToggle;
		private TextField _currentStateField;

		public override VisualElement CreateInspectorGUI()
		{
			if (target == null) return new VisualElement();

			_graphProperty = serializedObject.FindProperty("_graph");
			_sharedContextObjectProperty = serializedObject.FindProperty("_sharedContextObject");
			_isGenericRunner = IsGenericStateMachineRunner(target.GetType());

			_root = new VisualElement();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.visualstatemachinev3/Editor/Styles/Inspectors.uss");
			if (styleSheet != null)
			{
				_root.styleSheets.Add(styleSheet);
			}

			// Graph field
			var graphField = new PropertyField(serializedObject.FindProperty("_graph"), "Graph");
			graphField.tooltip = "The state machine graph to run";
			graphField.RegisterValueChangeCallback(OnGraphChanged);
			_root.Add(graphField);

			// Shared context field (hidden for generic runners)
			_sharedContextContainer = new VisualElement();
			if (!_isGenericRunner)
			{
				BuildSharedContextField();
			}
			_root.Add(_sharedContextContainer);

			// Base properties
			_root.Add(new PropertyField(serializedObject.FindProperty("_autoStart"), "Auto Start")
				{ tooltip = "Automatically start on Awake" });
			_root.Add(new PropertyField(serializedObject.FindProperty("_useAsyncMethods"), "Use Async Methods")
				{ tooltip = "Use async lifecycle methods" });
			_root.Add(new PropertyField(serializedObject.FindProperty("_updateMode"), "Update Mode")
				{ tooltip = "When to call Update" });

			// Derived class properties
			_derivedPropertiesContainer = new VisualElement();
			BuildDerivedClassProperties();
			_root.Add(_derivedPropertiesContainer);

			// Exposed variables
			_exposedVariablesContainer = new VisualElement();
			_exposedVariablesContainer.AddToClassList("vsm-runner__exposed-variables");
			_root.Add(_exposedVariablesContainer);

			// Runtime info
			_runtimeContainer = new VisualElement();
			_runtimeContainer.AddToClassList("vsm-runner__runtime-section");
			_root.Add(_runtimeContainer);

			// Subscribe to blackboard events
			StateMachineGraph.OnBlackboardChanged += OnBlackboardChanged;
			StateMachineGraph.OnBlackboardVariableRenamed += OnBlackboardVariableRenamed;

			// Clean up subscriptions when element is detached
			_root.RegisterCallback<DetachFromPanelEvent>(_ =>
			{
				StateMachineGraph.OnBlackboardChanged -= OnBlackboardChanged;
				StateMachineGraph.OnBlackboardVariableRenamed -= OnBlackboardVariableRenamed;
				_runtimeUpdateSchedule?.Pause();
			});

			// Initial sync and auto-populate
			_needsSync = true;
			if (!_isGenericRunner)
			{
				AutoPopulateSharedContext();
			}

			// Schedule periodic updates for exposed variables and runtime info
			_runtimeUpdateSchedule = _root.schedule.Execute(() =>
			{
				if (_needsSync)
				{
					RebuildExposedVariables();
					_needsSync = false;
				}

				UpdateRuntimeInfo();
			}).Every(200);

			// Initial build of exposed variables
			_root.schedule.Execute(() =>
			{
				RebuildExposedVariables();
				_needsSync = false;
			});

			return _root;
		}

		private void OnGraphChanged(SerializedPropertyChangeEvent evt)
		{
			_needsSync = true;

			if (!_isGenericRunner)
			{
				AutoPopulateSharedContext();
				RebuildSharedContextField();
			}
		}

		private void OnBlackboardChanged(StateMachineGraph graph, string variableKey)
		{
			if (target == null) return;
			var runner = (StateMachineRunner)target;
			if (runner.Graph == graph)
			{
				_needsSync = true;
			}
		}

		private void OnBlackboardVariableRenamed(StateMachineGraph graph, string oldKey, string newKey)
		{
			if (target == null) return;
			var runner = (StateMachineRunner)target;
			if (runner.Graph != graph) return;

			var variableOverride = runner.VariableOverrides.FirstOrDefault(o => o.Key == oldKey);
			if (variableOverride != null)
			{
				Undo.RecordObject(target, "Rename Variable Override");
				variableOverride.Key = newKey;
				EditorUtility.SetDirty(target);
				serializedObject.Update();
				_needsSync = true;
			}
		}

		private void BuildSharedContextField()
		{
			_sharedContextContainer.Clear();

			if (_sharedContextObjectProperty == null) return;

			var graph = _graphProperty?.objectReferenceValue as StateMachineGraph;
			if (graph == null) return;

			var contextType = graph.ContextType ?? graph.GetRequiredContextTypeFromStates();
			if (contextType == null) return;
			if (!typeof(UnityEngine.Object).IsAssignableFrom(contextType)) return;

			var typeName = ObjectNames.NicifyVariableName(contextType.Name);

			var objectField = new ObjectField(typeName)
			{
				objectType = contextType,
				allowSceneObjects = true,
				tooltip = $"The graph's states require a '{typeName}' reference to function correctly"
			};
			objectField.BindProperty(_sharedContextObjectProperty);
			_sharedContextContainer.Add(objectField);

			// Validation help box
			var helpBox = new HelpBox("", HelpBoxMessageType.Warning);
			helpBox.AddToClassList("vsm-inspector__help-box");
			_sharedContextContainer.Add(helpBox);

			// Update validation on value changes
			objectField.RegisterValueChangedCallback(evt =>
			{
				UpdateContextValidation(helpBox, evt.newValue, contextType, typeName);
			});

			// Initial validation
			var current = _sharedContextObjectProperty.objectReferenceValue;
			UpdateContextValidation(helpBox, current, contextType, typeName);
		}

		private void RebuildSharedContextField()
		{
			BuildSharedContextField();
		}

		private void UpdateContextValidation(HelpBox helpBox, UnityEngine.Object current, Type contextType, string typeName)
		{
			if (current != null && !contextType.IsInstanceOfType(current))
			{
				helpBox.text = $"The assigned object is not compatible with '{typeName}'. " +
							   $"Please assign a valid '{typeName}' reference.";
				helpBox.messageType = HelpBoxMessageType.Error;
				helpBox.style.display = DisplayStyle.Flex;
			}
			else if (current == null)
			{
				helpBox.text = $"This graph requires a '{typeName}' reference. " +
							   $"Drag a '{typeName}' component from the scene or this GameObject into the field above.";
				helpBox.messageType = HelpBoxMessageType.Warning;
				helpBox.style.display = DisplayStyle.Flex;
			}
			else
			{
				helpBox.style.display = DisplayStyle.None;
			}
		}

		private void AutoPopulateSharedContext()
		{
			if (_sharedContextObjectProperty == null) return;

			var graph = _graphProperty?.objectReferenceValue as StateMachineGraph;

			if (graph == null)
			{
				if (_sharedContextObjectProperty.objectReferenceValue != null)
				{
					_sharedContextObjectProperty.objectReferenceValue = null;
					serializedObject.ApplyModifiedProperties();
				}

				return;
			}

			var contextType = graph.ContextType ?? graph.GetRequiredContextTypeFromStates();

			// Clear if the current value is incompatible with the new graph
			var current = _sharedContextObjectProperty.objectReferenceValue;
			if (current != null && contextType != null && !contextType.IsInstanceOfType(current))
			{
				_sharedContextObjectProperty.objectReferenceValue = null;
				current = null;
			}

			// Auto-populate if empty and type is a Component
			if (current == null && contextType != null
				&& typeof(Component).IsAssignableFrom(contextType)
				&& targets.Length == 1)
			{
				var sibling = ((Component)target).GetComponent(contextType);
				if (sibling != null)
				{
					_sharedContextObjectProperty.objectReferenceValue = sibling;
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void BuildDerivedClassProperties()
		{
			_derivedPropertiesContainer.Clear();

			if (target.GetType() == typeof(StateMachineRunner)) return;

			var iterator = serializedObject.GetIterator();
			var enterChildren = true;

			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (BasePropertyNames.Contains(iterator.name)) continue;

				var propertyField = new PropertyField(iterator.Copy());
				_derivedPropertiesContainer.Add(propertyField);
			}
		}

		private void RebuildExposedVariables()
		{
			_exposedVariablesContainer.Clear();

			if (target == null) return;
			var runner = (StateMachineRunner)target;
			var graph = runner.Graph;

			SyncOverridesWithGraph();

			if (graph == null) return;

			var exposedVars = graph.BlackboardDefinitions?.Where(d => d.IsExposedInGraph).ToList();
			if (exposedVars == null || exposedVars.Count == 0) return;

			var foldout = new Foldout
			{
				text = "Exposed Variables",
				value = _exposedVariablesFoldout
			};
			foldout.RegisterValueChangedCallback(evt => _exposedVariablesFoldout = evt.newValue);
			_exposedVariablesContainer.Add(foldout);

			foreach (var definition in exposedVars)
			{
				var field = CreateVariableOverrideField(runner, definition);
				field.AddToClassList("vsm-runner__variable-row");
				foldout.Add(field);
			}
		}

		private VisualElement CreateVariableOverrideField(StateMachineRunner runner, BlackboardVariableDefinition definition)
		{
			var variableOverride = FindOrCreateOverride(runner, definition);
			if (variableOverride == null) return new VisualElement();

			var type = definition.ValueType;
			if (type == null) return new VisualElement();

			return CreateValueField(definition.Key, type, variableOverride.Value ?? definition.DefaultValue, newValue =>
			{
				Undo.RecordObject(target, "Change Variable Override");
				variableOverride.Value = newValue;
				EditorUtility.SetDirty(target);
			});
		}

		private BlackboardOverride FindOrCreateOverride(StateMachineRunner runner, BlackboardVariableDefinition definition)
		{
			var overrides = runner.VariableOverrides;
			var existing = overrides.FirstOrDefault(o => o.Key == definition.Key);

			if (existing != null) return existing;

			var newOverride = new BlackboardOverride
			{
				Key = definition.Key,
				ValueType = definition.ValueType
			};

			var defaultValue = definition.DefaultValue;
			if (defaultValue != null)
			{
				newOverride.Value = defaultValue;
			}

			Undo.RecordObject(target, "Add Variable Override");
			overrides.Add(newOverride);
			EditorUtility.SetDirty(target);

			return newOverride;
		}

		private VisualElement CreateValueField(string label, Type type, object currentValue, Action<object> onValueChanged)
		{
			if (type == typeof(int))
			{
				var field = new IntegerField(label) { value = currentValue != null ? (int)currentValue : 0 };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(float))
			{
				var field = new FloatField(label) { value = currentValue != null ? (float)currentValue : 0f };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(bool))
			{
				var field = new Toggle(label) { value = currentValue != null && (bool)currentValue };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(string))
			{
				var field = new TextField(label) { value = currentValue as string ?? string.Empty };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(Vector2))
			{
				var field = new Vector2Field(label) { value = currentValue != null ? (Vector2)currentValue : Vector2.zero };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(Vector3))
			{
				var field = new Vector3Field(label) { value = currentValue != null ? (Vector3)currentValue : Vector3.zero };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (type == typeof(Color))
			{
				var field = new ColorField(label) { value = currentValue != null ? (Color)currentValue : Color.white };
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				var field = new ObjectField(label)
				{
					objectType = type,
					allowSceneObjects = true,
					value = currentValue as UnityEngine.Object
				};
				field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
				return field;
			}

			// Handle List<T> where T : UnityEngine.Object
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
			{
				var elementType = type.GetGenericArguments()[0];
				if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
				{
					return CreateObjectListField(label, elementType, currentValue as IList, onValueChanged);
				}
			}

			return new TextField(label) { value = $"({type.Name})", isReadOnly = true };
		}

		private VisualElement CreateObjectListField(string label, Type elementType, IList currentList, Action<object> onValueChanged)
		{
			var container = new VisualElement();

			var listType = typeof(List<>).MakeGenericType(elementType);
			var list = (IList)Activator.CreateInstance(listType);
			if (currentList != null)
			{
				foreach (var item in currentList) list.Add(item);
			}

			var foldout = new Foldout { text = $"{label} ({list.Count})", value = true };
			foldout.style.marginTop = 4;
			container.Add(foldout);

			var itemsContainer = new VisualElement();
			foldout.Add(itemsContainer);

			void RebuildItems()
			{
				itemsContainer.Clear();
				foldout.text = $"{label} ({list.Count})";

				for (var i = 0; i < list.Count; i++)
				{
					var index = i;
					var row = new VisualElement();
					row.style.flexDirection = FlexDirection.Row;
					row.style.marginBottom = 2;

					var objectField = new ObjectField
					{
						objectType = elementType,
						allowSceneObjects = true,
						value = list[index] as UnityEngine.Object
					};
					objectField.style.flexGrow = 1;
					objectField.RegisterValueChangedCallback(evt =>
					{
						Undo.RecordObject(target, "Change List Element");
						list[index] = evt.newValue;
						onValueChanged(list);
					});
					row.Add(objectField);

					var removeButton = new Button(() =>
					{
						Undo.RecordObject(target, "Remove List Element");
						list.RemoveAt(index);
						onValueChanged(list);
						RebuildItems();
					})
					{
						text = "-"
					};
					removeButton.style.width = 24;
					row.Add(removeButton);

					itemsContainer.Add(row);
				}
			}

			RebuildItems();

			var addButton = new Button(() =>
			{
				Undo.RecordObject(target, "Add List Element");
				list.Add(null);
				onValueChanged(list);
				RebuildItems();
			})
			{
				text = "+"
			};
			addButton.style.alignSelf = Align.FlexEnd;
			addButton.style.width = 24;
			foldout.Add(addButton);

			return container;
		}

		private void SyncOverridesWithGraph()
		{
			if (target == null) return;
			var runner = (StateMachineRunner)target;
			var graph = runner.Graph;
			var overrides = runner.VariableOverrides;

			if (graph == null)
			{
				if (overrides.Count > 0)
				{
					Undo.RecordObject(target, "Clear Variable Overrides");
					overrides.Clear();
					EditorUtility.SetDirty(target);
					serializedObject.Update();
				}

				return;
			}

			var exposedKeys = new HashSet<string>(
				graph.BlackboardDefinitions?.Where(d => d.IsExposedInGraph).Select(d => d.Key) ?? Array.Empty<string>()
			);

			var toRemove = overrides.Where(o => !exposedKeys.Contains(o.Key)).ToList();
			if (toRemove.Count > 0)
			{
				Undo.RecordObject(target, "Sync Variable Overrides");
				foreach (var item in toRemove)
				{
					overrides.Remove(item);
				}

				EditorUtility.SetDirty(target);
				serializedObject.Update();
			}
		}

		private void UpdateRuntimeInfo()
		{
			if (!Application.isPlaying)
			{
				if (_runtimeInfoBuilt)
				{
					_runtimeContainer.Clear();
					_runtimeInfoBuilt = false;
					_isRunningToggle = null;
					_currentStateField = null;
				}

				return;
			}

			if (target == null) return;

			var runner = (StateMachineRunner)target;

			if (!_runtimeInfoBuilt)
			{
				_runtimeContainer.Clear();

				var header = new Label("Runtime Info");
				header.AddToClassList("vsm-inspector__section-header");
				_runtimeContainer.Add(header);

				_isRunningToggle = new Toggle("Is Running") { value = runner.IsRunning };
				_isRunningToggle.SetEnabled(false);
				_runtimeContainer.Add(_isRunningToggle);

				_currentStateField = new TextField("Current State")
				{
					value = runner.CurrentState?.GetType().Name ?? "None",
					isReadOnly = true
				};
				_currentStateField.AddToClassList("vsm-inspector__info-field");
				_runtimeContainer.Add(_currentStateField);

				var buttonRow = new VisualElement();
				buttonRow.AddToClassList("vsm-runner__runtime-buttons");
				_runtimeContainer.Add(buttonRow);

				buttonRow.Add(new Button(() => runner.StartStateMachine()) { text = "Start" });
				buttonRow.Add(new Button(() => runner.StopStateMachine()) { text = "Stop" });
				buttonRow.Add(new Button(() => runner.RestartStateMachine()) { text = "Restart" });

				_runtimeInfoBuilt = true;
				return;
			}

			_isRunningToggle.SetValueWithoutNotify(runner.IsRunning);

			var stateName = runner.CurrentState?.GetType().Name ?? "None";
			if (_currentStateField.value != stateName)
			{
				_currentStateField.SetValueWithoutNotify(stateName);
			}
		}

		private static bool IsGenericStateMachineRunner(Type type)
		{
			var current = type;
			while (current != null && current != typeof(object))
			{
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(StateMachineRunner<>))
				{
					return true;
				}

				current = current.BaseType;
			}

			return false;
		}
	}
}

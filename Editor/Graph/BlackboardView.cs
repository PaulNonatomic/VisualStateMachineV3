using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Editor.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Floating blackboard panel for managing state machine variables.
	/// </summary>
	public class BlackboardView : FloatingPanel
	{
		private readonly StateMachineGraphWindow _window;

		private readonly Dictionary<string, BlackboardVariableElement> _variableElements =
			new Dictionary<string, BlackboardVariableElement>();

		private StateMachineGraph _asset;
		private VisualElement _variableContainer;
		private TextField _searchField;

		public BlackboardView(StateMachineGraphWindow window)
			: base("Blackboard", "Blackboard", new Vector2(10, 10), new Vector2(260, 350))
		{
			_window = window;

			AddToClassList("blackboard-view");
			BuildContent();
		}

		private void BuildContent()
		{
			// Search and add row
			var searchRow = new VisualElement();
			searchRow.style.flexDirection = FlexDirection.Row;
			searchRow.style.marginBottom = 8;
			ContentContainer.Add(searchRow);

			// Search field
			_searchField = new TextField();
			_searchField.style.flexGrow = 1;
			_searchField.RegisterValueChangedCallback(OnSearchChanged);

			var placeholder = new Label("Search...");
			placeholder.style.position = Position.Absolute;
			placeholder.style.left = 4;
			placeholder.style.top = 2;
			placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
			placeholder.pickingMode = PickingMode.Ignore;
			_searchField.Add(placeholder);

			_searchField.RegisterCallback<FocusInEvent>(evt => placeholder.style.display = DisplayStyle.None);
			_searchField.RegisterCallback<FocusOutEvent>(evt =>
			{
				if (string.IsNullOrEmpty(_searchField.value))
					placeholder.style.display = DisplayStyle.Flex;
			});

			searchRow.Add(_searchField);

			// Add button
			var addButton = new Button(ShowAddVariableMenu);
			addButton.text = "+";
			addButton.style.width = 24;
			addButton.style.height = 20;
			addButton.style.marginLeft = 4;
			addButton.style.paddingLeft = 0;
			addButton.style.paddingRight = 0;
			addButton.style.paddingTop = 0;
			addButton.style.paddingBottom = 0;
			addButton.style.fontSize = 14;
			addButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			addButton.tooltip = "Add Variable";
			searchRow.Add(addButton);

			// Variable container
			_variableContainer = new VisualElement();
			ContentContainer.Add(_variableContainer);
		}

		public void PopulateFromAsset(StateMachineGraph asset)
		{
			_asset = asset;
			_variableContainer.Clear();
			_variableElements.Clear();

			if (asset == null) return;

			foreach (var definition in asset.BlackboardDefinitions)
			{
				var element = new BlackboardVariableElement(definition, this, _window);
				_variableContainer.Add(element);
				_variableElements[definition.Key] = element;
			}
		}

		public void RemoveVariable(string key)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Remove Blackboard Variable");
			_asset.RemoveBlackboardVariable(key);
			EditorUtility.SetDirty(_asset);

			PopulateFromAsset(_asset);
		}

		public void DuplicateVariable(BlackboardVariableDefinition original)
		{
			if (_asset == null || original == null) return;

			Undo.RecordObject(_asset, "Duplicate Blackboard Variable");

			// Generate unique name
			var name = GenerateUniqueVariableName(original.Key + "_Copy");

			var duplicate = new BlackboardVariableDefinition
			{
				Key = name,
				ValueType = original.ValueType,
				DefaultValue = original.DefaultValue,
				IsExposed = original.IsExposed,
				IsExposedInGraph = original.IsExposedInGraph,
				Category = original.Category,
				Tooltip = original.Tooltip
			};

			_asset.AddBlackboardVariable(duplicate);
			EditorUtility.SetDirty(_asset);

			PopulateFromAsset(_asset);
		}

		public void RenameVariable(string oldKey, string newKey)
		{
			if (_asset == null) return;

			if (_asset.GetBlackboardVariable(newKey) != null)
			{
				Debug.LogWarning($"Cannot rename variable: A variable with name '{newKey}' already exists.");
				return;
			}

			if (string.IsNullOrWhiteSpace(newKey))
			{
				Debug.LogWarning("Cannot rename variable: Name cannot be empty.");
				return;
			}

			Undo.RecordObject(_asset, "Rename Blackboard Variable");

			var variable = _asset.GetBlackboardVariable(oldKey);
			if (variable == null) return;

			variable.Key = newKey;

			foreach (var node in _asset.GetAllNodes())
			{
				if (node is BlackboardVariableNode varNode && varNode.VariableKey == oldKey)
				{
					varNode.VariableKey = newKey;
					varNode.DisplayName = newKey;
				}
			}

			EditorUtility.SetDirty(_asset);
			_asset.NotifyBlackboardVariableRenamed(oldKey, newKey);
			PopulateFromAsset(_asset);
			_window.GraphView?.PopulateFromAsset(_asset);
		}

		public void SetExposureDirection(BlackboardVariableDefinition definition, ExposureDirection direction)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Set Variable Exposure Direction");
			definition.ExposureDirection = direction;
			EditorUtility.SetDirty(_asset);
			_asset.NotifyBlackboardChanged(definition.Key);
		}

		public void MarkAssetDirty()
		{
			if (_asset == null) return;
			EditorUtility.SetDirty(_asset);
		}

		private void ShowAddVariableMenu()
		{
			var menu = new GenericMenu();
			var categories = BlackboardTypeEditorRegistry.GetMenuCategories().ToList();

			foreach (var category in categories)
			{
				var categoryName = category.Key;
				var isFirst = category == categories.First();

				if (!isFirst)
				{
					menu.AddSeparator("");
				}

				foreach (var typeInfo in category)
				{
					var menuPath = $"{categoryName}/{typeInfo.DisplayName}";
					var type = typeInfo.Type;
					menu.AddItem(new GUIContent(menuPath), false, () => CreateVariable(type));
				}
			}

			menu.ShowAsContext();
		}

		private void CreateVariable(Type type)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Create Blackboard Variable");

			var baseName = char.ToUpper(type.Name[0]) + type.Name.Substring(1);
			var name = GenerateUniqueVariableName(baseName);

			var definition = new BlackboardVariableDefinition
			{
				Key = name,
				ValueType = type
			};

			_asset.AddBlackboardVariable(definition);
			EditorUtility.SetDirty(_asset);

			PopulateFromAsset(_asset);
		}

		private string GenerateUniqueVariableName(string baseName)
		{
			var name = baseName;
			var index = 1;

			while (_asset.GetBlackboardVariable(name) != null)
			{
				name = $"{baseName}{index++}";
			}

			return name;
		}

		private void OnSearchChanged(ChangeEvent<string> evt)
		{
			var search = evt.newValue.ToLower();

			foreach (var kvp in _variableElements)
			{
				var visible = string.IsNullOrEmpty(search) ||
							  kvp.Key.ToLower().Contains(search);
				kvp.Value.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}

	/// <summary>
	/// UI element representing a single blackboard variable.
	/// </summary>
	public class BlackboardVariableElement : VisualElement
	{
		private readonly BlackboardVariableDefinition _definition;
		private readonly BlackboardView _blackboardView;
		private readonly StateMachineGraphWindow _window;

		private Foldout _foldout;
		private VisualElement _header;
		private VisualElement _contentContainer;
		private Label _nameLabel;
		private TextField _nameTextField;
		private bool _isRenaming;

		public BlackboardVariableElement(
			BlackboardVariableDefinition definition,
			BlackboardView blackboardView,
			StateMachineGraphWindow window)
		{
			_definition = definition;
			_blackboardView = blackboardView;
			_window = window;

			style.marginTop = 2;
			style.marginBottom = 2;

			BuildUI();
		}

		private void BuildUI()
		{
			_foldout = new Foldout { value = false };
			_foldout.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			_foldout.style.borderTopLeftRadius = 4;
			_foldout.style.borderTopRightRadius = 4;
			_foldout.style.borderBottomLeftRadius = 4;
			_foldout.style.borderBottomRightRadius = 4;
			Add(_foldout);

			BuildExpandedContent();

			_foldout.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			// Add context menu manipulator for right-click
			this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
		}

		private void BuildContextMenu(ContextualMenuPopulateEvent evt)
		{
			evt.menu.AppendAction("Rename", _ => StartRename());
			evt.menu.AppendAction("Duplicate", _ => _blackboardView.DuplicateVariable(_definition));
			evt.menu.AppendSeparator("");
			evt.menu.AppendAction("Delete", _ => _blackboardView.RemoveVariable(_definition.Key));
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			_foldout.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			CustomizeHeader();
		}

		private void CustomizeHeader()
		{
			var toggle = _foldout.Q<Toggle>();
			if (toggle == null) return;

			_header = toggle;

			var visualInput = toggle.Q(className: "unity-toggle__input");
			if (visualInput != null)
			{
				visualInput.style.flexGrow = 0;
			}

			_contentContainer = new VisualElement();
			_contentContainer.style.flexDirection = FlexDirection.Row;
			_contentContainer.style.flexGrow = 1;
			_contentContainer.style.alignItems = Align.Center;
			toggle.Add(_contentContainer);

			_contentContainer.AddManipulator(new BlackboardDragManipulator(_definition, _window));

			_nameLabel = new Label(_definition.Key);
			_nameLabel.style.flexGrow = 1;
			_nameLabel.style.flexShrink = 1;
			_nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
			_nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_nameLabel.style.overflow = Overflow.Hidden;
			_nameLabel.style.textOverflow = TextOverflow.Ellipsis;
			_nameLabel.tooltip = _definition.Key;
			_contentContainer.Add(_nameLabel);

			// Register at toggle level with TrickleDown to intercept before Foldout handles it
			toggle.RegisterCallback<PointerDownEvent>(OnTogglePointerDown, TrickleDown.TrickleDown);

			_nameTextField = new TextField();
			_nameTextField.style.flexGrow = 1;
			_nameTextField.style.display = DisplayStyle.None;
			_nameTextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
			_nameTextField.RegisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
			_contentContainer.Add(_nameTextField);

			var typeName = GetFriendlyTypeName(_definition.ValueType);
			var typeLabel = new Label(typeName);
			typeLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
			typeLabel.style.fontSize = 10;
			typeLabel.style.flexGrow = 0;
			typeLabel.style.flexShrink = 1;
			typeLabel.style.overflow = Overflow.Hidden;
			typeLabel.style.textOverflow = TextOverflow.Ellipsis;
			typeLabel.style.maxWidth = 70;
			typeLabel.style.paddingLeft = 3;
			typeLabel.style.marginRight = 8;
			typeLabel.tooltip = typeName;
			_contentContainer.Add(typeLabel);

		}

		private void BuildExpandedContent()
		{
			_foldout.contentContainer.style.marginLeft = 0;
			_foldout.contentContainer.style.borderLeftWidth = 2;
			_foldout.contentContainer.style.borderRightWidth = 2;
			_foldout.contentContainer.style.borderTopWidth = 2;
			_foldout.contentContainer.style.borderBottomWidth = 2;
			_foldout.contentContainer.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
			_foldout.contentContainer.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
			_foldout.contentContainer.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
			_foldout.contentContainer.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);

			var contentContainer = new VisualElement();
			contentContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
			contentContainer.style.paddingLeft = 8;
			contentContainer.style.paddingRight = 8;
			contentContainer.style.paddingTop = 8;
			contentContainer.style.paddingBottom = 8;
			contentContainer.style.marginTop = 4;
			contentContainer.style.marginLeft = 0;
			contentContainer.style.marginRight = 0;
			contentContainer.style.marginBottom = 0;

			var defaultValueField = CreateDefaultValueField();
			if (defaultValueField != null)
			{
				contentContainer.Add(defaultValueField);
			}

			// Exposure direction for sub-graph ports
			var exposureField = new EnumField("Sub-Graph Port", _definition.ExposureDirection);
			exposureField.tooltip = "Controls whether this variable appears as a port when the graph is used as a sub-state machine";
			exposureField.RegisterValueChangedCallback(evt =>
			{
				_blackboardView.SetExposureDirection(_definition, (ExposureDirection)evt.newValue);
			});
			StyleFieldLabel(exposureField);
			contentContainer.Add(exposureField);

			_foldout.contentContainer.Add(contentContainer);
		}

		private VisualElement CreateDefaultValueField()
		{
			var type = _definition.ValueType;
			var field = BlackboardTypeEditorRegistry.CreateDefaultValueField(
				type,
				_definition.DefaultValue,
				SetDefaultValue);

			if (field != null)
			{
				StyleFieldLabel(field);
			}

			return field;
		}

		private void SetDefaultValue(object value)
		{
			_definition.DefaultValue = value;
			_blackboardView.MarkAssetDirty();
		}

		private void StyleFieldLabel(VisualElement field)
		{
			if (field is BaseField<int> intField)
			{
				intField.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<float> floatField)
			{
				floatField.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<bool> boolField)
			{
				boolField.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<string> stringField)
			{
				stringField.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<Vector2> vector2Field)
			{
				vector2Field.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<Vector3> vector3Field)
			{
				vector3Field.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<Color> colorField)
			{
				colorField.labelElement.style.minWidth = 60;
			}
			else if (field is BaseField<UnityEngine.Object> objectField)
			{
				objectField.labelElement.style.minWidth = 60;
			}
			else
			{
				var label = field.Q<Label>(className: "unity-base-field__label");
				if (label != null)
				{
					label.style.minWidth = 60;
				}
			}
		}

		private void OnTogglePointerDown(PointerDownEvent evt)
		{
			// Handle double-click on name label area
			if (evt.clickCount == 2 && evt.button == 0)
			{
				// Check if clicking on the name label
				if (evt.target == _nameLabel ||
				    (evt.target is VisualElement ve && _nameLabel.Contains(ve)) ||
				    _nameLabel.worldBound.Contains(evt.position))
				{
					StartRename();
					evt.StopImmediatePropagation();
				}
			}
		}

		private void StartRename()
		{
			if (_isRenaming) return;

			_isRenaming = true;
			_nameLabel.style.display = DisplayStyle.None;
			_nameTextField.style.display = DisplayStyle.Flex;
			_nameTextField.value = _definition.Key;
			_nameTextField.Focus();
			_nameTextField.SelectAll();
		}

		private void OnTextFieldKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
			{
				FinishRename();
			}
			else if (evt.keyCode == KeyCode.Escape)
			{
				CancelRename();
			}

			// Stop all key events from propagating to the GraphView during rename
			// to prevent shortcuts (e.g. 'd' for delete) from consuming input
			evt.StopPropagation();
		}

		private void OnTextFieldFocusOut(FocusOutEvent evt)
		{
			if (_isRenaming)
			{
				FinishRename();
			}
		}

		private void FinishRename()
		{
			if (!_isRenaming) return;

			var newName = _nameTextField.value.Trim();

			if (!string.IsNullOrEmpty(newName) && newName != _definition.Key)
			{
				_blackboardView.RenameVariable(_definition.Key, newName);
			}

			ExitRenameMode();
		}

		private void CancelRename()
		{
			ExitRenameMode();
		}

		private void ExitRenameMode()
		{
			_isRenaming = false;
			_nameLabel.text = _definition.Key;
			_nameLabel.tooltip = _definition.Key;
			_nameLabel.style.display = DisplayStyle.Flex;
			_nameTextField.style.display = DisplayStyle.None;
		}

		private Color GetTypeColor(Type type)
		{
			return BlackboardTypeEditorRegistry.GetTypeColor(type);
		}

		private string GetFriendlyTypeName(Type type)
		{
			return BlackboardTypeEditorRegistry.GetDisplayName(type);
		}
	}
}

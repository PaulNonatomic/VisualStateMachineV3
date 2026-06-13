using System;
using System.Linq;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Inspectors
{
	/// <summary>
	/// Custom property drawer for StateMachineGraph and derived types using UI Toolkit.
	/// Provides an enhanced inspector with:
	/// - Object picker filtered to compatible graph types
	/// - Quick access "Open" button
	/// - Context type information display
	/// - Graph validation status indicator
	/// </summary>
	[CustomPropertyDrawer(typeof(StateMachineGraph), true)]
	public class StateMachineGraphPropertyDrawer : PropertyDrawer
	{
		private VisualElement _infoContainer;
		private ObjectField _objectField;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var root = new VisualElement();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.visualstatemachinev3/Editor/Styles/Inspectors.uss");
			if (styleSheet != null)
			{
				root.styleSheets.Add(styleSheet);
			}

			// Top row: object field + open button
			var row = new VisualElement();
			row.AddToClassList("vsm-property-drawer__row");
			root.Add(row);

			var expectedType = GetExpectedGraphType();

			_objectField = new ObjectField(property.displayName)
			{
				objectType = expectedType,
				allowSceneObjects = false
			};
			_objectField.AddToClassList("vsm-property-drawer__object-field");
			_objectField.BindProperty(property);
			row.Add(_objectField);

			var openButton = new Button(() =>
			{
				var graph = property.objectReferenceValue as StateMachineGraph;
				if (graph != null)
				{
					StateMachineGraphWindow.OpenAsset(graph);
				}
			})
			{
				text = "Open"
			};
			openButton.AddToClassList("vsm-property-drawer__open-button");
			row.Add(openButton);

			// Info line (shown when a graph is assigned)
			_infoContainer = new VisualElement();
			_infoContainer.AddToClassList("vsm-property-drawer__info");
			root.Add(_infoContainer);

			// Update info when value changes and on initial display
			_objectField.RegisterValueChangedCallback(evt =>
			{
				var graph = evt.newValue as StateMachineGraph;
				openButton.SetEnabled(graph != null);
				RefreshInfo(graph);
			});

			// Schedule initial refresh after binding completes
			root.schedule.Execute(() =>
			{
				var graph = property.objectReferenceValue as StateMachineGraph;
				openButton.SetEnabled(graph != null);
				RefreshInfo(graph);
			});

			return root;
		}

		private void RefreshInfo(StateMachineGraph graph)
		{
			_infoContainer.Clear();

			if (graph == null)
			{
				_infoContainer.style.display = DisplayStyle.None;
				return;
			}

			_infoContainer.style.display = DisplayStyle.Flex;

			var nodeCount = graph.GetAllNodes().Count();
			var connectionCount = graph.Connections.Count;

			var statsLabel = new Label($"{nodeCount} nodes, {connectionCount} connections");
			statsLabel.AddToClassList("vsm-property-drawer__info-label");
			_infoContainer.Add(statsLabel);

			if (graph.RequiresContext && graph.ContextType != null)
			{
				var separator = new Label(" | ");
				separator.AddToClassList("vsm-property-drawer__info-label");
				_infoContainer.Add(separator);

				var contextLabel = new Label($"Context: {graph.ContextType.Name}");
				contextLabel.AddToClassList("vsm-property-drawer__info-context");
				_infoContainer.Add(contextLabel);
			}

			var issues = graph.Validate();
			var errorCount = 0;
			var warningCount = 0;

			foreach (var issue in issues)
			{
				if (issue.Severity == GraphValidationSeverity.Error) errorCount++;
				else if (issue.Severity == GraphValidationSeverity.Warning) warningCount++;
			}

			if (errorCount > 0)
			{
				var separator = new Label(" | ");
				separator.AddToClassList("vsm-property-drawer__info-label");
				_infoContainer.Add(separator);

				var errorLabel = new Label($"{errorCount} error(s)");
				errorLabel.AddToClassList("vsm-property-drawer__info-error");
				_infoContainer.Add(errorLabel);
			}
			else if (warningCount > 0)
			{
				var separator = new Label(" | ");
				separator.AddToClassList("vsm-property-drawer__info-label");
				_infoContainer.Add(separator);

				var warningLabel = new Label($"{warningCount} warning(s)");
				warningLabel.AddToClassList("vsm-property-drawer__info-warning");
				_infoContainer.Add(warningLabel);
			}
		}

		private Type GetExpectedGraphType()
		{
			if (fieldInfo != null)
			{
				return fieldInfo.FieldType;
			}

			return typeof(StateMachineGraph);
		}
	}
}

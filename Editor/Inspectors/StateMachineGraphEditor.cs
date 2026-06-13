using System.Linq;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Inspectors
{
	/// <summary>
	/// Custom editor for StateMachineGraph using UI Toolkit.
	/// Displays graph info, validation status, and an "Open in Graph Editor" button.
	/// </summary>
	[CustomEditor(typeof(StateMachineGraph), true)]
	public class StateMachineGraphEditor : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.visualstatemachinev3/Editor/Styles/Inspectors.uss");
			if (styleSheet != null)
			{
				root.styleSheets.Add(styleSheet);
			}

			var graph = (StateMachineGraph)target;

			// Header
			var header = new Label("State Machine Graph");
			header.AddToClassList("vsm-inspector__section-header");
			root.Add(header);

			// Info fields (read-only)
			var entryNodeField = new TextField("Entry Node ID") { isReadOnly = true, value = graph.EntryNodeId ?? "Not Set" };
			entryNodeField.AddToClassList("vsm-inspector__info-field");
			root.Add(entryNodeField);

			var nodeCountField = new IntegerField("Node Count") { isReadOnly = true, value = graph.GetAllNodes().Count() };
			nodeCountField.AddToClassList("vsm-inspector__info-field");
			root.Add(nodeCountField);

			var connectionCountField = new IntegerField("Connection Count") { isReadOnly = true, value = graph.Connections.Count };
			connectionCountField.AddToClassList("vsm-inspector__info-field");
			root.Add(connectionCountField);

			var blackboardCountField = new IntegerField("Blackboard Variables") { isReadOnly = true, value = graph.BlackboardDefinitions.Count };
			blackboardCountField.AddToClassList("vsm-inspector__info-field");
			root.Add(blackboardCountField);

			// Open button
			var openButton = new Button(() => StateMachineGraphWindow.OpenAsset(graph))
			{
				text = "Open in Graph Editor"
			};
			openButton.AddToClassList("vsm-inspector__action-button");
			root.Add(openButton);

			// Validation section
			var validationHeader = new Label("Validation");
			validationHeader.AddToClassList("vsm-inspector__section-header");
			root.Add(validationHeader);

			BuildValidation(root, graph);

			return root;
		}

		private void BuildValidation(VisualElement root, StateMachineGraph graph)
		{
			var issues = graph.Validate();

			if (issues.Count == 0)
			{
				var helpBox = new HelpBox("No issues found.", HelpBoxMessageType.Info);
				helpBox.AddToClassList("vsm-inspector__help-box");
				root.Add(helpBox);
				return;
			}

			foreach (var issue in issues)
			{
				var messageType = issue.Severity switch
				{
					GraphValidationSeverity.Error => HelpBoxMessageType.Error,
					GraphValidationSeverity.Warning => HelpBoxMessageType.Warning,
					_ => HelpBoxMessageType.Info
				};

				var helpBox = new HelpBox(issue.Message, messageType);
				helpBox.AddToClassList("vsm-inspector__help-box");
				root.Add(helpBox);
			}
		}
	}
}

using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Debugging
{
	public class BreakpointConditionWindow : EditorWindow
	{
		private StateMachineGraph _graph;
		private string _nodeId;
		private string _condition = "";
		private TextField _conditionField;

		public static void Show(StateMachineGraph graph, string nodeId, string currentCondition)
		{
			var window = GetWindow<BreakpointConditionWindow>(true, "Breakpoint Condition", true);
			window._graph = graph;
			window._nodeId = nodeId;
			window._condition = currentCondition ?? "";
			window.minSize = new Vector2(350, 120);
			window.maxSize = new Vector2(500, 120);
			window.BuildUI();
			window.ShowUtility();
		}

		private void CreateGUI()
		{
			BuildUI();
		}

		private void BuildUI()
		{
			rootVisualElement.Clear();

			rootVisualElement.style.paddingTop = 8;
			rootVisualElement.style.paddingLeft = 8;
			rootVisualElement.style.paddingRight = 8;
			rootVisualElement.style.paddingBottom = 8;

			var header = new Label("Condition Expression");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.marginBottom = 4;
			rootVisualElement.Add(header);

			var helpBox = new HelpBox(
				"Examples: Health < 10, IsAlive == false, Score >= 100, !GameOver",
				HelpBoxMessageType.Info);
			helpBox.style.marginBottom = 4;
			rootVisualElement.Add(helpBox);

			_conditionField = new TextField("Condition") { value = _condition };
			_conditionField.RegisterValueChangedCallback(evt => _condition = evt.newValue);
			rootVisualElement.Add(_conditionField);

			var spacer = new VisualElement();
			spacer.style.flexGrow = 1;
			rootVisualElement.Add(spacer);

			var buttonRow = new VisualElement();
			buttonRow.style.flexDirection = FlexDirection.Row;
			buttonRow.style.justifyContent = Justify.FlexEnd;
			buttonRow.style.marginTop = 4;
			rootVisualElement.Add(buttonRow);

			var clearButton = new Button(() =>
			{
				_condition = "";
				ApplyCondition();
				Close();
			})
			{
				text = "Clear"
			};
			clearButton.style.width = 60;
			buttonRow.Add(clearButton);

			var applyButton = new Button(() =>
			{
				ApplyCondition();
				Close();
			})
			{
				text = "Apply"
			};
			applyButton.style.width = 60;
			buttonRow.Add(applyButton);

			rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode != KeyCode.Return) return;
				ApplyCondition();
				Close();
			});
		}

		private void ApplyCondition()
		{
			if (_graph == null || string.IsNullOrEmpty(_nodeId)) return;
			BreakpointManager.SetBreakpointCondition(_graph, _nodeId, _condition);
		}
	}
}

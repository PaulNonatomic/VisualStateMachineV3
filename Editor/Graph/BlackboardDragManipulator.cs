using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Manipulator that handles dragging blackboard variables into the graph view.
	/// Stops MouseDown propagation to prevent the Foldout's Toggle from stealing capture,
	/// and manually toggles the foldout on click (no drag).
	/// </summary>
	public class BlackboardDragManipulator : MouseManipulator
	{
		private readonly BlackboardVariableDefinition _definition;
		private readonly StateMachineGraphWindow _window;
		private bool _active;
		private bool _thresholdMet;
		private Vector2 _startPosition;
		private VisualElement _dragIndicator;

		public BlackboardDragManipulator(BlackboardVariableDefinition definition, StateMachineGraphWindow window)
		{
			_definition = definition;
			_window = window;
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
		}

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
			target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			Cleanup();
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
			target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}

		private void OnMouseDown(MouseDownEvent evt)
		{
			if (evt.button != (int)MouseButton.LeftMouse) return;
			if (evt.clickCount == 2) return;

			_active = true;
			_thresholdMet = false;
			_startPosition = evt.mousePosition;

			target.CaptureMouse();

			// Stop propagation so the Foldout's Toggle doesn't steal mouse capture
			evt.StopPropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt)
		{
			if (!_active) return;

			var pos = evt.mousePosition;

			if (!_thresholdMet)
			{
				if ((pos - _startPosition).magnitude <= 5f) return;

				_thresholdMet = true;
				CreateDragIndicator(pos);
			}

			if (_dragIndicator != null)
			{
				_dragIndicator.style.left = pos.x + 10;
				_dragIndicator.style.top = pos.y + 10;
			}

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt)
		{
			if (!_active) return;

			if (_thresholdMet)
			{
				CompleteDrop(evt.mousePosition);
			}
			else
			{
				// No drag occurred — toggle the foldout manually
				var foldout = target.GetFirstAncestorOfType<Foldout>();
				if (foldout != null)
				{
					foldout.value = !foldout.value;
				}
			}

			Cleanup();
			evt.StopPropagation();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			Cleanup();
		}

		private void Cleanup()
		{
			if (_dragIndicator != null)
			{
				_dragIndicator.RemoveFromHierarchy();
				_dragIndicator = null;
			}

			if (_active)
			{
				if (target.HasMouseCapture())
				{
					target.ReleaseMouse();
				}
			}

			_active = false;
			_thresholdMet = false;
		}

		private void CreateDragIndicator(Vector2 mousePosition)
		{
			var headerColor = new Color(0.2f, 0.4f, 0.6f);

			_dragIndicator = new VisualElement();
			_dragIndicator.style.position = Position.Absolute;
			_dragIndicator.style.flexDirection = FlexDirection.Column;
			_dragIndicator.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
			_dragIndicator.style.borderTopLeftRadius = 6;
			_dragIndicator.style.borderTopRightRadius = 6;
			_dragIndicator.style.borderBottomLeftRadius = 6;
			_dragIndicator.style.borderBottomRightRadius = 6;
			_dragIndicator.style.borderTopWidth = 1;
			_dragIndicator.style.borderBottomWidth = 1;
			_dragIndicator.style.borderLeftWidth = 1;
			_dragIndicator.style.borderRightWidth = 1;
			_dragIndicator.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
			_dragIndicator.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
			_dragIndicator.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
			_dragIndicator.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
			_dragIndicator.style.minWidth = 120;
			_dragIndicator.style.opacity = 0.55f;
			_dragIndicator.pickingMode = PickingMode.Ignore;

			// Header bar
			var header = new VisualElement();
			header.style.backgroundColor = new StyleColor(headerColor);
			header.style.borderTopLeftRadius = 5;
			header.style.borderTopRightRadius = 5;
			header.style.paddingLeft = 8;
			header.style.paddingRight = 8;
			header.style.paddingTop = 4;
			header.style.paddingBottom = 4;
			header.pickingMode = PickingMode.Ignore;

			var titleLabel = new Label(_definition.Key);
			titleLabel.style.color = Color.white;
			titleLabel.style.fontSize = 12;
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.pickingMode = PickingMode.Ignore;
			header.Add(titleLabel);
			_dragIndicator.Add(header);

			// Port row
			var portRow = new VisualElement();
			portRow.style.flexDirection = FlexDirection.Row;
			portRow.style.justifyContent = Justify.FlexEnd;
			portRow.style.paddingLeft = 8;
			portRow.style.paddingRight = 8;
			portRow.style.paddingTop = 4;
			portRow.style.paddingBottom = 4;
			portRow.pickingMode = PickingMode.Ignore;

			var typeName = GetFriendlyTypeName(_definition.ValueType);
			var typeLabel = new Label(typeName);
			typeLabel.style.color = new StyleColor(new Color(0.5f, 0.8f, 0.9f, 0.9f));
			typeLabel.style.fontSize = 11;
			typeLabel.pickingMode = PickingMode.Ignore;
			portRow.Add(typeLabel);

			// Port dot
			var portDot = new VisualElement();
			portDot.style.width = 8;
			portDot.style.height = 8;
			portDot.style.borderTopLeftRadius = 4;
			portDot.style.borderTopRightRadius = 4;
			portDot.style.borderBottomLeftRadius = 4;
			portDot.style.borderBottomRightRadius = 4;
			portDot.style.backgroundColor = new StyleColor(headerColor);
			portDot.style.marginLeft = 6;
			portDot.style.alignSelf = Align.Center;
			portDot.pickingMode = PickingMode.Ignore;
			portRow.Add(portDot);

			_dragIndicator.Add(portRow);

			_window.rootVisualElement.Add(_dragIndicator);

			_dragIndicator.style.left = mousePosition.x + 10;
			_dragIndicator.style.top = mousePosition.y + 10;
		}

		private static string GetFriendlyTypeName(System.Type type)
		{
			if (type == null) return "?";
			if (type == typeof(int)) return "int";
			if (type == typeof(float)) return "float";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(string)) return "string";

			if (type.IsGenericType)
			{
				var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
				var args = type.GetGenericArguments();
				var argNames = new string[args.Length];
				for (var i = 0; i < args.Length; i++)
				{
					argNames[i] = GetFriendlyTypeName(args[i]);
				}
				return $"{baseName}<{string.Join(", ", argNames)}>";
			}

			return type.Name;
		}

		private void CompleteDrop(Vector2 mousePosition)
		{
			var graphView = _window.GraphView;
			if (graphView == null) return;

			var graphRect = graphView.worldBound;
			if (!graphRect.Contains(mousePosition)) return;

			var localPos = graphView.contentViewContainer.WorldToLocal(mousePosition);
			ShowNodeTypeMenu(localPos);
		}

		private void ShowNodeTypeMenu(Vector2 position)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent($"Get {_definition.Key}"), false, () => CreateBlackboardNode(position, BlackboardNodeMode.Get));
			menu.AddItem(new GUIContent($"Set {_definition.Key}"), false, () => CreateBlackboardNode(position, BlackboardNodeMode.Set));
			menu.ShowAsContext();
		}

		private void CreateBlackboardNode(Vector2 position, BlackboardNodeMode mode)
		{
			var graphView = _window.GraphView;
			var asset = graphView?.CurrentAsset;

			if (asset == null)
			{
				Debug.LogError("Cannot create blackboard node: No graph asset loaded");
				return;
			}

			Undo.RecordObject(asset, "Create Blackboard Variable Node");

			var node = new BlackboardVariableNode(_definition.Key, _definition.ValueType, mode)
			{
				Position = position
			};

			asset.AddNode(node);

			var nodeView = new BlackboardVariableNodeView(node, graphView);
			nodeView.SetPosition(new Rect(node.Position, Vector2.zero));
			graphView.AddNodeView(nodeView, node.Id);

			EditorUtility.SetDirty(asset);
		}
	}
}

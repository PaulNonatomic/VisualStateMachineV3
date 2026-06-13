using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Displays current viewport coordinates and zoom level in the bottom-right corner of the graph view.
	/// </summary>
	public class CoordinatesDisplayView : VisualElement
	{
		private readonly Label _xValueLabel;
		private readonly Label _yValueLabel;
		private readonly Label _zoomValueLabel;
		private StateMachineGraphView _graphView;
		private IVisualElementScheduledItem _updateSchedule;
		private bool _isDragging;

		public CoordinatesDisplayView(StateMachineGraphView graphView)
		{
			_graphView = graphView;

			// Setup container styling
			name = "coordinates-display";
			AddToClassList("coordinates-display");

			// Position in bottom-right corner
			style.position = Position.Absolute;
			style.right = 8;
			style.bottom = 8;

			// Visual styling
			style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
			style.borderTopLeftRadius = 4;
			style.borderTopRightRadius = 4;
			style.borderBottomLeftRadius = 4;
			style.borderBottomRightRadius = 4;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 4;
			style.paddingBottom = 4;
			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;

			// Create X coordinate group
			var xGroup = CreateCoordinateGroup("X:", out _xValueLabel);
			Add(xGroup);

			// Create Y coordinate group
			var yGroup = CreateCoordinateGroup("Y:", out _yValueLabel);
			Add(yGroup);

			// Create Zoom group
			var zoomGroup = CreateCoordinateGroup("Zoom:", out _zoomValueLabel);
			Add(zoomGroup);

			// Subscribe to graph view events
			SubscribeToGraphView();

			// Initial update
			UpdateCoordinates();
		}

		/// <summary>
		/// Unsubscribes from events when the element is removed from the hierarchy.
		/// </summary>
		public void Cleanup()
		{
			StopContinuousUpdate();
			UnsubscribeFromGraphView();
		}

		/// <summary>
		/// Sets the graph view that the coordinates display tracks.
		/// </summary>
		public void SetGraphView(StateMachineGraphView graphView)
		{
			if (_graphView == graphView) return;

			UnsubscribeFromGraphView();
			_graphView = graphView;
			SubscribeToGraphView();
			UpdateCoordinates();
		}

		private void UnsubscribeFromGraphView()
		{
			if (_graphView != null)
			{
				_graphView.viewTransformChanged -= OnViewTransformChanged;
				_graphView.UnregisterCallback<PointerDownEvent>(OnPointerDown);
				_graphView.UnregisterCallback<PointerUpEvent>(OnPointerUp);
				_graphView.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
				_graphView.UnregisterCallback<WheelEvent>(OnWheelEvent);
			}
		}

		private void SubscribeToGraphView()
		{
			if (_graphView != null)
			{
				_graphView.viewTransformChanged += OnViewTransformChanged;
				_graphView.RegisterCallback<PointerDownEvent>(OnPointerDown);
				_graphView.RegisterCallback<PointerUpEvent>(OnPointerUp);
				_graphView.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
				_graphView.RegisterCallback<WheelEvent>(OnWheelEvent);
			}
		}

		private void OnViewTransformChanged(GraphView graphView)
		{
			UpdateCoordinates();
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			// Start continuous updates when dragging begins
			if (!_isDragging)
			{
				_isDragging = true;
				StartContinuousUpdate();
			}
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			StopDragging();
		}

		private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
		{
			// Also stop when pointer capture is released
			StopDragging();
		}

		private void OnWheelEvent(WheelEvent evt)
		{
			// Update immediately on zoom
			UpdateCoordinates();
		}

		private void StopDragging()
		{
			if (_isDragging)
			{
				_isDragging = false;
				StopContinuousUpdate();
				UpdateCoordinates();
			}
		}

		private void StartContinuousUpdate()
		{
			if (_updateSchedule != null) return;

			// Schedule updates every frame while dragging
			_updateSchedule = schedule.Execute(UpdateCoordinates).Every(16); // ~60fps
		}

		private void StopContinuousUpdate()
		{
			if (_updateSchedule != null)
			{
				_updateSchedule.Pause();
				_updateSchedule = null;
			}
		}

		private void UpdateCoordinates()
		{
			if (_graphView == null) return;

			// Get the view transform from the content container
			var contentContainer = _graphView.contentViewContainer;
			if (contentContainer == null) return;

			// Get position and scale using resolvedStyle (Unity 6+ approach)
			var translate = contentContainer.resolvedStyle.translate;
			var scale = contentContainer.resolvedStyle.scale;

			// The view position is the negative of the translate (world coordinates)
			var viewX = -translate.x;
			var viewY = -translate.y;

			// Calculate zoom percentage from scale
			var zoomPercent = scale.value.x * 100f;

			// Update individual value labels
			_xValueLabel.text = $"{viewX:F0}";
			_yValueLabel.text = $"{viewY:F0}";
			_zoomValueLabel.text = $"{zoomPercent:F0}%";
		}

		private VisualElement CreateCoordinateGroup(string labelText, out Label valueLabel)
		{
			var group = new VisualElement();
			group.AddToClassList("coordinates-group");

			var label = new Label(labelText);
			label.AddToClassList("coordinates-label");
			group.Add(label);

			valueLabel = new Label();
			valueLabel.AddToClassList("coordinates-value");
			group.Add(valueLabel);

			return group;
		}
	}
}

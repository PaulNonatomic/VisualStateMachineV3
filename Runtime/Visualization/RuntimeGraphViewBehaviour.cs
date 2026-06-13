using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Convenience component that wires a <see cref="RuntimeGraphView"/> into a
	/// <see cref="UIDocument"/>, optionally tracking a <see cref="StateMachineRunner"/>.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	public class RuntimeGraphViewBehaviour : MonoBehaviour
	{
		[SerializeField]
		private StateMachineGraph _graph;

		[SerializeField]
		private StateMachineRunner _runner;

		[SerializeField]
		[Tooltip("Optional visual theme. Leave empty to use built-in defaults.")]
		private RuntimeGraphTheme _theme;

		[SerializeField]
		private StyleSheet _styleSheet;

		private UIDocument _document;
		private RuntimeGraphView _view;

		public RuntimeGraphView View => _view;

		/// <summary>
		/// Theme currently assigned to the behaviour (may differ from the live view's
		/// theme if it was changed via <see cref="RuntimeGraphView.SetTheme"/>).
		/// </summary>
		public RuntimeGraphTheme Theme
		{
			get => _theme;
			set
			{
				_theme = value;
				_view?.SetTheme(value);
			}
		}

		private void Awake()
		{
			_document = GetComponent<UIDocument>();
		}

		private void Start()
		{
			BuildView();
			Rebind();
		}

		private void OnEnable()
		{
			if (_runner != null)
			{
				_runner.OnStateChanged += HandleRunnerStateChanged;
			}
		}

		private void OnDisable()
		{
			if (_runner != null)
			{
				_runner.OnStateChanged -= HandleRunnerStateChanged;
			}
		}

		private void OnDestroy()
		{
			// Release the view's live-instance subscription explicitly. DetachFromPanelEvent
			// also covers this, but unbinding here is safe and avoids relying on teardown order.
			_view?.Unbind();
		}

		private void BuildView()
		{
			if (_view != null)
			{
				return;
			}

			var root = _document.rootVisualElement;
			if (root == null)
			{
				return;
			}

			_view = new RuntimeGraphView();
			_view.style.flexGrow = 1;

			if (_theme != null)
			{
				_view.SetTheme(_theme);
			}

			if (_styleSheet != null)
			{
				_view.styleSheets.Add(_styleSheet);
			}

			root.Add(_view);
		}

		/// <summary>
		/// Rebind the view to the current graph + runner. Call after assigning these
		/// fields at runtime (e.g. when the runner has just been initialized).
		/// </summary>
		public void Rebind()
		{
			if (_view == null)
			{
				return;
			}

			var graph = _graph;
			var instance = _runner != null ? _runner.Instance : null;

			if (graph == null && _runner != null)
			{
				graph = _runner.Graph;
			}

			_view.Bind(graph, instance);
		}

		private void HandleRunnerStateChanged(State previous, State next)
		{
			// The runner forwards instance events; nothing to do here today, but the
			// hook is in place for future runner-level effects (toasts, history view).
		}
	}
}

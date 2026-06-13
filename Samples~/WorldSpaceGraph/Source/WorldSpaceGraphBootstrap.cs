using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// Builds the entire world-space graph sample at runtime: ground, creature,
	/// camera, and a UI Toolkit panel that floats above the creature rendering
	/// the live state machine graph.
	/// </summary>
	[DefaultExecutionOrder(-50)]
	public class WorldSpaceGraphBootstrap : MonoBehaviour
	{
		[Header("State Machine")]
		[SerializeField]
		private StateMachineGraph _graph;

		[Header("World Space Panel")]
		[SerializeField]
		[Tooltip("PanelSettings asset configured with Render Mode = World Space.")]
		private PanelSettings _worldSpacePanelSettings;

		[SerializeField]
		private Vector2 _panelSize = new Vector2(640f, 360f);

		[SerializeField]
		[Tooltip("World-space position of the floating graph panel. The panel is fixed at this " +
				 "location so the camera can move freely without the graph chasing the creature.")]
		private Vector3 _panelPosition = new Vector3(0f, 2.0f, 0f);

		[SerializeField]
		[Tooltip("Multiplier on the panel GameObject scale. The natural world size of the panel is " +
				 "(panelSize / PanelSettings.PixelsPerUnit). At default settings (640x360 / 100ppu), " +
				 "scale 0.3 produces ~1.9m x 1.1m — a comfortable billboard above a 1m creature.")]
		private float _panelWorldScale = 0.3f;

		[SerializeField]
		[Tooltip("If true, the panel rotates each frame so it always faces the camera.")]
		private bool _billboardPanel = true;

		[SerializeField]
		[Tooltip("Optional graph theme override. Leave empty to use the RuntimeGraphView defaults (Wire edge style).")]
		private RuntimeGraphTheme _graphTheme;

		[Header("Visuals")]
		[SerializeField]
		private Color _creatureColor = new Color(0.95f, 0.55f, 0.25f);

		[SerializeField]
		private Color _groundColor = new Color(0.16f, 0.18f, 0.22f);

		private Camera _camera;
		private CreatureController _creature;
		private StateMachineRunner _runner;
		private GameObject _panelGameObject;
		private RuntimeGraphView _graphView;

		private void Start()
		{
			BuildEnvironment();
			BuildCreature();
			BuildPanel();
			StartStateMachine();

			if (!SampleInput.Available)
			{
				Debug.LogWarning(
					"WorldSpaceGraphBootstrap: the legacy Input Manager is disabled, so the fly-camera " +
					"and click-to-poke controls are inactive. Set Project Settings > Player > Active Input " +
					"Handling to 'Both' (or 'Input Manager (Old)') to enable them.",
					this);
			}
		}

		private void Update()
		{
			HandleClickInput();

			if (_billboardPanel && _panelGameObject != null && _camera != null)
			{
				// UIDocument world-space panels render on the -Z face (the back of the
				// GameObject's local axes), so we want +Z pointing AWAY from the camera.
				var awayFromCam = _panelGameObject.transform.position - _camera.transform.position;
				if (awayFromCam.sqrMagnitude > 0.0001f)
				{
					_panelGameObject.transform.rotation = Quaternion.LookRotation(awayFromCam, Vector3.up);
				}
			}
		}

		private void BuildEnvironment()
		{
			_camera = Camera.main;
			if (_camera == null)
			{
				var camGo = new GameObject("WorldSpaceGraph Camera");
				_camera = camGo.AddComponent<Camera>();
				camGo.tag = "MainCamera";
				camGo.transform.position = new Vector3(0f, 3f, -5f);
				camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
				_camera.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
				_camera.clearFlags = CameraClearFlags.SolidColor;
				camGo.AddComponent<AudioListener>();
			}

			// Free-fly camera control: RMB to look, WASD/QE to move.
			if (_camera.gameObject.GetComponent<FlyCameraController>() == null)
			{
				_camera.gameObject.AddComponent<FlyCameraController>();
			}

			var lightGo = new GameObject("WorldSpaceGraph Sun");
			lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.1f;
			light.color = new Color(1f, 0.96f, 0.88f);

			var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
			ground.name = "Ground";
			ground.transform.SetParent(transform, false);
			ground.transform.localScale = new Vector3(2f, 1f, 2f);
			TintRenderer(ground.GetComponent<Renderer>(), _groundColor);
		}

		private void BuildCreature()
		{
			var creatureGo = new GameObject("Creature");
			creatureGo.transform.SetParent(transform, false);

			_creature = creatureGo.AddComponent<CreatureController>();

			var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			body.name = "Body";
			body.transform.SetParent(creatureGo.transform, false);
			body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
			body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
			TintRenderer(body.GetComponent<Renderer>(), _creatureColor);

			_creature.Body = body.transform;

			var clickTarget = body.AddComponent<WorldSpaceGraphClickTarget>();
			clickTarget.Creature = _creature;
		}

		private void BuildPanel()
		{
			if (_worldSpacePanelSettings == null)
			{
				Debug.LogWarning(
					"WorldSpaceGraphBootstrap: no PanelSettings assigned. Assign a PanelSettings asset " +
					"with Render Mode = World Space (the sample installer creates one for you).",
					this);
				return;
			}

			// Create the panel GameObject inactive so UIDocument's OnEnable doesn't
			// run before we assign panelSettings. Without this, the UIDocument
			// registers no panel with the UI Toolkit system and never renders.
			_panelGameObject = new GameObject("Graph Panel");
			_panelGameObject.SetActive(false);
			_panelGameObject.transform.SetParent(transform, false);
			_panelGameObject.transform.position = _panelPosition;
			_panelGameObject.transform.localScale = Vector3.one * _panelWorldScale;

			var document = _panelGameObject.AddComponent<UIDocument>();
			document.panelSettings = _worldSpacePanelSettings;

			// Activate so OnEnable runs with panelSettings already in place,
			// which causes the UIDocument to register a real panel + rootVisualElement.
			_panelGameObject.SetActive(true);

			var root = document.rootVisualElement;
			if (root == null)
			{
				Debug.LogError("WorldSpaceGraphBootstrap: rootVisualElement is null after UIDocument activation.", this);
				return;
			}

			root.style.flexGrow = 1;

			_graphView = new RuntimeGraphView();
			if (_graphTheme != null)
			{
				_graphView.SetTheme(_graphTheme);
			}
			// Absolute fill — flexGrow alone wasn't sizing the view to the full panel
			// because the absolutely-positioned interior (edges/nodes) gives a 0 intrinsic size.
			_graphView.style.position = Position.Absolute;
			_graphView.style.left = 0;
			_graphView.style.top = 0;
			_graphView.style.right = 0;
			_graphView.style.bottom = 0;
			_graphView.style.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 0.92f);
			_graphView.style.borderTopLeftRadius = 24;
			_graphView.style.borderTopRightRadius = 24;
			_graphView.style.borderBottomLeftRadius = 24;
			_graphView.style.borderBottomRightRadius = 24;

			root.Add(_graphView);
		}

		private void StartStateMachine()
		{
			if (_creature == null || _graph == null)
			{
				Debug.LogWarning("WorldSpaceGraphBootstrap: missing creature or graph; nothing to run.", this);
				return;
			}

			_runner = _creature.gameObject.AddComponent<StateMachineRunner>();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			_graphView?.Bind(_graph, _runner.Instance);
		}

		private void HandleClickInput()
		{
			if (_creature == null || _camera == null) return;
			if (!SampleInput.GetMouseButtonDown(0)) return;

			var ray = _camera.ScreenPointToRay(SampleInput.MousePosition);
			if (Physics.Raycast(ray, out var hit, 100f))
			{
				var click = hit.collider.GetComponent<WorldSpaceGraphClickTarget>();
				if (click != null && click.Creature == _creature)
				{
					_creature.Poke();
				}
			}
		}

		private static void TintRenderer(Renderer renderer, Color color)
		{
			if (renderer == null) return;

			// MaterialPropertyBlock tints without instantiating a Material (no leak, GPU-instancing
			// friendly). Both _BaseColor (URP/HDRP Lit) and _Color (built-in) are set; a property the
			// active shader does not declare is simply ignored.
			var block = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(block);
			block.SetColor("_BaseColor", color);
			block.SetColor("_Color", color);
			renderer.SetPropertyBlock(block);
		}
	}

	internal class WorldSpaceGraphClickTarget : MonoBehaviour
	{
		public CreatureController Creature;
	}
}

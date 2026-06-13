using System.IO;
using System.Reflection;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Visualization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph.Editor
{
	/// <summary>
	/// Builds the World-Space Graph sample on demand: graph asset, panel settings,
	/// and a scene with a single bootstrap GameObject. After running, open the
	/// generated scene and press Play.
	/// </summary>
	public static class WorldSpaceGraphSampleInstaller
	{
		private const string SampleRoot = "Packages/com.nonatomic.visualstatemachinev3/Samples/WorldSpaceGraph";
		private const string ContentDir = SampleRoot + "/Content";
		private const string GraphAssetPath = ContentDir + "/CreatureGraph.asset";
		private const string PanelSettingsAssetPath = ContentDir + "/CreaturePanelSettings.asset";
		private const string ThemeAssetPath = ContentDir + "/CreatureTheme.asset";
		private const string SceneAssetPath = SampleRoot + "/WorldSpaceGraph.unity";

		[MenuItem("Tools/VSM Samples/Setup World Space Graph Sample")]
		public static void Install()
		{
			EnsureDirectory(ContentDir);

			var graph = CreateOrLoadGraph();
			var panelSettings = CreateOrLoadPanelSettings();
			var theme = CreateOrLoadTheme();
			CreateScene(graph, panelSettings, theme);

			EditorUtility.DisplayDialog(
				"World Space Graph Sample",
				"Sample installed.\n\n" +
				"Open WorldSpaceGraph.unity in Samples/WorldSpaceGraph and press Play.\n\n" +
				"The look of the in-world graph is driven by CreatureTheme — tweak it (or assign your " +
				"own RuntimeGraphTheme) on the WorldSpaceGraph Bootstrap to restyle it.\n\n" +
				"If the floating graph panel does not appear, open CreaturePanelSettings and set " +
				"'Render Mode' to 'World Space'. Camera and click controls require the legacy Input " +
				"Manager (Active Input Handling = 'Both' or 'Input Manager (Old)').",
				"OK");
		}

		private static StateMachineGraph CreateOrLoadGraph()
		{
			// Always regenerate so re-running the installer picks up edits to the layout.
			if (AssetDatabase.LoadAssetAtPath<StateMachineGraph>(GraphAssetPath) != null)
			{
				AssetDatabase.DeleteAsset(GraphAssetPath);
			}

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			// Position layout — entry on the left, idle in middle, branches on the right.
			var entry = graph.GetEntryNode();
			entry.Position = new Vector2(0f, 0f);

			// Positions chosen to fit a 640x360 world-space panel with 200px-wide state nodes,
			// 100px-wide entry node, and 24px padding. Rightmost edge lands at ~604px.
			var idle = new StateNode(typeof(IdleState))
			{
				Position = new Vector2(160f, 0f),
				DisplayName = "Idle",
				IsEntryState = true
			};
			graph.AddNode(idle);

			var wander = new StateNode(typeof(WanderState))
			{
				Position = new Vector2(380f, -110f),
				DisplayName = "Wander"
			};
			graph.AddNode(wander);

			var excited = new StateNode(typeof(ExcitedState))
			{
				Position = new Vector2(380f, 110f),
				DisplayName = "Excited"
			};
			graph.AddNode(excited);

			graph.AddConnection(new Connection(entry.Id, "Start", idle.Id, "Enter", ConnectionType.Transition));
			graph.AddConnection(new Connection(idle.Id, "OnBored", wander.Id, "Enter", ConnectionType.Transition));
			graph.AddConnection(new Connection(idle.Id, "OnPoked", excited.Id, "Enter", ConnectionType.Transition));
			graph.AddConnection(new Connection(wander.Id, "OnArrived", idle.Id, "Enter", ConnectionType.Transition));
			graph.AddConnection(new Connection(excited.Id, "OnSettled", idle.Id, "Enter", ConnectionType.Transition));

			AssetDatabase.CreateAsset(graph, GraphAssetPath);
			AssetDatabase.SaveAssets();
			return graph;
		}

		private static PanelSettings CreateOrLoadPanelSettings()
		{
			// Always regenerate so re-running the installer picks up scaleMode/render mode changes.
			if (AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAssetPath) != null)
			{
				AssetDatabase.DeleteAsset(PanelSettingsAssetPath);
			}

			var settings = ScriptableObject.CreateInstance<PanelSettings>();
			// ConstantPixelSize: panel renders at exactly referenceResolution regardless of DPI.
			// Critical for world-space rendering where we want our pixel-coordinate node layout
			// to map predictably to the mesh (rather than getting DPI-scaled into a corner).
			settings.scaleMode = PanelScaleMode.ConstantPixelSize;
			settings.referenceResolution = new Vector2Int(640, 360);
			TrySetWorldSpaceMode(settings);

			AssetDatabase.CreateAsset(settings, PanelSettingsAssetPath);
			AssetDatabase.SaveAssets();
			return settings;
		}

		private static RuntimeGraphTheme CreateOrLoadTheme()
		{
			// Always regenerate so re-running the installer picks up theme edits.
			if (AssetDatabase.LoadAssetAtPath<RuntimeGraphTheme>(ThemeAssetPath) != null)
			{
				AssetDatabase.DeleteAsset(ThemeAssetPath);
			}

			// A distinctly-styled theme to showcase RuntimeGraphTheme customisation. Tweak this asset
			// (or assign your own) on the WorldSpaceGraph Bootstrap to restyle the in-world graph.
			var theme = ScriptableObject.CreateInstance<RuntimeGraphTheme>();
			theme.EdgeRendererType = RuntimeEdgeRendererType.Wire;
			theme.EdgeColor = new Color(0.40f, 0.55f, 0.85f, 0.9f);
			theme.EdgeColorActive = new Color(1f, 0.78f, 0.25f, 1f);
			theme.EdgeWidth = 2.75f;
			theme.ArrowheadSize = 10f;
			theme.NodeBodyColor = new Color(0.10f, 0.13f, 0.18f, 1f);
			theme.NodeBorderRadius = 10f;
			theme.ActiveBorderColor = new Color(1f, 0.85f, 0.30f, 1f);
			theme.ActiveScaleMultiplier = 1.06f;

			AssetDatabase.CreateAsset(theme, ThemeAssetPath);
			AssetDatabase.SaveAssets();
			return theme;
		}

		/// <summary>
		/// Attempts to set the panel into World Space mode via SerializedObject so the
		/// installer is robust across Unity versions where the public property name
		/// changes. Falls back gracefully — the README and the install dialog tell
		/// the user to flip the toggle manually if needed.
		/// </summary>
		private static void TrySetWorldSpaceMode(PanelSettings settings)
		{
			var so = new SerializedObject(settings);
			var prop = so.FindProperty("m_PanelRenderMode") ?? so.FindProperty("m_RenderMode");
			if (prop != null)
			{
				prop.enumValueIndex = 1; // 0 = ScreenSpaceOverlay, 1 = WorldSpace in current Unity 6 enum.
				so.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		private static void CreateScene(StateMachineGraph graph, PanelSettings panelSettings, RuntimeGraphTheme theme)
		{
			// Ensure assets are flushed before we reference them from the new scene.
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			var bootstrapGo = new GameObject("WorldSpaceGraph Bootstrap");
			var bootstrap = bootstrapGo.AddComponent<WorldSpaceGraphBootstrap>();

			// Assign serialized fields by reflection. SerializedObject was unreliable
			// when called immediately after delete+create asset cycles — fields silently
			// stayed null after SaveScene.
			SetPrivateField(bootstrap, "_graph", graph);
			SetPrivateField(bootstrap, "_worldSpacePanelSettings", panelSettings);
			SetPrivateField(bootstrap, "_graphTheme", theme);
			EditorUtility.SetDirty(bootstrap);

			SceneManager.SetActiveScene(scene);
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene, SceneAssetPath);
		}

		private static void SetPrivateField(object target, string fieldName, object value)
		{
			var field = target.GetType().GetField(
				fieldName,
				BindingFlags.Instance | BindingFlags.NonPublic);

			if (field == null)
			{
				Debug.LogError($"WorldSpaceGraphSampleInstaller: field '{fieldName}' not found on {target.GetType().Name}");
				return;
			}

			field.SetValue(target, value);
		}

		private static void EnsureDirectory(string assetPath)
		{
			if (AssetDatabase.IsValidFolder(assetPath))
			{
				return;
			}

			var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
			var leaf = Path.GetFileName(assetPath);
			if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
			{
				EnsureDirectory(parent);
			}

			AssetDatabase.CreateFolder(parent, leaf);
		}
	}
}

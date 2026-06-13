using UnityEngine;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Visual configuration for <see cref="RuntimeGraphView"/>. Defines colours, sizes,
	/// edge renderer choice, and node default styling. Create one via
	/// <b>Assets &gt; Create &gt; Visual State Machine &gt; Runtime Graph Theme</b> and assign it
	/// to a <see cref="RuntimeGraphViewBehaviour"/>, or call <c>Create()</c> for code-only
	/// setup.
	/// </summary>
	[CreateAssetMenu(menuName = "Visual State Machine/Runtime Graph Theme", fileName = "RuntimeGraphTheme")]
	public class RuntimeGraphTheme : ScriptableObject
	{
		[Header("Edges")]
		[Tooltip("Algorithm used for drawing edges. Default is a fast horizontal bezier; Wire matches the editor's smoother style.")]
		public RuntimeEdgeRendererType EdgeRendererType = RuntimeEdgeRendererType.Wire;

		public Color EdgeColor = new Color(0.55f, 0.62f, 0.78f, 0.85f);
		public Color EdgeColorActive = new Color(1f, 0.65f, 0.25f, 1f);

		[Range(0.5f, 6f)]
		public float EdgeWidth = 2.25f;

		public bool DrawArrowheads = true;

		[Range(4f, 24f)]
		public float ArrowheadSize = 8f;

		[Header("Node Sizing")]
		[Range(80f, 400f)]
		public float StateNodeWidth = 200f;

		[Range(40f, 160f)]
		public float EntryExitNodeWidth = 100f;

		[Range(16f, 48f)]
		public float HeaderHeight = 26f;

		[Range(12f, 32f)]
		public float PortRowHeight = 18f;

		[Range(0f, 16f)]
		public float BodyVerticalPadding = 6f;

		[Range(0f, 24f)]
		public float NodeBorderRadius = 6f;

		[Range(8f, 64f)]
		public float CanvasPadding = 24f;

		[Header("Node Body")]
		public Color NodeBodyColor = new Color(0.13f, 0.16f, 0.20f, 1f);
		public Color BorderColor = new Color(0.05f, 0.07f, 0.10f, 1f);

		[Range(0f, 6f)]
		public float BorderWidth = 1f;

		[Header("Active State Highlight")]
		public Color ActiveBorderColor = new Color(1f, 0.82f, 0.22f, 1f);

		[Range(0.5f, 6f)]
		public float ActiveBorderWidth = 2f;

		[Range(1f, 1.25f)]
		public float ActiveScaleMultiplier = 1.04f;

		[Header("Text")]
		public Color HeaderTextColor = new Color(0.97f, 0.98f, 1f, 1f);
		public Color PortLabelColor = new Color(0.78f, 0.83f, 0.90f, 1f);

		[Range(8, 24)]
		public int HeaderFontSize = 12;

		[Range(8, 18)]
		public int PortLabelFontSize = 10;

		[Header("Default Header Colours (used when [StateStyle] is not present)")]
		public Color EntryHeaderColor = new Color(0.10f, 0.45f, 0.32f, 1f);
		public Color ExitHeaderColor = new Color(0.45f, 0.18f, 0.18f, 1f);
		public Color StateDefaultHeaderColor = new Color(0.20f, 0.30f, 0.45f, 1f);
		public Color SubStateMachineHeaderColor = new Color(0.30f, 0.18f, 0.45f, 1f);

		[Header("Port Dot Colours")]
		public Color EnterPortColor = new Color(0.55f, 0.85f, 0.50f, 1f);
		public Color TransitionPortColor = new Color(0.95f, 0.78f, 0.30f, 1f);
		public Color DataInPortColor = new Color(0.45f, 0.70f, 0.95f, 1f);
		public Color DataOutPortColor = new Color(0.95f, 0.55f, 0.55f, 1f);

		/// <summary>
		/// Creates an in-memory theme with default values. Useful for code-only setup
		/// where you don't want to manage a ScriptableObject asset.
		/// </summary>
		public static RuntimeGraphTheme CreateDefault()
		{
			return CreateInstance<RuntimeGraphTheme>();
		}
	}
}

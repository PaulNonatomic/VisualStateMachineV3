using System;
using Nonatomic.VSM.Editor.Graph.Settings;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Settings for wire-like edge rendering with configurable control points.
	/// </summary>
	public class WireEdgeSettings : IEdgeRendererSettings
	{
		private static WireEdgeSettings _instance;

		/// <summary>
		/// Singleton instance of the wire edge settings.
		/// </summary>
		public static WireEdgeSettings Instance => _instance ??= new WireEdgeSettings();

		/// <summary>
		/// Distance between control points along the edge path (in pixels).
		/// Lower values create more control points for finer curves.
		/// </summary>
		public float ControlPointSpacing { get; set; } = 150f;

		/// <summary>
		/// Radius of the loop curve for backward (right-to-left) connections.
		/// </summary>
		public float BackwardLoopRadius { get; set; } = 50f;

		/// <summary>
		/// Vertical offset for backward connection loops (how far below/above nodes the loop goes).
		/// </summary>
		public float BackwardLoopOffset { get; set; } = 80f;

		/// <summary>
		/// Tension parameter for Catmull-Rom spline (0 = sharp corners, 1 = very smooth).
		/// Default 0.5 provides balanced smoothness.
		/// </summary>
		public float SplineTension { get; set; } = 0.5f;

		/// <summary>
		/// Number of sample points per spline segment for rendering.
		/// Higher values create smoother curves but use more vertices.
		/// </summary>
		public int SamplesPerSegment { get; set; } = 10;

		/// <summary>
		/// Minimum horizontal distance to exit/enter a port before curving.
		/// </summary>
		public float PortExitDistance { get; set; } = 40f;

		/// <summary>
		/// Edge rendering width in pixels.
		/// </summary>
		public float EdgeWidth { get; set; } = 2f;

		/// <summary>
		/// Whether to use wire-style edges (true) or default Unity edges (false).
		/// </summary>
		[Obsolete("Use EdgeSettings.Instance.RendererType instead. This property is kept for backward compatibility.")]
		public bool UseWireEdges { get; set; } = true;

		/// <summary>
		/// Resets all settings to their default values.
		/// </summary>
		public void ResetToDefaults()
		{
			ControlPointSpacing = 150f;
			BackwardLoopRadius = 50f;
			BackwardLoopOffset = 80f;
			SplineTension = 0.5f;
			SamplesPerSegment = 10;
			PortExitDistance = 40f;
			EdgeWidth = 2f;
#pragma warning disable CS0618 // Obsolete - kept for backward compatibility
			UseWireEdges = true;
#pragma warning restore CS0618
		}

		/// <summary>
		/// Saves settings to EditorPrefs for persistence.
		/// </summary>
		public void Save()
		{
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_ControlPointSpacing", ControlPointSpacing);
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_BackwardLoopRadius", BackwardLoopRadius);
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_BackwardLoopOffset", BackwardLoopOffset);
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_SplineTension", SplineTension);
			UnityEditor.EditorPrefs.SetInt("VSM_WireEdge_SamplesPerSegment", SamplesPerSegment);
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_PortExitDistance", PortExitDistance);
			UnityEditor.EditorPrefs.SetFloat("VSM_WireEdge_EdgeWidth", EdgeWidth);
#pragma warning disable CS0618 // Obsolete - kept for backward compatibility
			UnityEditor.EditorPrefs.SetBool("VSM_WireEdge_UseWireEdges", UseWireEdges);
#pragma warning restore CS0618
		}

		/// <summary>
		/// Loads settings from EditorPrefs.
		/// </summary>
		public void Load()
		{
			ControlPointSpacing = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_ControlPointSpacing", 150f);
			BackwardLoopRadius = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_BackwardLoopRadius", 50f);
			BackwardLoopOffset = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_BackwardLoopOffset", 80f);
			SplineTension = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_SplineTension", 0.5f);
			SamplesPerSegment = UnityEditor.EditorPrefs.GetInt("VSM_WireEdge_SamplesPerSegment", 10);
			PortExitDistance = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_PortExitDistance", 40f);
			EdgeWidth = UnityEditor.EditorPrefs.GetFloat("VSM_WireEdge_EdgeWidth", 2f);
#pragma warning disable CS0618 // Obsolete - kept for backward compatibility
			UseWireEdges = UnityEditor.EditorPrefs.GetBool("VSM_WireEdge_UseWireEdges", true);
#pragma warning restore CS0618
		}
	}
}

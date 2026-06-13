using System.Collections.Generic;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph.Renderers
{
	/// <summary>
	/// Interface for edge rendering strategies.
	/// Implementations provide both the visual control and settings.
	/// </summary>
	public interface IEdgeRenderer
	{
		/// <summary>
		/// Unique identifier for this renderer type.
		/// </summary>
		EdgeRendererType Type { get; }

		/// <summary>
		/// Display name for UI.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Creates the EdgeControl for rendering this edge style.
		/// </summary>
		EdgeControl CreateEdgeControl();

		/// <summary>
		/// Gets the settings specific to this renderer (can be null if no custom settings).
		/// </summary>
		IEdgeRendererSettings Settings { get; }

		/// <summary>
		/// Whether this renderer supports curve customization (control points, tangents).
		/// </summary>
		bool SupportsCurveCustomization { get; }

		/// <summary>
		/// Applies connection data to the edge control (curve offsets, tangents, etc.).
		/// </summary>
		void ApplyConnectionData(EdgeControl control, Connection connectionData);

		/// <summary>
		/// Gets curve points for hit testing. Returns empty if not applicable.
		/// </summary>
		IReadOnlyList<Vector2> GetCurvePoints(EdgeControl control);
	}
}

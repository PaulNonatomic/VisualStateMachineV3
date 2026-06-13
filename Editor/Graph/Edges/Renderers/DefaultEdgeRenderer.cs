using System;
using System.Collections.Generic;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph.Renderers
{
	/// <summary>
	/// Unity's default edge renderer.
	/// Uses the built-in EdgeControl for simple bezier curves.
	/// </summary>
	public class DefaultEdgeRenderer : IEdgeRenderer
	{
		/// <inheritdoc/>
		public EdgeRendererType Type => EdgeRendererType.Default;

		/// <inheritdoc/>
		public string DisplayName => "Default (Unity)";

		/// <inheritdoc/>
		public bool SupportsCurveCustomization => false;

		/// <inheritdoc/>
		public IEdgeRendererSettings Settings => null;

		/// <inheritdoc/>
		public EdgeControl CreateEdgeControl()
		{
			return new EdgeControl();
		}

		/// <inheritdoc/>
		public void ApplyConnectionData(EdgeControl control, Connection connectionData)
		{
			// No curve customization supported for default renderer
		}

		/// <inheritdoc/>
		public IReadOnlyList<Vector2> GetCurvePoints(EdgeControl control)
		{
			return Array.Empty<Vector2>();
		}
	}
}

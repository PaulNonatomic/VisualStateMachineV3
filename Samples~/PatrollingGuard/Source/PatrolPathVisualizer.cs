using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	/// <summary>
	/// Draws the patrol path in the game view using layered LineRenderers
	/// with additive blending for a holographic glow effect.
	/// </summary>
	public class PatrolPathVisualizer : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private GuardController _guardController;

		[Header("Path Display")]
		[SerializeField] private bool _showPath = true;
		[SerializeField] private Color _glowColor = new Color(0f, 0.8f, 1f, 0.08f);
		[SerializeField] private float _glowWidth = 0.6f;
		[SerializeField] private Color _coreColor = new Color(0f, 0.9f, 1f, 0.25f);
		[SerializeField] private float _coreWidth = 0.12f;
		[SerializeField] private float _heightOffset = 0.05f;

		private LineRenderer _glowRenderer;
		private LineRenderer _coreRenderer;

		private void Awake()
		{
			var glowMat = CreateAdditiveMaterial();
			var coreMat = CreateAdditiveMaterial();

			_glowRenderer = CreateLineRenderer("PatrolGlow", glowMat, _glowColor, _glowWidth);
			_coreRenderer = CreateLineRenderer("PatrolCore", coreMat, _coreColor, _coreWidth);
		}

		private void LateUpdate()
		{
			UpdatePathRenderers();
		}

		private void OnDestroy()
		{
			DestroyRendererMaterial(_glowRenderer);
			DestroyRendererMaterial(_coreRenderer);
		}

		private void OnValidate()
		{
			ApplyStyle(_glowRenderer, _glowColor, _glowWidth);
			ApplyStyle(_coreRenderer, _coreColor, _coreWidth);
		}

		private static Material CreateAdditiveMaterial()
		{
			var mat = new Material(Shader.Find("Sprites/Default"));
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
			return mat;
		}

		private LineRenderer CreateLineRenderer(string objectName, Material mat, Color color, float width)
		{
			var go = new GameObject(objectName);
			go.transform.SetParent(transform);

			var lr = go.AddComponent<LineRenderer>();
			lr.useWorldSpace = true;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.allowOcclusionWhenDynamic = false;
			lr.material = mat;
			lr.numCornerVertices = 4;
			lr.numCapVertices = 4;

			ApplyStyle(lr, color, width);

			return lr;
		}

		private static void ApplyStyle(LineRenderer lr, Color color, float width)
		{
			if (lr == null) return;

			lr.startWidth = width;
			lr.endWidth = width;
			lr.startColor = color;
			lr.endColor = color;

			if (lr.material != null)
			{
				lr.material.color = color;
			}
		}

		private static void DestroyRendererMaterial(LineRenderer lr)
		{
			if (lr != null && lr.material != null) Destroy(lr.material);
		}

		private void UpdatePathRenderers()
		{
			if (_guardController == null) return;

			var enabled = _showPath;
			if (_glowRenderer != null) _glowRenderer.enabled = enabled;
			if (_coreRenderer != null) _coreRenderer.enabled = enabled;

			if (!enabled) return;

			var waypoints = _guardController.Waypoints;
			if (waypoints == null || waypoints.Count < 2)
			{
				SetPositionCount(_glowRenderer, 0);
				SetPositionCount(_coreRenderer, 0);
				return;
			}

			var count = waypoints.Count + 1;
			SetPositionCount(_glowRenderer, count);
			SetPositionCount(_coreRenderer, count);

			var offset = Vector3.up * _heightOffset;

			for (var i = 0; i < waypoints.Count; i++)
			{
				var pos = waypoints[i] != null ? waypoints[i].position + offset : Vector3.zero;
				SetPosition(_glowRenderer, i, pos);
				SetPosition(_coreRenderer, i, pos);
			}

			// Close the loop
			var firstPos = waypoints[0] != null ? waypoints[0].position + offset : Vector3.zero;
			SetPosition(_glowRenderer, waypoints.Count, firstPos);
			SetPosition(_coreRenderer, waypoints.Count, firstPos);
		}

		private static void SetPositionCount(LineRenderer lr, int count)
		{
			if (lr != null) lr.positionCount = count;
		}

		private static void SetPosition(LineRenderer lr, int index, Vector3 pos)
		{
			if (lr != null) lr.SetPosition(index, pos);
		}
	}
}

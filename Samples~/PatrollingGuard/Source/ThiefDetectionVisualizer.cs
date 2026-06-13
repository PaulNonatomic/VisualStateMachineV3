using Nonatomic.VSM.Core;
using Nonatomic.VSM.Samples.PatrollingGuard.States;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	/// <summary>
	/// Renders the thief's awareness and danger detection areas in the game view
	/// using a filled mesh with additive blending and LineRenderer outlines.
	/// Switches between a wedge (wander) and a circle (flee) based on state.
	/// </summary>
	public class ThiefDetectionVisualizer : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private ThiefController _thiefController;
		[SerializeField] private StateMachineRunner _stateMachineRunner;

		[Header("Awareness Fill")]
		[SerializeField] private Color _awarenessCenterColor = new Color(1f, 0.9f, 0.2f, 0.07f);
		[SerializeField] private Color _awarenessEdgeColor = new Color(1f, 0.9f, 0.2f, 0f);

		[Header("Danger Fill")]
		[SerializeField] private Color _dangerCenterColor = new Color(1f, 0.2f, 0.1f, 0.05f);
		[SerializeField] private Color _dangerEdgeColor = new Color(1f, 0.2f, 0.1f, 0f);

		[Header("Outline")]
		[SerializeField] private bool _showOutline = true;
		[SerializeField] private Color _awarenessOutlineColor = new Color(1f, 0.9f, 0.2f, 0.12f);
		[SerializeField] private Color _dangerOutlineColor = new Color(1f, 0.3f, 0.1f, 0.25f);
		[SerializeField] private float _outlineWidth = 0.08f;

		[Header("Settings")]
		[SerializeField] private float _heightOffset = 0.05f;
		[SerializeField] private int _segments = 30;

		private Mesh _mesh;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;
		private Material _meshMaterial;
		private LineRenderer _outline;
		private Material _outlineMaterial;

		private bool _lastIsFleeing;
		private float _lastRange;
		private float _lastAngle;

		private void Awake()
		{
			CreateMesh();
			CreateOutline();
		}

		private void LateUpdate()
		{
			if (_thiefController == null) return;

			var targetTransform = _thiefController.Transform;
			var offset = Vector3.up * _heightOffset;

			if (_meshFilter != null)
			{
				_meshFilter.transform.position = targetTransform.position + offset;
				_meshFilter.transform.rotation = targetTransform.rotation;
			}

			var isFleeing = IsFleeing();
			RebuildIfChanged(isFleeing);
			UpdateOutline(isFleeing);
		}

		private void OnDestroy()
		{
			if (_mesh != null) Destroy(_mesh);
			if (_meshMaterial != null) Destroy(_meshMaterial);
			if (_outlineMaterial != null) Destroy(_outlineMaterial);
		}

		private void OnValidate()
		{
			if (_outline != null)
			{
				_outline.enabled = _showOutline;
				_outline.startWidth = _outlineWidth;
				_outline.endWidth = _outlineWidth;
			}
		}

		private bool IsFleeing()
		{
			if (_stateMachineRunner == null) return false;
			return _stateMachineRunner.CurrentState is FleeState;
		}

		private void CreateMesh()
		{
			var go = new GameObject("DetectionArea");
			go.transform.SetParent(transform);

			_meshFilter = go.AddComponent<MeshFilter>();
			_meshRenderer = go.AddComponent<MeshRenderer>();

			_meshMaterial = CreateAdditiveMaterial();
			_meshRenderer.material = _meshMaterial;
			_meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_meshRenderer.receiveShadows = false;

			_mesh = new Mesh { name = "ThiefDetectionArea" };
			_meshFilter.mesh = _mesh;

			BuildMesh(false);
		}

		private void CreateOutline()
		{
			var go = new GameObject("DetectionOutline");
			go.transform.SetParent(transform);

			_outline = go.AddComponent<LineRenderer>();
			_outline.useWorldSpace = true;
			_outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_outline.receiveShadows = false;
			_outline.allowOcclusionWhenDynamic = false;
			_outline.numCornerVertices = 2;
			_outline.numCapVertices = 2;

			_outlineMaterial = CreateAdditiveMaterial();
			_outline.material = _outlineMaterial;

			_outline.startWidth = _outlineWidth;
			_outline.endWidth = _outlineWidth;
			_outline.enabled = _showOutline;
		}

		private void BuildMesh(bool isFleeing)
		{
			if (_thiefController == null || _mesh == null) return;

			float range;
			float halfAngle;
			Color centerColor;
			Color edgeColor;

			if (isFleeing)
			{
				range = _thiefController.DangerRange;
				halfAngle = 180f;
				centerColor = _dangerCenterColor;
				edgeColor = _dangerEdgeColor;
			}
			else
			{
				range = _thiefController.AwarenessRange;
				halfAngle = _thiefController.AwarenessAngle;
				centerColor = _awarenessCenterColor;
				edgeColor = _awarenessEdgeColor;
			}

			_lastRange = range;
			_lastAngle = halfAngle;
			_lastIsFleeing = isFleeing;

			var vertCount = _segments + 2;
			var vertices = new Vector3[vertCount];
			var colors = new Color[vertCount];
			var triangles = new int[_segments * 3];

			vertices[0] = Vector3.zero;
			colors[0] = centerColor;

			var angleStep = (halfAngle * 2f) / _segments;
			for (var i = 0; i <= _segments; i++)
			{
				var angle = -halfAngle + angleStep * i;
				var rad = angle * Mathf.Deg2Rad;
				vertices[i + 1] = new Vector3(Mathf.Sin(rad) * range, 0f, Mathf.Cos(rad) * range);
				colors[i + 1] = edgeColor;
			}

			for (var i = 0; i < _segments; i++)
			{
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}

			_mesh.Clear();
			_mesh.vertices = vertices;
			_mesh.colors = colors;
			_mesh.triangles = triangles;
			_mesh.RecalculateNormals();
		}

		private void RebuildIfChanged(bool isFleeing)
		{
			if (_thiefController == null) return;

			var range = isFleeing ? _thiefController.DangerRange : _thiefController.AwarenessRange;
			var angle = isFleeing ? 180f : _thiefController.AwarenessAngle;

			if (isFleeing == _lastIsFleeing
				&& Mathf.Approximately(range, _lastRange)
				&& Mathf.Approximately(angle, _lastAngle))
			{
				return;
			}

			BuildMesh(isFleeing);
		}

		private void UpdateOutline(bool isFleeing)
		{
			if (_outline == null || _thiefController == null) return;

			_outline.enabled = _showOutline;
			if (!_showOutline) return;

			var targetTransform = _thiefController.Transform;
			var origin = targetTransform.position + Vector3.up * _heightOffset;

			var outlineColor = isFleeing ? _dangerOutlineColor : _awarenessOutlineColor;
			_outline.startColor = outlineColor;
			_outline.endColor = outlineColor;
			if (_outlineMaterial != null) _outlineMaterial.color = outlineColor;

			if (isFleeing)
			{
				BuildCircleOutline(origin, _thiefController.DangerRange);
			}
			else
			{
				BuildWedgeOutline(origin, targetTransform, _thiefController.AwarenessRange, _thiefController.AwarenessAngle);
			}
		}

		private void BuildCircleOutline(Vector3 origin, float radius)
		{
			_outline.positionCount = _segments + 1;
			_outline.loop = true;

			var angleStep = 360f / _segments;
			for (var i = 0; i <= _segments; i++)
			{
				var dir = Quaternion.Euler(0, angleStep * i, 0) * Vector3.forward;
				_outline.SetPosition(i, origin + dir * radius);
			}
		}

		private void BuildWedgeOutline(Vector3 origin, Transform targetTransform, float range, float halfAngle)
		{
			var pointCount = _segments + 3;
			_outline.positionCount = pointCount;
			_outline.loop = false;

			_outline.SetPosition(0, origin);

			var angleStep = (halfAngle * 2f) / _segments;
			for (var i = 0; i <= _segments; i++)
			{
				var angle = -halfAngle + angleStep * i;
				var dir = Quaternion.Euler(0, angle, 0) * targetTransform.forward;
				_outline.SetPosition(i + 1, origin + dir * range);
			}

			_outline.SetPosition(pointCount - 1, origin);
		}

		private static Material CreateAdditiveMaterial()
		{
			var mat = new Material(Shader.Find("Sprites/Default"));
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
			return mat;
		}
	}
}

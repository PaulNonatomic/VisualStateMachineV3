using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Represents a connection between two ports in the graph.
	/// </summary>
	[Serializable]
	public class Connection
	{
		[SerializeField] private string _id;
		[SerializeField] private string _sourceNodeId;
		[SerializeField] private string _sourcePortName;
		[SerializeField] private string _targetNodeId;
		[SerializeField] private string _targetPortName;
		[SerializeField] private ConnectionType _connectionType;
		[SerializeField] private float _transitionTime;
		[SerializeField] private AnimationCurve _easingCurve;
		[SerializeField] private int _subdivisionLevel;

		// Level 1 control points (quarter, mid, three-quarter)
		[SerializeField] private Vector2 _quarterPointOffset;
		[SerializeField] private bool _hasQuarterPointOffset;
		[SerializeField] private Vector2 _quarterPointTangent;
		[SerializeField] private bool _hasQuarterPointTangent;

		[SerializeField] private Vector2 _midpointOffset;
		[SerializeField] private bool _hasMidpointOffset;
		[SerializeField] private Vector2 _midpointTangent;
		[SerializeField] private bool _hasMidpointTangent;

		[SerializeField] private Vector2 _threeQuarterPointOffset;
		[SerializeField] private bool _hasThreeQuarterPointOffset;
		[SerializeField] private Vector2 _threeQuarterPointTangent;
		[SerializeField] private bool _hasThreeQuarterPointTangent;

		// Level 2 subdivision points (1/8, 3/8, 5/8, 7/8)
		[SerializeField] private Vector2 _eighthPointOffset;
		[SerializeField] private bool _hasEighthPointOffset;
		[SerializeField] private Vector2 _eighthPointTangent;
		[SerializeField] private bool _hasEighthPointTangent;

		[SerializeField] private Vector2 _threeEighthsPointOffset;
		[SerializeField] private bool _hasThreeEighthsPointOffset;
		[SerializeField] private Vector2 _threeEighthsPointTangent;
		[SerializeField] private bool _hasThreeEighthsPointTangent;

		[SerializeField] private Vector2 _fiveEighthsPointOffset;
		[SerializeField] private bool _hasFiveEighthsPointOffset;
		[SerializeField] private Vector2 _fiveEighthsPointTangent;
		[SerializeField] private bool _hasFiveEighthsPointTangent;

		[SerializeField] private Vector2 _sevenEighthsPointOffset;
		[SerializeField] private bool _hasSevenEighthsPointOffset;
		[SerializeField] private Vector2 _sevenEighthsPointTangent;
		[SerializeField] private bool _hasSevenEighthsPointTangent;

		/// <summary>
		/// Unique identifier for this connection.
		/// </summary>
		public string Id
		{
			get => _id;
			set => _id = value;
		}

		/// <summary>
		/// ID of the source node.
		/// </summary>
		public string SourceNodeId
		{
			get => _sourceNodeId;
			set => _sourceNodeId = value;
		}

		/// <summary>
		/// Name of the source port (field name on the source node).
		/// </summary>
		public string SourcePortName
		{
			get => _sourcePortName;
			set => _sourcePortName = value;
		}

		/// <summary>
		/// ID of the target node.
		/// </summary>
		public string TargetNodeId
		{
			get => _targetNodeId;
			set => _targetNodeId = value;
		}

		/// <summary>
		/// Name of the target port (field name on the target node).
		/// </summary>
		public string TargetPortName
		{
			get => _targetPortName;
			set => _targetPortName = value;
		}

		/// <summary>
		/// Type of this connection.
		/// </summary>
		public ConnectionType ConnectionType
		{
			get => _connectionType;
			set => _connectionType = value;
		}

		/// <summary>
		/// Time delay for transitions (0 = instant).
		/// </summary>
		public float TransitionTime
		{
			get => _transitionTime;
			set => _transitionTime = value;
		}

		/// <summary>
		/// Easing curve for timed transitions.
		/// </summary>
		public AnimationCurve EasingCurve
		{
			get => _easingCurve;
			set => _easingCurve = value;
		}

		/// <summary>
		/// Whether this is an instant (same-frame) transition.
		/// </summary>
		public bool IsInstant => _transitionTime <= 0f;

		/// <summary>
		/// Quarter point offset for bending the bezier curve (t=0.25).
		/// </summary>
		public Vector2? QuarterPointOffset
		{
			get => _hasQuarterPointOffset ? _quarterPointOffset : null;
			set
			{
				_hasQuarterPointOffset = value.HasValue;
				_quarterPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the quarter point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? QuarterPointTangent
		{
			get => _hasQuarterPointTangent ? _quarterPointTangent : null;
			set
			{
				_hasQuarterPointTangent = value.HasValue;
				_quarterPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Midpoint offset for bending the bezier curve (t=0.5).
		/// </summary>
		public Vector2? MidpointOffset
		{
			get => _hasMidpointOffset ? _midpointOffset : null;
			set
			{
				_hasMidpointOffset = value.HasValue;
				_midpointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the midpoint (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? MidpointTangent
		{
			get => _hasMidpointTangent ? _midpointTangent : null;
			set
			{
				_hasMidpointTangent = value.HasValue;
				_midpointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Three-quarter point offset for bending the bezier curve (t=0.75).
		/// </summary>
		public Vector2? ThreeQuarterPointOffset
		{
			get => _hasThreeQuarterPointOffset ? _threeQuarterPointOffset : null;
			set
			{
				_hasThreeQuarterPointOffset = value.HasValue;
				_threeQuarterPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the three-quarter point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? ThreeQuarterPointTangent
		{
			get => _hasThreeQuarterPointTangent ? _threeQuarterPointTangent : null;
			set
			{
				_hasThreeQuarterPointTangent = value.HasValue;
				_threeQuarterPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Eighth point offset for bending the bezier curve (t=0.125).
		/// </summary>
		public Vector2? EighthPointOffset
		{
			get => _hasEighthPointOffset ? _eighthPointOffset : null;
			set
			{
				_hasEighthPointOffset = value.HasValue;
				_eighthPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the eighth point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? EighthPointTangent
		{
			get => _hasEighthPointTangent ? _eighthPointTangent : null;
			set
			{
				_hasEighthPointTangent = value.HasValue;
				_eighthPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Three-eighths point offset for bending the bezier curve (t=0.375).
		/// </summary>
		public Vector2? ThreeEighthsPointOffset
		{
			get => _hasThreeEighthsPointOffset ? _threeEighthsPointOffset : null;
			set
			{
				_hasThreeEighthsPointOffset = value.HasValue;
				_threeEighthsPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the three-eighths point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? ThreeEighthsPointTangent
		{
			get => _hasThreeEighthsPointTangent ? _threeEighthsPointTangent : null;
			set
			{
				_hasThreeEighthsPointTangent = value.HasValue;
				_threeEighthsPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Five-eighths point offset for bending the bezier curve (t=0.625).
		/// </summary>
		public Vector2? FiveEighthsPointOffset
		{
			get => _hasFiveEighthsPointOffset ? _fiveEighthsPointOffset : null;
			set
			{
				_hasFiveEighthsPointOffset = value.HasValue;
				_fiveEighthsPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the five-eighths point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? FiveEighthsPointTangent
		{
			get => _hasFiveEighthsPointTangent ? _fiveEighthsPointTangent : null;
			set
			{
				_hasFiveEighthsPointTangent = value.HasValue;
				_fiveEighthsPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Seven-eighths point offset for bending the bezier curve (t=0.875).
		/// </summary>
		public Vector2? SevenEighthsPointOffset
		{
			get => _hasSevenEighthsPointOffset ? _sevenEighthsPointOffset : null;
			set
			{
				_hasSevenEighthsPointOffset = value.HasValue;
				_sevenEighthsPointOffset = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Tangent at the seven-eighths point (symmetric, controls curve direction/strength).
		/// </summary>
		public Vector2? SevenEighthsPointTangent
		{
			get => _hasSevenEighthsPointTangent ? _sevenEighthsPointTangent : null;
			set
			{
				_hasSevenEighthsPointTangent = value.HasValue;
				_sevenEighthsPointTangent = value ?? Vector2.zero;
			}
		}

		/// <summary>
		/// Subdivision level for curve control points.
		/// 0 = single midpoint handle only (default)
		/// 1 = quarter + midpoint + three-quarter handles (3 handles)
		/// 2 = eighth points + quarter + three-eighths + mid + five-eighths + three-quarter + seven-eighths (7 handles)
		/// </summary>
		public int SubdivisionLevel
		{
			get => _subdivisionLevel;
			set => _subdivisionLevel = Mathf.Clamp(value, 0, 2);
		}

		/// <summary>
		/// Initializes a new connection with an auto-generated ID and default transition type.
		/// </summary>
		public Connection()
		{
			_id = Guid.NewGuid().ToString();
			_connectionType = ConnectionType.Transition;
		}

		/// <summary>
		/// Initializes a new connection between two ports with an auto-generated ID.
		/// </summary>
		/// <param name="sourceNodeId">ID of the source node.</param>
		/// <param name="sourcePort">Name of the source port.</param>
		/// <param name="targetNodeId">ID of the target node.</param>
		/// <param name="targetPort">Name of the target port.</param>
		/// <param name="type">Connection type (defaults to Transition).</param>
		public Connection(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort, ConnectionType type = ConnectionType.Transition)
		{
			_id = Guid.NewGuid().ToString();
			_sourceNodeId = sourceNodeId;
			_sourcePortName = sourcePort;
			_targetNodeId = targetNodeId;
			_targetPortName = targetPort;
			_connectionType = type;
		}

		/// <summary>
		/// Initializes a new connection between two ports with the specified ID.
		/// </summary>
		/// <param name="id">Connection ID, or null to auto-generate.</param>
		/// <param name="sourceNodeId">ID of the source node.</param>
		/// <param name="sourcePort">Name of the source port.</param>
		/// <param name="targetNodeId">ID of the target node.</param>
		/// <param name="targetPort">Name of the target port.</param>
		/// <param name="type">Connection type (defaults to Transition).</param>
		public Connection(string id, string sourceNodeId, string sourcePort, string targetNodeId, string targetPort, ConnectionType type = ConnectionType.Transition)
		{
			_id = id ?? Guid.NewGuid().ToString();
			_sourceNodeId = sourceNodeId;
			_sourcePortName = sourcePort;
			_targetNodeId = targetNodeId;
			_targetPortName = targetPort;
			_connectionType = type;
		}
	}
}

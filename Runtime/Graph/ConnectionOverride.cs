using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Stores per-connection modifications for a graph variant.
	/// References a connection in the base graph by ID and defines property overrides.
	/// </summary>
	[Serializable]
	public class ConnectionOverride
	{
		[SerializeField] private string _connectionId;
		[SerializeField] private float _transitionTimeOverride;
		[SerializeField] private bool _hasTransitionTimeOverride;
		[SerializeField] private AnimationCurve _easingCurveOverride;
		[SerializeField] private bool _hasEasingCurveOverride;

		public string ConnectionId
		{
			get => _connectionId;
			set => _connectionId = value;
		}

		public float TransitionTimeOverride
		{
			get => _transitionTimeOverride;
			set
			{
				_transitionTimeOverride = value;
				_hasTransitionTimeOverride = true;
			}
		}

		public bool HasTransitionTimeOverride => _hasTransitionTimeOverride;

		public AnimationCurve EasingCurveOverride
		{
			get => _easingCurveOverride;
			set
			{
				_easingCurveOverride = value;
				_hasEasingCurveOverride = true;
			}
		}

		public bool HasEasingCurveOverride => _hasEasingCurveOverride;

		public ConnectionOverride()
		{
		}

		public ConnectionOverride(string connectionId)
		{
			_connectionId = connectionId;
		}

		public void ClearTransitionTimeOverride()
		{
			_transitionTimeOverride = 0f;
			_hasTransitionTimeOverride = false;
		}

		public void ClearEasingCurveOverride()
		{
			_easingCurveOverride = null;
			_hasEasingCurveOverride = false;
		}

		public bool HasAnyOverrides => _hasTransitionTimeOverride || _hasEasingCurveOverride;
	}
}

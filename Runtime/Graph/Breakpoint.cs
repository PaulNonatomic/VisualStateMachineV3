using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	[Serializable]
	public class Breakpoint
	{
		[SerializeField] private string _id;
		[SerializeField] private string _nodeId;
		[SerializeField] private bool _enabled = true;
		[SerializeField] private BreakpointType _type = BreakpointType.OnEntry;
		[SerializeField] private string _condition;
		[SerializeField] private string _transitionPortName;
		[SerializeField] private int _hitCount;

		public Breakpoint()
		{
			_id = Guid.NewGuid().ToString();
		}

		public Breakpoint(string nodeId, BreakpointType type = BreakpointType.OnEntry) : this()
		{
			_nodeId = nodeId;
			_type = type;
		}

		public string Id
		{
			get => _id;
			set => _id = value;
		}

		public string NodeId
		{
			get => _nodeId;
			set => _nodeId = value;
		}

		public bool Enabled
		{
			get => _enabled;
			set => _enabled = value;
		}

		public BreakpointType Type
		{
			get => _type;
			set => _type = value;
		}

		public string Condition
		{
			get => _condition;
			set => _condition = value;
		}

		public string TransitionPortName
		{
			get => _transitionPortName;
			set => _transitionPortName = value;
		}

		public int HitCount
		{
			get => _hitCount;
			set => _hitCount = value;
		}
	}

	public enum BreakpointType
	{
		OnEntry,
		OnTransition,
		Conditional
	}
}

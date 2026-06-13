using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// Shared context for the world-space graph sample. The state machine reads and
	/// mutates fields here to drive the agent's behaviour, while the
	/// <c>RuntimeGraphView</c> floating above it visualises the active state.
	/// </summary>
	public class CreatureController : MonoBehaviour, IStateMachineContext
	{
		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		[SerializeField]
		private Transform _body;

		[SerializeField]
		private float _wanderRadius = 4f;

		[SerializeField]
		private float _moveSpeed = 1.5f;

		public Transform Body
		{
			get => _body != null ? _body : transform;
			set => _body = value;
		}
		public float WanderRadius => _wanderRadius;
		public float MoveSpeed => _moveSpeed;

		public Vector3 Origin { get; private set; }
		public Vector3 Target { get; set; }
		public bool Poked { get; set; }

		private void Awake()
		{
			Origin = transform.position;
			Target = Origin;
		}

		public void Poke()
		{
			Poked = true;
		}

		public Vector3 PickWanderPoint()
		{
			var offset = Random.insideUnitCircle * _wanderRadius;
			return Origin + new Vector3(offset.x, 0f, offset.y);
		}

		public void StepTowardTarget()
		{
			var body = Body;
			body.position = Vector3.MoveTowards(body.position, Target, _moveSpeed * Time.deltaTime);
		}

		public bool ReachedTarget(float tolerance = 0.1f)
		{
			return Vector3.Distance(Body.position, Target) <= tolerance;
		}
	}
}

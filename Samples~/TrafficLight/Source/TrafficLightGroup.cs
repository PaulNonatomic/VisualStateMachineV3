using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight
{
	/// <summary>
	/// Groups multiple TrafficLightControllers and drives them in sync.
	/// This is the state machine context — place it alongside a StateMachineRunner
	/// and assign all the controllers for one direction of traffic.
	/// </summary>
	[BlackboardType]
	public class TrafficLightGroup : MonoBehaviour, IStateMachineContext
	{
		[SerializeField] private TrafficLightController[] _controllers;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		public void TurnAllOff()
		{
			foreach (var controller in _controllers) controller.TurnAllOff();
		}

		public void SetRed()
		{
			foreach (var controller in _controllers) controller.SetRed();
		}

		public void SetYellow()
		{
			foreach (var controller in _controllers) controller.SetYellow();
		}

		public void SetGreen()
		{
			foreach (var controller in _controllers) controller.SetGreen();
		}

		public void SetRedYellow()
		{
			foreach (var controller in _controllers) controller.SetRedYellow();
		}

		public void SetGreenLight(bool on)
		{
			foreach (var controller in _controllers) controller.SetGreenLight(on);
		}
	}
}

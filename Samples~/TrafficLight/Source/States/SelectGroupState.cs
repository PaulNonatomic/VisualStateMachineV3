using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.TrafficLight.States
{
	/// <summary>
	/// Reads the Direction, resolves the corresponding TrafficLightGroup,
	/// and outputs it as ActiveGroup so downstream states can use it.
	/// Also outputs the toggled direction for the next loop iteration.
	/// </summary>
	[StateInfo(
		"Select traffic light group",
		"Reads Direction, resolves the matching group, outputs ActiveGroup and the toggled direction.")]
	[StateStyle(Color = StateColors.LightBlue, Icon = NodeIcon.Share)]
	public class SelectGroupState : State
	{
		[DataIn("NS Group")]
		public TrafficLightGroup NSGroup;

		[DataIn("EW Group")]
		public TrafficLightGroup EWGroup;

		[DataIn("Direction")]
		public TrafficDirection Direction;

		[DataOut("Active Group")]
		public TrafficLightGroup ActiveGroup;

		[DataOut("Next Direction")]
		public TrafficDirection NextDirection;

		[Transition("Next")]
		public Action OnNext;

		public override void OnEnter()
		{
			ActiveGroup = Direction switch
			{
				TrafficDirection.NorthSouth => NSGroup,
				TrafficDirection.EastWest => EWGroup,
				_ => null
			};

			NextDirection = Direction == TrafficDirection.EastWest
				? TrafficDirection.NorthSouth
				: TrafficDirection.EastWest;

			OnNext?.Invoke();
		}
	}
}

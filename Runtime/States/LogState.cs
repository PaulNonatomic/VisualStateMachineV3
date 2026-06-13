using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that logs a message to the console and immediately transitions.
	/// Useful for debugging state machine flow.
	/// </summary>
	[StateInfo(
		"Logs a message to the console",
		"Logs a configurable message at the specified log level (Log, Warning, or Error) " +
		"and immediately transitions to the next state. Useful for debugging and tracing " +
		"state machine execution flow."
	)]
	[StateStyle(Color = StateColors.Gray, Icon = NodeIcon.Note)]
	public class LogState : State
	{
		/// <summary>
		/// The message to log.
		/// </summary>
		[DataIn("Message")]
		public string Message = "State reached";

		/// <summary>
		/// The log level to use.
		/// </summary>
		[DataIn("Level")]
		public LogLevel Level = LogLevel.Log;

		/// <summary>
		/// Whether to include the GameObject name in the message.
		/// </summary>
		[DataIn("Include Object Name")]
		public bool IncludeObjectName = true;

		/// <summary>
		/// Transition that fires after logging.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var message = IncludeObjectName && GameObject != null
				? $"[{GameObject.name}] {Message}"
				: Message;

			switch (Level)
			{
				case LogLevel.Log:
					Debug.Log(message, GameObject);
					break;
				case LogLevel.Warning:
					Debug.LogWarning(message, GameObject);
					break;
				case LogLevel.Error:
					Debug.LogError(message, GameObject);
					break;
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Log level for the LogState.
	/// </summary>
	public enum LogLevel
	{
		Log,
		Warning,
		Error
	}
}

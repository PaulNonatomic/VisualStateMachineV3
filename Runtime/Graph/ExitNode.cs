using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Exit point node for the state machine.
	/// Can have multiple exit nodes for different exit conditions.
	/// </summary>
	[Serializable]
	public class ExitNode : GraphNode
	{
		[SerializeField] private string _exitName;

		[SerializeField] private int _exitCode;

		/// <summary>
		/// Name identifying this exit (e.g., "Success", "Failure").
		/// </summary>
		public string ExitName
		{
			get => _exitName;
			set => _exitName = value;
		}

		/// <summary>
		/// Optional exit code for programmatic handling.
		/// </summary>
		public int ExitCode
		{
			get => _exitCode;
			set => _exitCode = value;
		}

		public override GraphNodeType NodeType => GraphNodeType.Exit;

		/// <summary>
		/// Initializes a new exit node with an auto-generated ID.
		/// </summary>
		public ExitNode() : base()
		{
			DisplayName = "Exit";
			_exitName = "Exit";
		}

		/// <summary>
		/// Initializes a new exit node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public ExitNode(string id) : base(id)
		{
			DisplayName = "Exit";
			_exitName = "Exit";
		}

		/// <summary>
		/// Initializes a new exit node with a named exit condition.
		/// </summary>
		/// <param name="exitName">Name identifying this exit condition.</param>
		/// <param name="exitCode">Optional exit code for programmatic handling.</param>
		public ExitNode(string exitName, int exitCode) : base()
		{
			_exitName = exitName;
			_exitCode = exitCode;
			DisplayName = exitName;
		}
	}
}

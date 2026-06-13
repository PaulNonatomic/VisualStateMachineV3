using System;
using System.Collections.Generic;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nonatomic.VSM.VisualScripting
{
	/// <summary>
	/// State that executes a Unity Visual Scripting graph as its body.
	/// Designers can build state logic visually without writing C#.
	/// The graph receives StateEnter, StateUpdate, and StateExit custom events.
	/// </summary>
	[StateInfo(
		"Runs a Visual Scripting graph",
		"Executes a ScriptGraphAsset as the state body. The graph receives " +
		"StateEnter, StateUpdate, and StateExit custom events. Blackboard " +
		"variables are synced to graph object variables on enter and back on exit. " +
		"Use the CompleteState unit in the graph to trigger the Done transition."
	)]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.Command)]
	public class VisualScriptingState : State
	{
		/// <summary>
		/// The Visual Scripting graph to execute as this state's body.
		/// </summary>
		[DataIn("Graph", Required = true)]
		public ScriptGraphAsset Graph;

		/// <summary>
		/// Whether to sync blackboard variables to/from the graph's object variables.
		/// Variables are synced to the graph on enter and back to the blackboard on exit.
		/// </summary>
		[DataIn("Sync Blackboard")]
		public bool SyncBlackboard = true;

		/// <summary>
		/// Transition triggered by the CompleteState unit in the graph.
		/// </summary>
		[Transition("Done")]
		public Action OnDone;

		private GameObject _runner;
		private ScriptMachine _machine;
		private bool _enterPending;
		private List<string> _syncedKeys;

		public override void OnEnter()
		{
			if (Graph == null)
			{
				LogWarning("No script graph assigned");
				OnDone?.Invoke();
				return;
			}

			CreateGraphRunner();

			if (SyncBlackboard && Blackboard != null)
			{
				SyncBlackboardToGraph();
			}

			StoreStateReference();
			_enterPending = true;
		}

		public override void OnUpdate()
		{
			if (_runner == null) return;

			if (_enterPending)
			{
				_enterPending = false;
				CustomEvent.Trigger(_runner, "StateEnter");
				return;
			}

			CustomEvent.Trigger(_runner, "StateUpdate");
		}

		public override void OnExit()
		{
			if (_runner == null) return;

			CustomEvent.Trigger(_runner, "StateExit");

			if (SyncBlackboard && Blackboard != null)
			{
				SyncGraphToBlackboard();
			}

			Object.Destroy(_runner);
			_runner = null;
			_machine = null;
			_enterPending = false;
		}

		/// <summary>
		/// Called by the CompleteState unit to trigger the Done transition.
		/// </summary>
		internal void CompleteState()
		{
			OnDone?.Invoke();
		}

		private void CreateGraphRunner()
		{
			_runner = new GameObject("[VSM] Graph Runner");

			if (Transform != null)
			{
				_runner.transform.SetParent(Transform, false);
			}

			_machine = _runner.AddComponent<ScriptMachine>();
			_machine.nest.source = GraphSource.Macro;
			_machine.nest.macro = Graph;
		}

		private void StoreStateReference()
		{
			Variables.Object(_runner).Set("_vsmState", this);
		}

		private void SyncBlackboardToGraph()
		{
			var vars = Variables.Object(_runner);
			_syncedKeys = new List<string>();

			foreach (var key in Blackboard.Keys)
			{
				vars.Set(key, Blackboard.GetBoxed(key));
				_syncedKeys.Add(key);
			}
		}

		private void SyncGraphToBlackboard()
		{
			if (_syncedKeys == null) return;

			var vars = Variables.Object(_runner);

			foreach (var key in _syncedKeys)
			{
				if (!vars.IsDefined(key)) continue;

				var value = vars.Get(key);
				Blackboard.SetBoxed(key, value, Blackboard.GetVariableType(key));
			}

			_syncedKeys = null;
		}
	}
}

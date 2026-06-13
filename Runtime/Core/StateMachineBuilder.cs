using System;
using System.Collections.Generic;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Reference to a state in the builder, used for type-safe transitions.
	/// </summary>
	public class StateRef
	{
		internal string Key { get; }
		internal StateMachineBuilder Builder { get; }

		internal StateRef(string key, StateMachineBuilder builder)
		{
			Key = key;
			Builder = builder;
		}

		/// <summary>
		/// Adds a transition from this state to another.
		/// </summary>
		/// <param name="transitionName">The transition port name.</param>
		/// <param name="target">The target state reference.</param>
		/// <param name="transitionTime">Optional delay before transition.</param>
		public StateRef TransitionTo(string transitionName, StateRef target, float transitionTime = 0f)
		{
			Builder.AddTransition(Key, transitionName, target.Key, transitionTime);
			return this;
		}

		/// <summary>
		/// Adds a transition from this state to another state by key.
		/// </summary>
		/// <param name="transitionName">The transition port name.</param>
		/// <param name="targetKey">The target state key.</param>
		/// <param name="transitionTime">Optional delay before transition.</param>
		public StateRef TransitionTo(string transitionName, string targetKey, float transitionTime = 0f)
		{
			Builder.AddTransition(Key, transitionName, targetKey, transitionTime);
			return this;
		}

		/// <summary>
		/// Adds a transition from this state to another state type.
		/// </summary>
		/// <typeparam name="TTarget">The target state type.</typeparam>
		/// <param name="transitionName">The transition port name.</param>
		/// <param name="transitionTime">Optional delay before transition.</param>
		public StateRef TransitionTo<TTarget>(string transitionName, float transitionTime = 0f) where TTarget : State
		{
			Builder.AddTransition(Key, transitionName, typeof(TTarget).Name, transitionTime);
			return this;
		}

		/// <summary>
		/// Sets the position of this state node in the graph editor.
		/// </summary>
		/// <param name="x">X position.</param>
		/// <param name="y">Y position.</param>
		public StateRef AtPosition(float x, float y)
		{
			Builder.SetNodePosition(Key, new Vector2(x, y));
			return this;
		}

		/// <summary>
		/// Sets the position of this state node in the graph editor.
		/// </summary>
		/// <param name="position">The position vector.</param>
		public StateRef AtPosition(Vector2 position)
		{
			Builder.SetNodePosition(Key, position);
			return this;
		}

		public static implicit operator string(StateRef stateRef) => stateRef.Key;
	}

	/// <summary>
	/// Layout algorithm options for auto-arranging nodes.
	/// </summary>
	public enum GraphLayoutMode
	{
		/// <summary>
		/// No automatic layout - use manual positions.
		/// </summary>
		None,

		/// <summary>
		/// Hierarchical layout from left to right based on connections.
		/// </summary>
		Hierarchical,

		/// <summary>
		/// Simple grid layout.
		/// </summary>
		Grid
	}

	/// <summary>
	/// Fluent builder for creating state machines in code without the visual editor.
	/// </summary>
	public class StateMachineBuilder
	{
		private readonly GameObject _gameObject;
		private readonly StateMachineGraph _graph;
		private readonly Dictionary<string, StateNode> _statesByKey = new Dictionary<string, StateNode>();
		private readonly Dictionary<string, StateRef> _stateRefs = new Dictionary<string, StateRef>();
		private readonly EntryNode _entryNode;
		private string _entryStateKey;
		private IStateMachineContext _context;
		private GraphLayoutMode _layoutMode = GraphLayoutMode.Hierarchical;
		private Action<StateMachineGraph> _layoutAction;

		private StateMachineBuilder(GameObject gameObject)
		{
			_gameObject = gameObject;
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_entryNode = new EntryNode();
			_graph.AddNode(_entryNode);
		}

		/// <summary>
		/// Creates a new state machine builder.
		/// </summary>
		/// <param name="gameObject">The GameObject context for the state machine.</param>
		public static StateMachineBuilder Create(GameObject gameObject)
		{
			return new StateMachineBuilder(gameObject);
		}

		/// <summary>
		/// Creates a new state machine builder without a GameObject context.
		/// </summary>
		public static StateMachineBuilder Create()
		{
			return new StateMachineBuilder(null);
		}

		/// <summary>
		/// Adds a state to the state machine and returns a reference for chaining transitions.
		/// </summary>
		/// <typeparam name="T">The State class type.</typeparam>
		/// <param name="key">Optional key to identify this state. Defaults to the type name.</param>
		public StateRef AddState<T>(string key = null) where T : State
		{
			return AddState(typeof(T), key);
		}

		/// <summary>
		/// Adds a state to the state machine and returns a reference for chaining transitions.
		/// </summary>
		/// <param name="stateType">The State class type.</param>
		/// <param name="key">Optional key to identify this state. Defaults to the type name.</param>
		public StateRef AddState(Type stateType, string key = null)
		{
			if (!typeof(State).IsAssignableFrom(stateType))
			{
				throw new ArgumentException($"Type {stateType.Name} must inherit from State", nameof(stateType));
			}

			key ??= stateType.Name;

			if (_statesByKey.ContainsKey(key))
			{
				throw new ArgumentException($"A state with key '{key}' already exists", nameof(key));
			}

			var stateNode = new StateNode(stateType);
			_statesByKey[key] = stateNode;
			_graph.AddNode(stateNode);

			var stateRef = new StateRef(key, this);
			_stateRefs[key] = stateRef;

			return stateRef;
		}

		/// <summary>
		/// Gets a reference to a previously added state.
		/// </summary>
		/// <param name="key">The state key.</param>
		public StateRef State(string key)
		{
			if (!_stateRefs.TryGetValue(key, out var stateRef))
			{
				throw new ArgumentException($"No state with key '{key}' exists", nameof(key));
			}

			return stateRef;
		}

		/// <summary>
		/// Gets a reference to a previously added state by type.
		/// </summary>
		/// <typeparam name="T">The state type.</typeparam>
		public StateRef State<T>() where T : State
		{
			return State(typeof(T).Name);
		}

		/// <summary>
		/// Sets which state the state machine should start in.
		/// </summary>
		/// <param name="stateKey">The key of the entry state.</param>
		public StateMachineBuilder SetEntryState(string stateKey)
		{
			if (!_statesByKey.ContainsKey(stateKey))
			{
				throw new ArgumentException($"No state with key '{stateKey}' exists", nameof(stateKey));
			}

			_entryStateKey = stateKey;
			return this;
		}

		/// <summary>
		/// Sets which state the state machine should start in.
		/// </summary>
		/// <param name="stateRef">Reference to the entry state.</param>
		public StateMachineBuilder SetEntryState(StateRef stateRef)
		{
			return SetEntryState(stateRef.Key);
		}

		/// <summary>
		/// Sets which state the state machine should start in.
		/// </summary>
		/// <typeparam name="T">The State type to use as entry.</typeparam>
		public StateMachineBuilder SetEntryState<T>() where T : State
		{
			return SetEntryState(typeof(T).Name);
		}

		/// <summary>
		/// Adds a transition between two states.
		/// </summary>
		/// <param name="fromStateKey">The source state key.</param>
		/// <param name="transitionName">The transition port name (matches [Transition] attribute).</param>
		/// <param name="toStateKey">The target state key.</param>
		/// <param name="transitionTime">Optional delay before transition completes.</param>
		public StateMachineBuilder AddTransition(string fromStateKey, string transitionName, string toStateKey, float transitionTime = 0f)
		{
			if (!_statesByKey.TryGetValue(fromStateKey, out var fromNode))
			{
				throw new ArgumentException($"No state with key '{fromStateKey}' exists", nameof(fromStateKey));
			}

			if (!_statesByKey.TryGetValue(toStateKey, out var toNode))
			{
				throw new ArgumentException($"No state with key '{toStateKey}' exists", nameof(toStateKey));
			}

			var connection = new Connection(
				fromNode.Id, transitionName,
				toNode.Id, "Enter",
				ConnectionType.Transition
			)
			{
				TransitionTime = transitionTime
			};

			_graph.AddConnection(connection);
			return this;
		}

		/// <summary>
		/// Adds a transition between two states using type parameters.
		/// </summary>
		/// <typeparam name="TFrom">The source state type.</typeparam>
		/// <typeparam name="TTo">The target state type.</typeparam>
		/// <param name="transitionName">The transition port name.</param>
		/// <param name="transitionTime">Optional delay before transition completes.</param>
		public StateMachineBuilder AddTransition<TFrom, TTo>(string transitionName, float transitionTime = 0f)
			where TFrom : State
			where TTo : State
		{
			return AddTransition(typeof(TFrom).Name, transitionName, typeof(TTo).Name, transitionTime);
		}

		/// <summary>
		/// Adds a transition between two states using state references.
		/// </summary>
		/// <param name="fromState">The source state reference.</param>
		/// <param name="transitionName">The transition port name.</param>
		/// <param name="toState">The target state reference.</param>
		/// <param name="transitionTime">Optional delay before transition completes.</param>
		public StateMachineBuilder AddTransition(StateRef fromState, string transitionName, StateRef toState, float transitionTime = 0f)
		{
			return AddTransition(fromState.Key, transitionName, toState.Key, transitionTime);
		}

		/// <summary>
		/// Adds a blackboard variable with a default value.
		/// </summary>
		/// <typeparam name="T">The variable type.</typeparam>
		/// <param name="key">The variable key.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <param name="exposed">Whether the variable should be exposed in the inspector.</param>
		public StateMachineBuilder AddVariable<T>(string key, T defaultValue = default, bool exposed = false)
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = key,
				ValueType = typeof(T),
				DefaultValue = defaultValue,
				IsExposed = exposed,
				IsExposedInGraph = exposed
			};

			_graph.AddBlackboardVariable(definition);
			return this;
		}

		/// <summary>
		/// Sets a custom context for the state machine.
		/// </summary>
		/// <param name="context">The context to use.</param>
		public StateMachineBuilder WithContext(IStateMachineContext context)
		{
			_context = context;
			return this;
		}

		/// <summary>
		/// Sets the automatic layout mode for the graph.
		/// Default is Hierarchical.
		/// </summary>
		/// <param name="mode">The layout algorithm to use.</param>
		public StateMachineBuilder WithLayout(GraphLayoutMode mode)
		{
			_layoutMode = mode;
			_layoutAction = null;
			return this;
		}

		/// <summary>
		/// Sets a custom layout action for the graph.
		/// </summary>
		/// <param name="layoutAction">A custom action that positions nodes in the graph.</param>
		public StateMachineBuilder WithLayout(Action<StateMachineGraph> layoutAction)
		{
			_layoutMode = GraphLayoutMode.None;
			_layoutAction = layoutAction;
			return this;
		}

		/// <summary>
		/// Disables automatic layout. Use this if you want to set positions manually.
		/// </summary>
		public StateMachineBuilder WithoutLayout()
		{
			_layoutMode = GraphLayoutMode.None;
			_layoutAction = null;
			return this;
		}

		/// <summary>
		/// Sets the position of a node by its key.
		/// </summary>
		internal void SetNodePosition(string key, Vector2 position)
		{
			if (_statesByKey.TryGetValue(key, out var node))
			{
				node.Position = position;
			}
		}

		/// <summary>
		/// Sets the position of the entry node.
		/// </summary>
		public StateMachineBuilder SetEntryNodePosition(Vector2 position)
		{
			_entryNode.Position = position;
			return this;
		}

		/// <summary>
		/// Static callback for applying layout. Set by Editor code.
		/// </summary>
		public static Action<StateMachineGraph, GraphLayoutMode> LayoutCallback { get; set; }

		/// <summary>
		/// Builds and returns the state machine instance.
		/// Call Start() on the returned instance to begin execution.
		/// </summary>
		public StateMachineInstance Build()
		{
			// Validate
			if (_statesByKey.Count == 0)
			{
				throw new InvalidOperationException("State machine must have at least one state");
			}

			// Connect entry node to entry state
			if (!string.IsNullOrEmpty(_entryStateKey))
			{
				var entryState = _statesByKey[_entryStateKey];
				var entryConnection = new Connection(
					_entryNode.Id, "Start",
					entryState.Id, "Enter",
					ConnectionType.Transition
				);
				_graph.AddConnection(entryConnection);
			}
			else
			{
				// Default to first added state
				var firstState = GetFirstState();
				if (firstState != null)
				{
					var entryConnection = new Connection(
						_entryNode.Id, "Start",
						firstState.Id, "Enter",
						ConnectionType.Transition
					);
					_graph.AddConnection(entryConnection);
				}
			}

			// Apply layout
			if (_layoutAction != null)
			{
				_layoutAction(_graph);
			}
			else if (_layoutMode != GraphLayoutMode.None)
			{
				LayoutCallback?.Invoke(_graph, _layoutMode);
			}

			// Create context if not provided
			_context ??= _gameObject != null
				? new StateMachineContext(_gameObject)
				: new StateMachineContext();

			return new StateMachineInstance(_graph, _context);
		}

		/// <summary>
		/// Builds the state machine and immediately starts it.
		/// </summary>
		public StateMachineInstance BuildAndStart()
		{
			var instance = Build();
			instance.Start();
			return instance;
		}

		/// <summary>
		/// Gets the underlying graph for advanced configuration.
		/// </summary>
		public StateMachineGraph GetGraph() => _graph;

		/// <summary>
		/// Gets a state node by key for advanced configuration.
		/// </summary>
		public StateNode GetStateNode(string key)
		{
			_statesByKey.TryGetValue(key, out var node);
			return node;
		}

		private StateNode GetFirstState()
		{
			foreach (var state in _statesByKey.Values)
			{
				return state;
			}

			return null;
		}
	}
}

using System;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Generic base class for states that require a typed shared context.
	/// Use this when implementing the State Pattern where multiple states
	/// need access to shared, mutable state.
	/// </summary>
	/// <typeparam name="TContext">The type of shared context this state requires.</typeparam>
	/// <example>
	/// <code>
	/// public class EnemyContext
	/// {
	///     public Transform Target { get; set; }
	///     public float AlertLevel { get; set; }
	///     public Vector3 LastKnownPlayerPosition { get; set; }
	/// }
	///
	/// public class PatrolState : State&lt;EnemyContext&gt;
	/// {
	///     public override void OnEnter()
	///     {
	///         SharedContext.AlertLevel = 0;
	///         SharedContext.Target = null;
	///     }
	/// }
	/// </code>
	/// </example>
	public abstract class State<TContext> : State, IContextReceiver where TContext : class
	{
		private TContext _sharedContext;

		/// <summary>
		/// The typed shared context accessible to this state.
		/// This is guaranteed to be non-null during state lifecycle methods
		/// (OnEnter, OnUpdate, OnExit, etc.) when properly configured.
		/// </summary>
		protected TContext SharedContext => _sharedContext;

		/// <summary>
		/// The type of context this state requires.
		/// Used for validation and editor tooling.
		/// </summary>
		public Type RequiredContextType => typeof(TContext);

		/// <summary>
		/// Sets the shared context for this state.
		/// Called internally by StateMachineInstance during state initialization.
		/// </summary>
		internal void SetSharedContext(TContext context)
		{
			_sharedContext = context;
		}

		/// <summary>
		/// Sets the shared context from an untyped object.
		/// Implements IContextReceiver for fast context injection without reflection.
		/// </summary>
		public void SetContextUntyped(object context)
		{
			_sharedContext = context as TContext;
		}

		/// <summary>
		/// Checks if this state has a valid shared context.
		/// </summary>
		public bool HasValidContext => _sharedContext != null;
	}
}

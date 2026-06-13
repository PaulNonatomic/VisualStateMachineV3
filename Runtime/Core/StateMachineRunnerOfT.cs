using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Generic state machine runner that provides typed context to states.
	/// Use this when your states inherit from State&lt;TContext&gt;.
	/// </summary>
	/// <typeparam name="TContext">The type of shared context for all states.</typeparam>
	/// <remarks>
	/// Create a concrete runner class for your specific context type:
	/// <code>
	/// public class EnemyStateMachineRunner : StateMachineRunner&lt;EnemyContext&gt;
	/// {
	///     protected override EnemyContext CreateSharedContext()
	///     {
	///         return new EnemyContext
	///         {
	///             MaxHealth = 100,
	///             PatrolSpeed = 3f
	///         };
	///     }
	/// }
	/// </code>
	/// </remarks>
	public abstract class StateMachineRunner<TContext> : StateMachineRunner where TContext : class
	{
		private TContext _sharedContext;

		/// <summary>
		/// The typed shared context instance.
		/// </summary>
		public TContext SharedContext => _sharedContext;

		/// <summary>
		/// Creates the shared context instance.
		/// Override this to provide initial values for your context.
		/// </summary>
		/// <returns>A new instance of the shared context.</returns>
		protected abstract TContext CreateSharedContext();

		/// <summary>
		/// Creates the state machine instance with the shared context.
		/// </summary>
		protected override StateMachineInstance CreateInstance(StateMachineGraph graph, IStateMachineContext context)
		{
			// Create shared context if not already set
			_sharedContext ??= CreateSharedContext();

			var instance = new StateMachineInstance(graph, context, _sharedContext);
			instance.SetExternalCancellationToken(GetDestroyCancellationToken());
			return instance;
		}

		/// <summary>
		/// Sets the shared context before the state machine is initialized.
		/// Call this before Awake() or before StartStateMachine() if AutoStart is disabled.
		/// </summary>
		public void SetSharedContext(TContext context)
		{
			_sharedContext = context;

			// If instance already exists, update it
			if (Instance != null)
			{
				Instance.SetSharedContext(context);
			}
		}
	}
}

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Generic state machine graph that declares a typed context requirement.
	/// States using State&lt;TContext&gt; will have access to the shared context.
	/// </summary>
	/// <typeparam name="TContext">The type of shared context for all states in this graph.</typeparam>
	/// <remarks>
	/// Note: Unity cannot create ScriptableObject assets directly from generic types.
	/// To use this in the editor, create a concrete derived class:
	/// <code>
	/// [CreateAssetMenu(menuName = "State Machines/Enemy AI Graph")]
	/// public class EnemyAIGraph : StateMachineGraph&lt;EnemyContext&gt; { }
	/// </code>
	///
	/// For code-only usage, you can instantiate directly:
	/// <code>
	/// var graph = ScriptableObject.CreateInstance&lt;StateMachineGraph&lt;EnemyContext&gt;&gt;();
	/// </code>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Define your context
	/// public class EnemyContext
	/// {
	///     public Transform Target { get; set; }
	///     public float AlertLevel { get; set; }
	/// }
	///
	/// // Create a concrete graph type for Unity asset creation
	/// [CreateAssetMenu(menuName = "State Machines/Enemy AI")]
	/// public class EnemyAIGraph : StateMachineGraph&lt;EnemyContext&gt; { }
	///
	/// // States can now use typed context
	/// public class PatrolState : State&lt;EnemyContext&gt;
	/// {
	///     public override void OnEnter()
	///     {
	///         SharedContext.AlertLevel = 0;
	///     }
	/// }
	/// </code>
	/// </example>
	public class StateMachineGraph<TContext> : StateMachineGraph where TContext : class
	{
		protected override void OnEnable()
		{
			// Ensure context type is set when the graph is loaded/created
			if (ContextType == null || ContextType != typeof(TContext))
			{
				ContextType = typeof(TContext);
			}
		}

		protected override void Reset()
		{
			// Call base to initialize entry node
			base.Reset();

			// Set context type when created in editor
			ContextType = typeof(TContext);
		}
	}
}

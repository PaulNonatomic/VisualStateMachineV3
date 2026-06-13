using System;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Interface for states that can receive a typed context.
	/// Implemented by State&lt;TContext&gt; to enable fast context injection
	/// without reflection.
	/// </summary>
	public interface IContextReceiver
	{
		/// <summary>
		/// The type of context this state requires.
		/// Used for validation before context injection.
		/// </summary>
		Type RequiredContextType { get; }

		/// <summary>
		/// Sets the shared context from an untyped object.
		/// The context should be assignable to RequiredContextType.
		/// </summary>
		/// <param name="context">The context object to set.</param>
		void SetContextUntyped(object context);
	}
}

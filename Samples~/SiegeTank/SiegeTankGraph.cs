using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank
{
	/// <summary>
	/// State machine graph for the Siege Tank.
	/// Uses the SiegeTank component itself as the shared context type.
	///
	/// Flow:
	///                    ┌─────────────────────────────────────┐
	///                    │                                     │
	///                    ▼                                     │
	/// [Entry] → [Tank Mode] → [Sieging Up] → [Siege Mode] → [Unsieging]
	///               ▲                              │              │
	///               │                              │              │
	///               └──────────────────────────────┴──────────────┘
	///
	/// States use State&lt;SiegeTank&gt; and access the tank component directly:
	/// - SharedContext.IsSieged
	/// - SharedContext.Config.siegeModeDamage
	/// - SharedContext.transform.position
	/// </summary>
	[CreateAssetMenu(
		fileName = "SiegeTankGraph",
		menuName = "Visual State Machine/Samples/Siege Tank Graph"
	)]
	public class SiegeTankGraph : StateMachineGraph<SiegeTank>
	{
		// The base class automatically sets ContextType = typeof(SiegeTank)
		// All states using State<SiegeTank> will have access to the tank component
	}
}

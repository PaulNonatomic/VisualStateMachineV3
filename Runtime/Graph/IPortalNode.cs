using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Common interface for portal nodes (PortalIn and PortalOut).
	/// </summary>
	public interface IPortalNode
	{
		int ChannelId { get; set; }
		string ChannelLabel { get; set; }
		Color ChannelColor { get; }
	}
}

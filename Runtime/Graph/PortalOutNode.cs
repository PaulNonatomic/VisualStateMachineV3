using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// A portal-out node that teleports execution to a matching PortalIn node.
	/// Multiple PortalOut nodes can target the same channel (many-to-one).
	/// </summary>
	[Serializable]
	public class PortalOutNode : GraphNode, IPortalNode
	{
		[SerializeField] private int _channelId;
		[SerializeField] private string _channelLabel;

		/// <summary>
		/// The channel ID that links this PortalOut to a PortalIn node.
		/// </summary>
		public int ChannelId
		{
			get => _channelId;
			set => _channelId = value;
		}

		/// <summary>
		/// User-editable label for this portal channel.
		/// </summary>
		public string ChannelLabel
		{
			get => _channelLabel;
			set => _channelLabel = value;
		}

		/// <summary>
		/// Gets the color for this channel.
		/// </summary>
		public Color ChannelColor => PortalChannel.GetColor(_channelId);

		public override GraphNodeType NodeType => GraphNodeType.PortalOut;

		/// <summary>
		/// Initializes a new portal-out node with an auto-generated ID.
		/// </summary>
		public PortalOutNode() : base()
		{
			DisplayName = "Portal Out";
			_channelLabel = "";
		}

		/// <summary>
		/// Initializes a new portal-out node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public PortalOutNode(string id) : base(id)
		{
			DisplayName = "Portal Out";
			_channelLabel = "";
		}

		/// <summary>
		/// Initializes a new portal-out node on the specified channel.
		/// </summary>
		/// <param name="channelId">Channel ID to teleport execution to.</param>
		/// <param name="label">Optional label, defaults to channel's default label.</param>
		public PortalOutNode(int channelId, string label = null) : base()
		{
			_channelId = channelId;
			_channelLabel = label ?? PortalChannel.GetDefaultLabel(channelId);
			DisplayName = "Portal Out";
		}
	}
}

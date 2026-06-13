using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// A portal-in node that serves as a destination for PortalOut nodes.
	/// Only one PortalIn per channel is allowed in a graph.
	/// </summary>
	[Serializable]
	public class PortalInNode : GraphNode, IPortalNode
	{
		[SerializeField] private int _channelId;
		[SerializeField] private string _channelLabel;

		/// <summary>
		/// The channel ID that links PortalOut nodes to this PortalIn.
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

		public override GraphNodeType NodeType => GraphNodeType.PortalIn;

		/// <summary>
		/// Initializes a new portal-in node with an auto-generated ID.
		/// </summary>
		public PortalInNode() : base()
		{
			DisplayName = "Portal In";
			_channelLabel = "";
		}

		/// <summary>
		/// Initializes a new portal-in node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public PortalInNode(string id) : base(id)
		{
			DisplayName = "Portal In";
			_channelLabel = "";
		}

		/// <summary>
		/// Initializes a new portal-in node on the specified channel.
		/// </summary>
		/// <param name="channelId">Channel ID to receive teleported transitions from.</param>
		/// <param name="label">Optional label, defaults to channel's default label.</param>
		public PortalInNode(int channelId, string label = null) : base()
		{
			_channelId = channelId;
			_channelLabel = label ?? PortalChannel.GetDefaultLabel(channelId);
			DisplayName = "Portal In";
		}
	}
}

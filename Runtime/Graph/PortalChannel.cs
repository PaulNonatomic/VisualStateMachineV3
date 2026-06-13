using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Utility class for portal channel color mapping and default labels.
	/// </summary>
	public static class PortalChannel
	{
		/// <summary>
		/// 16-color palette for channels (cycles for channels > 15).
		/// </summary>
		private static readonly Color[] ChannelColors =
		{
			new Color(0.90f, 0.30f, 0.30f), // 0: Red
			new Color(0.30f, 0.70f, 0.90f), // 1: Blue
			new Color(0.40f, 0.80f, 0.40f), // 2: Green
			new Color(0.95f, 0.75f, 0.20f), // 3: Yellow
			new Color(0.80f, 0.40f, 0.80f), // 4: Purple
			new Color(0.95f, 0.60f, 0.30f), // 5: Orange
			new Color(0.40f, 0.85f, 0.85f), // 6: Cyan
			new Color(0.90f, 0.50f, 0.70f), // 7: Pink
			new Color(0.55f, 0.45f, 0.35f), // 8: Brown
			new Color(0.60f, 0.80f, 0.30f), // 9: Lime
			new Color(0.50f, 0.50f, 0.80f), // 10: Indigo
			new Color(0.85f, 0.85f, 0.50f), // 11: Gold
			new Color(0.45f, 0.65f, 0.55f), // 12: Teal
			new Color(0.75f, 0.55f, 0.45f), // 13: Coral
			new Color(0.65f, 0.65f, 0.65f), // 14: Gray
			new Color(0.95f, 0.95f, 0.95f), // 15: White
		};

		/// <summary>
		/// Gets the color for a given channel ID.
		/// </summary>
		/// <param name="channelId">The channel ID.</param>
		/// <returns>The color associated with the channel.</returns>
		public static Color GetColor(int channelId)
		{
			var index = Mathf.Abs(channelId) % ChannelColors.Length;
			return ChannelColors[index];
		}

		/// <summary>
		/// Gets the default label for a channel.
		/// </summary>
		/// <param name="channelId">The channel ID.</param>
		/// <returns>A default label like "Channel 0", "Channel 1", etc.</returns>
		public static string GetDefaultLabel(int channelId)
		{
			return $"Channel {channelId}";
		}

		/// <summary>
		/// Gets the number of colors in the palette.
		/// </summary>
		public static int ColorCount => ChannelColors.Length;
	}
}

using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalChannelTests
	{
		[Test]
		public void ColorCount_Returns16()
		{
			Assert.AreEqual(16, PortalChannel.ColorCount);
		}

		[Test]
		public void GetColor_Channel0_ReturnsRed()
		{
			var color = PortalChannel.GetColor(0);

			Assert.AreEqual(0.90f, color.r, 0.001f);
			Assert.AreEqual(0.30f, color.g, 0.001f);
			Assert.AreEqual(0.30f, color.b, 0.001f);
		}

		[Test]
		public void GetColor_Channel1_ReturnsBlue()
		{
			var color = PortalChannel.GetColor(1);

			Assert.AreEqual(0.30f, color.r, 0.001f);
			Assert.AreEqual(0.70f, color.g, 0.001f);
			Assert.AreEqual(0.90f, color.b, 0.001f);
		}

		[Test]
		public void GetColor_Channel15_ReturnsWhite()
		{
			var color = PortalChannel.GetColor(15);

			Assert.AreEqual(0.95f, color.r, 0.001f);
			Assert.AreEqual(0.95f, color.g, 0.001f);
			Assert.AreEqual(0.95f, color.b, 0.001f);
		}

		[Test]
		public void GetColor_Channel16_CyclesToChannel0()
		{
			var color0 = PortalChannel.GetColor(0);
			var color16 = PortalChannel.GetColor(16);

			Assert.AreEqual(color0, color16);
		}

		[Test]
		public void GetColor_Channel32_CyclesToChannel0()
		{
			var color0 = PortalChannel.GetColor(0);
			var color32 = PortalChannel.GetColor(32);

			Assert.AreEqual(color0, color32);
		}

		[Test]
		public void GetColor_NegativeChannel_UsesAbsoluteValue()
		{
			var colorPos = PortalChannel.GetColor(3);
			var colorNeg = PortalChannel.GetColor(-3);

			Assert.AreEqual(colorPos, colorNeg);
		}

		[Test]
		public void GetColor_NegativeChannel_LargeValue_Cycles()
		{
			var color = PortalChannel.GetColor(-17);
			var expected = PortalChannel.GetColor(1);

			Assert.AreEqual(expected, color);
		}

		[Test]
		public void GetColor_EachChannel_ReturnsDifferentColor()
		{
			for (var i = 0; i < 16; i++)
			{
				for (var j = i + 1; j < 16; j++)
				{
					var colorI = PortalChannel.GetColor(i);
					var colorJ = PortalChannel.GetColor(j);

					Assert.AreNotEqual(colorI, colorJ,
						$"Channel {i} and channel {j} should have different colors");
				}
			}
		}

		[Test]
		public void GetDefaultLabel_Channel0_ReturnsChannel0()
		{
			Assert.AreEqual("Channel 0", PortalChannel.GetDefaultLabel(0));
		}

		[Test]
		public void GetDefaultLabel_Channel5_ReturnsChannel5()
		{
			Assert.AreEqual("Channel 5", PortalChannel.GetDefaultLabel(5));
		}

		[Test]
		public void GetDefaultLabel_NegativeChannel_IncludesNegativeSign()
		{
			Assert.AreEqual("Channel -1", PortalChannel.GetDefaultLabel(-1));
		}

		[Test]
		public void GetDefaultLabel_LargeChannel_ReturnsLabel()
		{
			Assert.AreEqual("Channel 999", PortalChannel.GetDefaultLabel(999));
		}
	}
}

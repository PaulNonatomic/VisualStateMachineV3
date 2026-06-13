using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class NodeContextMenuHelperTests
	{
		[Test]
		public void TruncateId_NullInput_ReturnsEmptyString()
		{
			var result = NodeContextMenuHelper.TruncateId(null);

			Assert.AreEqual(string.Empty, result);
		}

		[Test]
		public void TruncateId_EmptyInput_ReturnsEmptyString()
		{
			var result = NodeContextMenuHelper.TruncateId(string.Empty);

			Assert.AreEqual(string.Empty, result);
		}

		[Test]
		public void TruncateId_ShortId_ReturnsFullId()
		{
			var shortId = "abc123";

			var result = NodeContextMenuHelper.TruncateId(shortId);

			Assert.AreEqual(shortId, result);
		}

		[Test]
		public void TruncateId_ExactlyTwelveCharacters_ReturnsFullId()
		{
			var exactId = "123456789012";

			var result = NodeContextMenuHelper.TruncateId(exactId);

			Assert.AreEqual(exactId, result);
		}

		[Test]
		public void TruncateId_LongGuid_ReturnsTruncatedWithEllipsis()
		{
			var guid = "550e8400-e29b-41d4-a716-446655440000";

			var result = NodeContextMenuHelper.TruncateId(guid);

			Assert.AreEqual("550e8400...", result);
		}

		[Test]
		public void TruncateId_ThirteenCharacters_ReturnsTruncated()
		{
			var id = "1234567890123";

			var result = NodeContextMenuHelper.TruncateId(id);

			Assert.AreEqual("12345678...", result);
		}
	}
}

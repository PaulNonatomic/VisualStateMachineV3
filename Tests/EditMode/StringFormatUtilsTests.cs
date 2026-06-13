using NUnit.Framework;
using Nonatomic.VSM.Utils;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StringFormatUtilsTests
	{

		[Test]
		public void FormatFieldName_NullInput_ReturnsNull()
		{
			var result = StringFormatUtils.FormatFieldName(null);
			Assert.IsNull(result);
		}

		[Test]
		public void FormatFieldName_EmptyString_ReturnsEmpty()
		{
			var result = StringFormatUtils.FormatFieldName("");
			Assert.AreEqual("", result);
		}

		[Test]
		public void FormatFieldName_RemovesLeadingUnderscore()
		{
			var result = StringFormatUtils.FormatFieldName("_playerHealth");
			Assert.AreEqual("Player Health", result);
		}

		[Test]
		public void FormatFieldName_ConvertsCamelCaseToTitleCase()
		{
			var result = StringFormatUtils.FormatFieldName("maxSpeed");
			Assert.AreEqual("Max Speed", result);
		}

		[Test]
		public void FormatFieldName_HandlesPascalCase()
		{
			var result = StringFormatUtils.FormatFieldName("PlayerName");
			Assert.AreEqual("Player Name", result);
		}

		[Test]
		public void FormatFieldName_HandlesConsecutiveUppercase()
		{
			var result = StringFormatUtils.FormatFieldName("XMLParser");
			Assert.AreEqual("XMLParser", result);
		}

		[Test]
		public void FormatFieldName_SingleCharacter()
		{
			var result = StringFormatUtils.FormatFieldName("x");
			Assert.AreEqual("X", result);
		}

		[Test]
		public void FormatFieldName_SingleCharacterWithUnderscore()
		{
			var result = StringFormatUtils.FormatFieldName("_x");
			Assert.AreEqual("X", result);
		}

		[Test]
		public void FormatFieldName_CapitalizesFirstLetter()
		{
			var result = StringFormatUtils.FormatFieldName("speed");
			Assert.AreEqual("Speed", result);
		}

		[Test]
		public void FormatFieldName_DoesNotRemoveOnPrefix()
		{
			var result = StringFormatUtils.FormatFieldName("onComplete");
			Assert.AreEqual("On Complete", result);
		}



		[Test]
		public void FormatTransitionName_NullInput_ReturnsNull()
		{
			var result = StringFormatUtils.FormatTransitionName(null);
			Assert.IsNull(result);
		}

		[Test]
		public void FormatTransitionName_EmptyString_ReturnsEmpty()
		{
			var result = StringFormatUtils.FormatTransitionName("");
			Assert.AreEqual("", result);
		}

		[Test]
		public void FormatTransitionName_RemovesLeadingUnderscore()
		{
			var result = StringFormatUtils.FormatTransitionName("_onComplete");
			Assert.AreEqual("Complete", result);
		}

		[Test]
		public void FormatTransitionName_RemovesLowercaseOnPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("onSuccess");
			Assert.AreEqual("Success", result);
		}

		[Test]
		public void FormatTransitionName_RemovesUppercaseOnPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("OnFailure");
			Assert.AreEqual("Failure", result);
		}

		[Test]
		public void FormatTransitionName_RemovesMixedCaseOnPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("ONTimeout");
			Assert.AreEqual("Timeout", result);
		}

		[Test]
		public void FormatTransitionName_ConvertsCamelCaseToTitleCase()
		{
			var result = StringFormatUtils.FormatTransitionName("onStateChanged");
			Assert.AreEqual("State Changed", result);
		}

		[Test]
		public void FormatTransitionName_ShortOnWord_NotRemoved()
		{
			// "on" with length exactly 2 should not be removed (leaves empty string)
			var result = StringFormatUtils.FormatTransitionName("on");
			Assert.AreEqual("On", result);
		}

		[Test]
		public void FormatTransitionName_UnderscoreAndOnPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("_onPlayerDied");
			Assert.AreEqual("Player Died", result);
		}

		[Test]
		public void FormatTransitionName_WithoutOnPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("complete");
			Assert.AreEqual("Complete", result);
		}

		[Test]
		public void FormatTransitionName_OnlyUnderscore()
		{
			var result = StringFormatUtils.FormatTransitionName("_");
			Assert.AreEqual("", result);
		}



		[Test]
		public void FormatFieldName_JustUnderscore_ReturnsEmpty()
		{
			var result = StringFormatUtils.FormatFieldName("_");
			Assert.AreEqual("", result);
		}

		[Test]
		public void FormatFieldName_DoubleUnderscore_RemovesOnlyFirst()
		{
			// "__value" -> removes first underscore -> "_value" -> ConvertCamelCase keeps underscore
			var result = StringFormatUtils.FormatFieldName("__value");
			Assert.AreEqual("_value", result);
		}

		[Test]
		public void FormatFieldName_NumberInMiddle_InsertsSpaceBeforeNextUpper()
		{
			// "speed2D" -> 'D' is upper, '2' is not upper -> space before D
			var result = StringFormatUtils.FormatFieldName("speed2D");
			Assert.AreEqual("Speed2 D", result);
		}

		[Test]
		public void FormatFieldName_NumberAtEnd_NoExtraSpace()
		{
			var result = StringFormatUtils.FormatFieldName("player3");
			Assert.AreEqual("Player3", result);
		}

		[Test]
		public void FormatFieldName_NumberBeforeWord_InsertsSpace()
		{
			var result = StringFormatUtils.FormatFieldName("mp3Player");
			Assert.AreEqual("Mp3 Player", result);
		}

		[Test]
		public void FormatFieldName_AllUppercase_NoSpacesAdded()
		{
			// All chars are uppercase, so char.IsUpper(i-1) is always true -> no spaces
			var result = StringFormatUtils.FormatFieldName("ABC");
			Assert.AreEqual("ABC", result);
		}

		[Test]
		public void FormatFieldName_AllLowercase_CapitalizesFirst()
		{
			var result = StringFormatUtils.FormatFieldName("speed");
			Assert.AreEqual("Speed", result);
		}

		[Test]
		public void FormatFieldName_MultipleWords_FormatsAll()
		{
			var result = StringFormatUtils.FormatFieldName("thisIsALongFieldName");
			Assert.AreEqual("This Is ALong Field Name", result);
		}

		[Test]
		public void FormatFieldName_AcronymFollowedByWord_KeepsAcronymTogether()
		{
			// "HTTPResponse" -> H(+) T(upper, H upper, no space) T(upper, T upper) P(upper, T upper) R(upper, P upper) e(lower)...
			// Wait: R is upper, P is upper -> no space. Then 'e' is not upper -> no space before 'e'.
			// Result: "HTTPResponse"
			var result = StringFormatUtils.FormatFieldName("HTTPResponse");
			Assert.AreEqual("HTTPResponse", result);
		}

		[Test]
		public void FormatFieldName_UnderscoreWithPascalCase_FormatsCorrectly()
		{
			var result = StringFormatUtils.FormatFieldName("_MaxHealth");
			Assert.AreEqual("Max Health", result);
		}

		[Test]
		public void FormatFieldName_WhitespaceString_ReturnsWhitespace()
		{
			// Single space is not null/empty, no underscore, ConvertCamelCase: char.ToUpper(' ') = ' '
			var result = StringFormatUtils.FormatFieldName(" ");
			Assert.AreEqual(" ", result);
		}



		[Test]
		public void FormatTransitionName_DoubleUnderscore_RemovesOnlyFirstUnderscore()
		{
			// "__onComplete" -> remove first underscore -> "_onComplete" -> starts with "on"? No, starts with "_"
			// So "_onComplete" -> remove leading underscore -> "onComplete" -> remove "on" -> "Complete"
			// Wait, it only does one underscore removal. Let me trace:
			// Step 1: "_" + "__onComplete".Substring(1) = "_onComplete"... no wait.
			// "__onComplete".StartsWith("_") = true -> Substring(1) -> "_onComplete"
			// "_onComplete".StartsWith("on") = false (starts with "_")
			// ConvertCamelCase("_onComplete") -> first char '_', then 'o' not upper, 'n' not upper, 'C' upper...
			var result = StringFormatUtils.FormatTransitionName("__onComplete");
			Assert.AreEqual("_on Complete", result);
		}

		[Test]
		public void FormatTransitionName_OnOnly_ReturnsOn()
		{
			// "on" has length 2, StartsWith("on") but length is NOT > 2, so "on" prefix NOT removed
			var result = StringFormatUtils.FormatTransitionName("on");
			Assert.AreEqual("On", result);
		}

		[Test]
		public void FormatTransitionName_OnX_RemovesOnPrefix()
		{
			// "onX" has length 3 > 2, starts with "on" -> remove -> "X"
			var result = StringFormatUtils.FormatTransitionName("onX");
			Assert.AreEqual("X", result);
		}

		[Test]
		public void FormatTransitionName_WordStartingWithOn_RemovesPrefix()
		{
			// "ongoing" starts with "on" and length > 2 -> removes "on" -> "going" -> "Going"
			var result = StringFormatUtils.FormatTransitionName("ongoing");
			Assert.AreEqual("Going", result);
		}

		[Test]
		public void FormatTransitionName_NumberAfterOn_RemovesPrefix()
		{
			var result = StringFormatUtils.FormatTransitionName("on3rdAttempt");
			Assert.AreEqual("3rd Attempt", result);
		}

		[Test]
		public void FormatTransitionName_UnderscoreOnly_ReturnsEmpty()
		{
			// Already tested but verifying: "_" -> Substring(1) -> "" -> returned
			var result = StringFormatUtils.FormatTransitionName("_");
			Assert.AreEqual("", result);
		}

		[Test]
		public void FormatTransitionName_UnderscoreOn_RemovesUnderscoreKeepsOn()
		{
			// "_on" -> remove underscore -> "on" (length 2, not > 2) -> keep "on" -> "On"
			var result = StringFormatUtils.FormatTransitionName("_on");
			Assert.AreEqual("On", result);
		}

	}
}

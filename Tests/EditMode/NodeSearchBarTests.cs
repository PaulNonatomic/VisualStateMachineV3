using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class NodeSearchBarTests
	{
		[Test]
		public void FuzzyScore_ExactMatch_ReturnsHighestScore()
		{
			var score = NodeSearchBar.FuzzyScore("logstate", "logstate");

			Assert.AreEqual(10000, score);
		}

		[Test]
		public void FuzzyScore_StartsWithMatch_ReturnsHighScore()
		{
			var score = NodeSearchBar.FuzzyScore("log", "logstate");

			Assert.Greater(score, 5000);
		}

		[Test]
		public void FuzzyScore_ContainsMatch_ReturnsMediumScore()
		{
			var score = NodeSearchBar.FuzzyScore("state", "logstate");

			Assert.Greater(score, 2000);
			Assert.Less(score, 5000);
		}

		[Test]
		public void FuzzyScore_SubsequenceMatch_ReturnsLowScore()
		{
			var score = NodeSearchBar.FuzzyScore("lgs", "logstate");

			Assert.Greater(score, 0);
			Assert.Less(score, 2000);
		}

		[Test]
		public void FuzzyScore_NoMatch_ReturnsZero()
		{
			var score = NodeSearchBar.FuzzyScore("xyz", "logstate");

			Assert.AreEqual(0, score);
		}

		[Test]
		public void FuzzyScore_EmptyQuery_ReturnsZero()
		{
			var score = NodeSearchBar.FuzzyScore("", "logstate");

			Assert.AreEqual(0, score);
		}

		[Test]
		public void FuzzyScore_EmptyTarget_ReturnsZero()
		{
			var score = NodeSearchBar.FuzzyScore("log", "");

			Assert.AreEqual(0, score);
		}

		[Test]
		public void FuzzyScore_NullQuery_ReturnsZero()
		{
			var score = NodeSearchBar.FuzzyScore(null, "logstate");

			Assert.AreEqual(0, score);
		}

		[Test]
		public void FuzzyScore_NullTarget_ReturnsZero()
		{
			var score = NodeSearchBar.FuzzyScore("log", null);

			Assert.AreEqual(0, score);
		}

		[Test]
		public void FuzzyScore_ExactMatch_ScoresHigherThan_StartsWith()
		{
			var exactScore = NodeSearchBar.FuzzyScore("logstate", "logstate");
			var startsWithScore = NodeSearchBar.FuzzyScore("log", "logstate");

			Assert.Greater(exactScore, startsWithScore);
		}

		[Test]
		public void FuzzyScore_StartsWith_ScoresHigherThan_Contains()
		{
			var startsWithScore = NodeSearchBar.FuzzyScore("log", "logstate");
			var containsScore = NodeSearchBar.FuzzyScore("state", "logstate");

			Assert.Greater(startsWithScore, containsScore);
		}

		[Test]
		public void FuzzyScore_Contains_ScoresHigherThan_Subsequence()
		{
			var containsScore = NodeSearchBar.FuzzyScore("state", "logstate");
			var subsequenceScore = NodeSearchBar.FuzzyScore("lgs", "logstate");

			Assert.Greater(containsScore, subsequenceScore);
		}

		[Test]
		public void FuzzyScore_WordBoundaryMatch_ScoresHigherThan_MidWordMatch()
		{
			// "state" at word boundary in "log_state" vs mid-word in "logstate"
			var wordBoundaryScore = NodeSearchBar.FuzzyScore("state", "log_state");
			var midWordScore = NodeSearchBar.FuzzyScore("state", "logstate");

			Assert.Greater(wordBoundaryScore, midWordScore);
		}

		[Test]
		public void FuzzyScore_ConsecutiveSubsequence_ScoresHigherThan_SpreadSubsequence()
		{
			// "lg" consecutive in both, but "las" has different spread
			var consecutiveScore = NodeSearchBar.FuzzyScore("lo", "logstate");
			var spreadScore = NodeSearchBar.FuzzyScore("le", "logstate");

			// "lo" matches first two chars consecutively; "le" matches l...e with gap
			Assert.Greater(consecutiveScore, spreadScore);
		}

		[Test]
		public void NodeSearchBar_CanBeCreated_WithNullWindow()
		{
			var searchBar = new NodeSearchBar(null);

			Assert.IsNotNull(searchBar);
		}

		[Test]
		public void NodeSearchBar_IsHiddenByDefault()
		{
			var searchBar = new NodeSearchBar(null);

			Assert.AreEqual(false, searchBar.IsVisible);
		}

		[Test]
		public void NodeSearchBar_HasCorrectClass()
		{
			var searchBar = new NodeSearchBar(null);

			Assert.IsTrue(searchBar.ClassListContains("node-search-bar"));
		}

		[Test]
		public void NodeSearchBar_PopulateFromAsset_AcceptsNull()
		{
			var searchBar = new NodeSearchBar(null);

			Assert.DoesNotThrow(() => searchBar.PopulateFromAsset(null));
		}
	}
}

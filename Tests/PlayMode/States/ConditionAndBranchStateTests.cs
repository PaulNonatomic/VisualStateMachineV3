using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class ConditionStatePlayModeTests
	{
		[UnityTest]
		public IEnumerator ConditionState_WhenTrue_TransitionsViaOnTrue()
		{
			var state = new ConditionState();
			state.Condition = true;
			var trueCalled = false;
			var falseCalled = false;
			state.OnTrue = () => trueCalled = true;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(trueCalled);
			Assert.IsFalse(falseCalled);
			Assert.IsTrue(state.Result);
		}

		[UnityTest]
		public IEnumerator ConditionState_WhenFalse_TransitionsViaOnFalse()
		{
			var state = new ConditionState();
			state.Condition = false;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(falseCalled);
			Assert.IsFalse(state.Result);
		}

		[UnityTest]
		public IEnumerator ConditionState_Invert_ReversesResult()
		{
			var state = new ConditionState();
			state.Condition = true;
			state.Invert = true;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(falseCalled);
			Assert.IsFalse(state.Result);
		}
	}

	[TestFixture]
	public class CompareStatePlayModeTests
	{
		[UnityTest]
		public IEnumerator CompareState_GreaterThan_EvaluatesCorrectly()
		{
			var state = new CompareState();
			state.ValueA = 10f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.GreaterThan;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(trueCalled);
			Assert.IsTrue(state.Result);
		}

		[UnityTest]
		public IEnumerator CompareState_EqualWithTolerance_HandlesFloatPrecision()
		{
			var state = new CompareState();
			state.ValueA = 1.0f;
			state.ValueB = 1.0f + 0.00001f;
			state.Operator = ComparisonOperator.Equal;
			state.Tolerance = 0.0001f;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(trueCalled);
		}
	}

	[TestFixture]
	public class CompareStringStatePlayModeTests
	{
		[UnityTest]
		public IEnumerator CompareStringState_IgnoreCase_MatchesCaseInsensitively()
		{
			var state = new CompareStringState();
			state.ValueA = "HELLO";
			state.ValueB = "hello";
			state.IgnoreCase = true;
			var equalCalled = false;
			state.OnEqual = () => equalCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(equalCalled);
			Assert.IsTrue(state.Result);
		}

		[UnityTest]
		public IEnumerator CompareStringState_CaseSensitive_DistinguishesCase()
		{
			var state = new CompareStringState();
			state.ValueA = "HELLO";
			state.ValueB = "hello";
			state.IgnoreCase = false;
			var notEqualCalled = false;
			state.OnNotEqual = () => notEqualCalled = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(notEqualCalled);
			Assert.IsFalse(state.Result);
		}
	}

	[TestFixture]
	public class RandomBranchStatePlayModeTests
	{
		[UnityTest]
		public IEnumerator RandomBranchState_AlwaysPathA_WhenProbabilityIsOne()
		{
			for (var i = 0; i < 20; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 1f;
				var pathACalled = false;
				var pathBCalled = false;
				state.OnPathA = () => pathACalled = true;
				state.OnPathB = () => pathBCalled = true;

				state.OnEnter();

				Assert.IsTrue(pathACalled, $"Iteration {i}: Path A should always be called");
				Assert.IsFalse(pathBCalled, $"Iteration {i}: Path B should never be called");
				Assert.IsTrue(state.TookPathA);
			}

			yield return null;
		}

		[UnityTest]
		public IEnumerator RandomBranchState_AlwaysPathB_WhenProbabilityIsZero()
		{
			for (var i = 0; i < 20; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 0f;
				var pathBCalled = false;
				state.OnPathB = () => pathBCalled = true;

				state.OnEnter();

				Assert.IsTrue(pathBCalled, $"Iteration {i}: Path B should always be called");
				Assert.IsFalse(state.TookPathA);
			}

			yield return null;
		}

		[UnityTest]
		public IEnumerator RandomBranchState_ProducesBothPaths_WhenProbabilityIsHalf()
		{
			var pathACount = 0;
			var pathBCount = 0;

			for (var i = 0; i < 100; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 0.5f;
				state.OnPathA = () => pathACount++;
				state.OnPathB = () => pathBCount++;

				state.OnEnter();
			}

			yield return null;

			Assert.Greater(pathACount, 0, "Path A should be taken at least once in 100 trials");
			Assert.Greater(pathBCount, 0, "Path B should be taken at least once in 100 trials");
		}

		[UnityTest]
		public IEnumerator RandomBranchState_NullTransitions_DoNotThrow()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 0.5f;
			state.OnPathA = null;
			state.OnPathB = null;

			state.OnEnter();

			yield return null;

			// Verify state executes without error when transitions are null
			Assert.IsFalse(state.TookPathA, "TookPathA should be false when OnPathA is null");
		}
	}

	[TestFixture]
	public class RandomBranch3StatePlayModeTests
	{
		[UnityTest]
		public IEnumerator RandomBranch3State_AllWeightsEqual_ProducesAllPaths()
		{
			var counts = new int[3];

			for (var i = 0; i < 300; i++)
			{
				var state = new RandomBranch3State();
				state.WeightA = 1f;
				state.WeightB = 1f;
				state.WeightC = 1f;
				state.OnPathA = () => counts[0]++;
				state.OnPathB = () => counts[1]++;
				state.OnPathC = () => counts[2]++;

				state.OnEnter();
			}

			yield return null;

			Assert.Greater(counts[0], 0, "Path A should be taken at least once");
			Assert.Greater(counts[1], 0, "Path B should be taken at least once");
			Assert.Greater(counts[2], 0, "Path C should be taken at least once");
		}

		[UnityTest]
		public IEnumerator RandomBranch3State_ZeroWeights_DefaultsToPathA()
		{
			var state = new RandomBranch3State();
			state.WeightA = 0f;
			state.WeightB = 0f;
			state.WeightC = 0f;
			var pathACalled = false;
			state.OnPathA = () => pathACalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			yield return null;

			Assert.IsTrue(pathACalled);
			Assert.AreEqual(0, state.SelectedPath);
		}

		[UnityTest]
		public IEnumerator RandomBranch3State_SetsSelectedPathOutput()
		{
			var state = new RandomBranch3State();
			state.WeightA = 1f;
			state.WeightB = 0f;
			state.WeightC = 0f;

			state.OnEnter();

			yield return null;

			Assert.AreEqual(0, state.SelectedPath);
		}
	}
}

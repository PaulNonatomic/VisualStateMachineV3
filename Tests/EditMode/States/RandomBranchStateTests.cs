using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class RandomBranchStateTests
	{

		[Test]
		public void RandomBranchState_InvokesExactlyOneTransition()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 0.5f;
			var pathACalled = false;
			var pathBCalled = false;
			state.OnPathA = () => pathACalled = true;
			state.OnPathB = () => pathBCalled = true;

			state.OnEnter();

			// Exactly one should be called
			Assert.IsTrue(pathACalled ^ pathBCalled, "Exactly one path should be taken");
		}

		[Test]
		public void RandomBranchState_SetsTookPathA_WhenPathAIsTaken()
		{
			// Run multiple times to ensure we get a Path A result
			for (int i = 0; i < 100; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 1f; // Always Path A
				state.OnEnter();

				Assert.IsTrue(state.TookPathA, "TookPathA should be true when ProbabilityA is 1");
			}
		}

		[Test]
		public void RandomBranchState_SetsTookPathAFalse_WhenPathBIsTaken()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 0f; // Always Path B

			state.OnEnter();

			Assert.IsFalse(state.TookPathA, "TookPathA should be false when ProbabilityA is 0");
		}



		[Test]
		public void RandomBranchState_WhenProbabilityIsZero_AlwaysTakesPathB()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 0f;
			var pathBCalled = false;
			state.OnPathB = () => pathBCalled = true;

			state.OnEnter();

			Assert.IsTrue(pathBCalled, "Path B should always be taken when probability is 0");
			Assert.IsFalse(state.TookPathA);
		}

		[Test]
		public void RandomBranchState_WhenProbabilityIsOne_AlwaysTakesPathA()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 1f;
			var pathACalled = false;
			state.OnPathA = () => pathACalled = true;

			state.OnEnter();

			Assert.IsTrue(pathACalled, "Path A should always be taken when probability is 1");
			Assert.IsTrue(state.TookPathA);
		}

		[Test]
		public void RandomBranchState_WithNullTransitions_DoesNotThrow()
		{
			var state = new RandomBranchState();
			state.ProbabilityA = 0.5f;
			state.OnPathA = null;
			state.OnPathB = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void RandomBranchState_DefaultProbability_IsFiftyPercent()
		{
			var state = new RandomBranchState();

			Assert.AreEqual(0.5f, state.ProbabilityA, "Default probability should be 0.5");
		}



		[Test]
		public void RandomBranchState_WithFiftyPercentProbability_ProducesReasonableDistribution()
		{
			var pathACount = 0;
			var pathBCount = 0;
			const int iterations = 1000;

			for (int i = 0; i < iterations; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 0.5f;
				state.OnPathA = () => pathACount++;
				state.OnPathB = () => pathBCount++;

				state.OnEnter();
			}

			// With 50% probability, we expect roughly equal distribution
			// Allow for reasonable variance (within 10% of expected)
			var expectedCount = iterations / 2;
			var tolerance = iterations * 0.1f;

			Assert.That(pathACount, Is.InRange((int)(expectedCount - tolerance), (int)(expectedCount + tolerance)),
				$"Path A count ({pathACount}) should be close to {expectedCount}");
			Assert.That(pathBCount, Is.InRange((int)(expectedCount - tolerance), (int)(expectedCount + tolerance)),
				$"Path B count ({pathBCount}) should be close to {expectedCount}");
		}

		[Test]
		public void RandomBranchState_WithTwentyPercentProbability_ProducesReasonableDistribution()
		{
			var pathACount = 0;
			var pathBCount = 0;
			const int iterations = 1000;

			for (int i = 0; i < iterations; i++)
			{
				var state = new RandomBranchState();
				state.ProbabilityA = 0.2f;
				state.OnPathA = () => pathACount++;
				state.OnPathB = () => pathBCount++;

				state.OnEnter();
			}

			// With 20% probability for A, expect roughly 200 A and 800 B
			var expectedACount = iterations * 0.2f;
			var expectedBCount = iterations * 0.8f;
			var tolerance = iterations * 0.1f;

			Assert.That(pathACount, Is.InRange((int)(expectedACount - tolerance), (int)(expectedACount + tolerance)),
				$"Path A count ({pathACount}) should be close to {expectedACount}");
		}

	}

	[TestFixture]
	public class RandomBranch3StateTests
	{

		[Test]
		public void RandomBranch3State_InvokesExactlyOneTransition()
		{
			var state = new RandomBranch3State();
			state.WeightA = 1f;
			state.WeightB = 1f;
			state.WeightC = 1f;
			var transitionCount = 0;
			state.OnPathA = () => transitionCount++;
			state.OnPathB = () => transitionCount++;
			state.OnPathC = () => transitionCount++;

			state.OnEnter();

			Assert.AreEqual(1, transitionCount, "Exactly one path should be taken");
		}

		[Test]
		public void RandomBranch3State_SetsSelectedPath_Correctly()
		{
			// Test with only Path A available
			var state = new RandomBranch3State();
			state.WeightA = 1f;
			state.WeightB = 0f;
			state.WeightC = 0f;

			state.OnEnter();

			Assert.AreEqual(0, state.SelectedPath, "SelectedPath should be 0 for Path A");
		}

		[Test]
		public void RandomBranch3State_SelectsPathB_WhenOnlyBHasWeight()
		{
			var state = new RandomBranch3State();
			state.WeightA = 0f;
			state.WeightB = 1f;
			state.WeightC = 0f;
			var pathBCalled = false;
			state.OnPathB = () => pathBCalled = true;

			state.OnEnter();

			Assert.IsTrue(pathBCalled);
			Assert.AreEqual(1, state.SelectedPath);
		}

		[Test]
		public void RandomBranch3State_SelectsPathC_WhenOnlyCHasWeight()
		{
			var state = new RandomBranch3State();
			state.WeightA = 0f;
			state.WeightB = 0f;
			state.WeightC = 1f;
			var pathCCalled = false;
			state.OnPathC = () => pathCCalled = true;

			state.OnEnter();

			Assert.IsTrue(pathCCalled);
			Assert.AreEqual(2, state.SelectedPath);
		}



		[Test]
		public void RandomBranch3State_WhenAllWeightsAreZero_DefaultsToPathA()
		{
			var state = new RandomBranch3State();
			state.WeightA = 0f;
			state.WeightB = 0f;
			state.WeightC = 0f;
			var pathACalled = false;
			state.OnPathA = () => pathACalled = true;

			state.OnEnter();

			Assert.IsTrue(pathACalled, "Should default to Path A when all weights are zero");
			Assert.AreEqual(0, state.SelectedPath);
		}

		[Test]
		public void RandomBranch3State_WithNullTransitions_DoesNotThrow()
		{
			var state = new RandomBranch3State();
			state.WeightA = 1f;
			state.WeightB = 1f;
			state.WeightC = 1f;
			state.OnPathA = null;
			state.OnPathB = null;
			state.OnPathC = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void RandomBranch3State_DefaultWeights_AreEquallyDistributed()
		{
			var state = new RandomBranch3State();

			Assert.AreEqual(1f, state.WeightA);
			Assert.AreEqual(1f, state.WeightB);
			Assert.AreEqual(1f, state.WeightC);
		}



		[Test]
		public void RandomBranch3State_WithEqualWeights_ProducesReasonableDistribution()
		{
			var pathCounts = new int[3];
			const int iterations = 1500;

			for (int i = 0; i < iterations; i++)
			{
				var state = new RandomBranch3State();
				state.WeightA = 1f;
				state.WeightB = 1f;
				state.WeightC = 1f;
				state.OnPathA = () => pathCounts[0]++;
				state.OnPathB = () => pathCounts[1]++;
				state.OnPathC = () => pathCounts[2]++;

				state.OnEnter();
			}

			// With equal weights, expect roughly 1/3 distribution
			var expectedCount = iterations / 3f;
			var tolerance = iterations * 0.1f;

			for (int i = 0; i < 3; i++)
			{
				Assert.That(pathCounts[i], Is.InRange((int)(expectedCount - tolerance), (int)(expectedCount + tolerance)),
					$"Path {(char)('A' + i)} count ({pathCounts[i]}) should be close to {expectedCount}");
			}
		}

		[Test]
		public void RandomBranch3State_WithWeightedDistribution_ProducesExpectedRatios()
		{
			var pathCounts = new int[3];
			const int iterations = 2000;

			// Weight ratio 2:1:1 - Path A should be selected twice as often
			for (int i = 0; i < iterations; i++)
			{
				var state = new RandomBranch3State();
				state.WeightA = 2f;
				state.WeightB = 1f;
				state.WeightC = 1f;
				state.OnPathA = () => pathCounts[0]++;
				state.OnPathB = () => pathCounts[1]++;
				state.OnPathC = () => pathCounts[2]++;

				state.OnEnter();
			}

			// Expected: A=50%, B=25%, C=25%
			var expectedA = iterations * 0.5f;
			var expectedBC = iterations * 0.25f;
			var tolerance = iterations * 0.1f;

			Assert.That(pathCounts[0], Is.InRange((int)(expectedA - tolerance), (int)(expectedA + tolerance)),
				$"Path A count ({pathCounts[0]}) should be close to {expectedA}");
		}

		[Test]
		public void RandomBranch3State_WithLargeWeights_WorksCorrectly()
		{
			var state = new RandomBranch3State();
			state.WeightA = 1000f;
			state.WeightB = 1000f;
			state.WeightC = 1000f;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void RandomBranch3State_WithVerySmallWeights_WorksCorrectly()
		{
			var state = new RandomBranch3State();
			state.WeightA = 0.001f;
			state.WeightB = 0.001f;
			state.WeightC = 0.001f;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}
}

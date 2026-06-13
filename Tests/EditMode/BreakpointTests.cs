using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BreakpointTests
	{
		[Test]
		public void Breakpoint_DefaultConstructor_GeneratesId()
		{
			var breakpoint = new Breakpoint();

			Assert.IsFalse(string.IsNullOrEmpty(breakpoint.Id));
		}

		[Test]
		public void Breakpoint_DefaultConstructor_IsEnabled()
		{
			var breakpoint = new Breakpoint();

			Assert.IsTrue(breakpoint.Enabled);
		}

		[Test]
		public void Breakpoint_DefaultConstructor_TypeIsOnEntry()
		{
			var breakpoint = new Breakpoint();

			Assert.AreEqual(BreakpointType.OnEntry, breakpoint.Type);
		}

		[Test]
		public void Breakpoint_NodeIdConstructor_SetsNodeId()
		{
			var breakpoint = new Breakpoint("node-123");

			Assert.AreEqual("node-123", breakpoint.NodeId);
			Assert.AreEqual(BreakpointType.OnEntry, breakpoint.Type);
		}

		[Test]
		public void Breakpoint_NodeIdConstructor_WithType_SetsType()
		{
			var breakpoint = new Breakpoint("node-123", BreakpointType.Conditional);

			Assert.AreEqual("node-123", breakpoint.NodeId);
			Assert.AreEqual(BreakpointType.Conditional, breakpoint.Type);
		}

		[Test]
		public void Breakpoint_HitCountDefaultsToZero()
		{
			var breakpoint = new Breakpoint();

			Assert.AreEqual(0, breakpoint.HitCount);
		}

		[Test]
		public void Breakpoint_ConditionDefaultsToNull()
		{
			var breakpoint = new Breakpoint();

			Assert.IsNull(breakpoint.Condition);
		}

		[Test]
		public void Breakpoint_SetProperties()
		{
			var breakpoint = new Breakpoint
			{
				NodeId = "test-node",
				Enabled = false,
				Type = BreakpointType.OnTransition,
				Condition = "Health < 10",
				TransitionPortName = "OnDeath",
				HitCount = 5
			};

			Assert.AreEqual("test-node", breakpoint.NodeId);
			Assert.IsFalse(breakpoint.Enabled);
			Assert.AreEqual(BreakpointType.OnTransition, breakpoint.Type);
			Assert.AreEqual("Health < 10", breakpoint.Condition);
			Assert.AreEqual("OnDeath", breakpoint.TransitionPortName);
			Assert.AreEqual(5, breakpoint.HitCount);
		}
	}

	[TestFixture]
	public class GraphViewDataBreakpointTests
	{
		private GraphViewData _data;

		[SetUp]
		public void SetUp()
		{
			_data = new GraphViewData();
		}

		[Test]
		public void Breakpoints_InitiallyEmpty()
		{
			Assert.AreEqual(0, _data.Breakpoints.Count);
		}

		[Test]
		public void AddBreakpoint_AddsToCollection()
		{
			var breakpoint = new Breakpoint("node-1");

			_data.AddBreakpoint(breakpoint);

			Assert.AreEqual(1, _data.Breakpoints.Count);
			Assert.AreEqual("node-1", _data.Breakpoints[0].NodeId);
		}

		[Test]
		public void AddBreakpoint_NullBreakpoint_DoesNotAdd()
		{
			_data.AddBreakpoint(null);

			Assert.AreEqual(0, _data.Breakpoints.Count);
		}

		[Test]
		public void AddBreakpoint_DuplicateId_UpdatesExisting()
		{
			var bp1 = new Breakpoint("node-1") { Enabled = true };
			var bp2 = new Breakpoint("node-2") { Enabled = false };
			bp2.Id = bp1.Id;

			_data.AddBreakpoint(bp1);
			_data.AddBreakpoint(bp2);

			Assert.AreEqual(1, _data.Breakpoints.Count);
			Assert.IsFalse(_data.Breakpoints[0].Enabled);
		}

		[Test]
		public void RemoveBreakpoint_RemovesById()
		{
			var breakpoint = new Breakpoint("node-1");
			_data.AddBreakpoint(breakpoint);

			_data.RemoveBreakpoint(breakpoint.Id);

			Assert.AreEqual(0, _data.Breakpoints.Count);
		}

		[Test]
		public void RemoveBreakpoint_NonexistentId_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _data.RemoveBreakpoint("nonexistent"));
		}

		[Test]
		public void RemoveBreakpointsForNode_RemovesAllForNode()
		{
			_data.AddBreakpoint(new Breakpoint("node-1"));
			_data.AddBreakpoint(new Breakpoint("node-2"));

			_data.RemoveBreakpointsForNode("node-1");

			Assert.AreEqual(1, _data.Breakpoints.Count);
			Assert.AreEqual("node-2", _data.Breakpoints[0].NodeId);
		}

		[Test]
		public void GetBreakpointForNode_ReturnsMatchingBreakpoint()
		{
			var breakpoint = new Breakpoint("node-1");
			_data.AddBreakpoint(breakpoint);

			var result = _data.GetBreakpointForNode("node-1");

			Assert.IsNotNull(result);
			Assert.AreEqual(breakpoint.Id, result.Id);
		}

		[Test]
		public void GetBreakpointForNode_NoMatch_ReturnsNull()
		{
			var result = _data.GetBreakpointForNode("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void HasEnabledBreakpoint_EnabledBreakpoint_ReturnsTrue()
		{
			_data.AddBreakpoint(new Breakpoint("node-1") { Enabled = true });

			Assert.IsTrue(_data.HasEnabledBreakpoint("node-1"));
		}

		[Test]
		public void HasEnabledBreakpoint_DisabledBreakpoint_ReturnsFalse()
		{
			_data.AddBreakpoint(new Breakpoint("node-1") { Enabled = false });

			Assert.IsFalse(_data.HasEnabledBreakpoint("node-1"));
		}

		[Test]
		public void HasEnabledBreakpoint_NoBreakpoint_ReturnsFalse()
		{
			Assert.IsFalse(_data.HasEnabledBreakpoint("node-1"));
		}

		[Test]
		public void ToggleBreakpoint_NoExisting_AddsBreakpoint()
		{
			var result = _data.ToggleBreakpoint("node-1");

			Assert.IsNotNull(result);
			Assert.AreEqual("node-1", result.NodeId);
			Assert.AreEqual(1, _data.Breakpoints.Count);
		}

		[Test]
		public void ToggleBreakpoint_Existing_RemovesBreakpoint()
		{
			_data.AddBreakpoint(new Breakpoint("node-1"));

			var result = _data.ToggleBreakpoint("node-1");

			Assert.IsNull(result);
			Assert.AreEqual(0, _data.Breakpoints.Count);
		}

		[Test]
		public void ClearAllBreakpoints_RemovesAll()
		{
			_data.AddBreakpoint(new Breakpoint("node-1"));
			_data.AddBreakpoint(new Breakpoint("node-2"));
			_data.AddBreakpoint(new Breakpoint("node-3"));

			_data.ClearAllBreakpoints();

			Assert.AreEqual(0, _data.Breakpoints.Count);
		}
	}

	[TestFixture]
	public class BreakpointTypeTests
	{
		[Test]
		public void BreakpointType_HasExpectedValues()
		{
			Assert.AreEqual(0, (int)BreakpointType.OnEntry);
			Assert.AreEqual(1, (int)BreakpointType.OnTransition);
			Assert.AreEqual(2, (int)BreakpointType.Conditional);
		}
	}
}

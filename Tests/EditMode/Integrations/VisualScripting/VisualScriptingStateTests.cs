using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.VisualScripting;
using Unity.VisualScripting;

namespace Nonatomic.VSM.Tests.VisualScripting
{
	[TestFixture]
	public class VisualScriptingStateAttributeTests
	{
		[Test]
		public void HasStateInfoAttribute()
		{
			var attr = typeof(VisualScriptingState).GetCustomAttribute<StateInfoAttribute>();

			Assert.IsNotNull(attr);
			Assert.IsFalse(string.IsNullOrEmpty(attr.Tooltip));
			Assert.IsFalse(string.IsNullOrEmpty(attr.Description));
		}

		[Test]
		public void HasStateStyleAttribute_WithPurpleColor()
		{
			var attr = typeof(VisualScriptingState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual(StateColors.Purple, attr.Color);
			Assert.AreEqual(NodeIcon.Command, attr.Icon);
		}

		[Test]
		public void HasGraphDataIn_Required()
		{
			var field = typeof(VisualScriptingState).GetField("Graph");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Graph", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasSyncBlackboardDataIn()
		{
			var field = typeof(VisualScriptingState).GetField("SyncBlackboard");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Sync Blackboard", attr.DisplayName);
		}

		[Test]
		public void HasOnDoneTransition()
		{
			var field = typeof(VisualScriptingState).GetField("OnDone");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void GraphField_IsScriptGraphAssetType()
		{
			var field = typeof(VisualScriptingState).GetField("Graph");

			Assert.IsNotNull(field);
			Assert.AreEqual(typeof(ScriptGraphAsset), field.FieldType);
		}
	}

	[TestFixture]
	public class VisualScriptingStateDefaultTests
	{
		[Test]
		public void SyncBlackboard_DefaultsToTrue()
		{
			var state = new VisualScriptingState();

			Assert.IsTrue(state.SyncBlackboard);
		}

		[Test]
		public void Graph_DefaultsToNull()
		{
			var state = new VisualScriptingState();

			Assert.IsNull(state.Graph);
		}

		[Test]
		public void OnDone_DefaultsToNull()
		{
			var state = new VisualScriptingState();

			Assert.IsNull(state.OnDone);
		}
	}

	[TestFixture]
	public class VisualScriptingStateNullSafetyTests
	{
		[Test]
		public void OnEnter_NullGraph_InvokesOnDone()
		{
			var state = new VisualScriptingState { Graph = null };
			var transitionInvoked = false;
			state.OnDone = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullGraphNullOnDone_DoesNotThrow()
		{
			var state = new VisualScriptingState
			{
				Graph = null,
				OnDone = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnUpdate_BeforeEnter_DoesNotThrow()
		{
			var state = new VisualScriptingState();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void OnExit_BeforeEnter_DoesNotThrow()
		{
			var state = new VisualScriptingState();

			Assert.DoesNotThrow(() => state.OnExit());
		}
	}
}

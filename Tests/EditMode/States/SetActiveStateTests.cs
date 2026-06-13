using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class SetActiveStateTests
	{
		[Test]
		public void Target_Default_IsNull()
		{
			var state = new SetActiveState();

			Assert.IsNull(state.Target);
		}

		[Test]
		public void Active_Default_IsTrue()
		{
			var state = new SetActiveState();

			Assert.IsTrue(state.Active);
		}

		[Test]
		public void OnComplete_Default_IsNull()
		{
			var state = new SetActiveState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void OnEnter_WithTarget_SetsTargetActive()
		{
			var target = new GameObject("Target");
			target.SetActive(false);
			try
			{
				var state = new SetActiveState
				{
					Target = target,
					Active = true,
					OnComplete = () => { }
				};

				state.OnEnter();

				Assert.IsTrue(target.activeSelf);
			}
			finally
			{
				Object.DestroyImmediate(target);
			}
		}

		[Test]
		public void OnEnter_WithTarget_SetsTargetInactive()
		{
			var target = new GameObject("Target");
			target.SetActive(true);
			try
			{
				var state = new SetActiveState
				{
					Target = target,
					Active = false,
					OnComplete = () => { }
				};

				state.OnEnter();

				Assert.IsFalse(target.activeSelf);
			}
			finally
			{
				Object.DestroyImmediate(target);
			}
		}

		[Test]
		public void OnEnter_WithTarget_CallsOnComplete()
		{
			var target = new GameObject("Target");
			try
			{
				var completeCalled = false;
				var state = new SetActiveState
				{
					Target = target,
					OnComplete = () => completeCalled = true
				};

				state.OnEnter();

				Assert.IsTrue(completeCalled);
			}
			finally
			{
				Object.DestroyImmediate(target);
			}
		}

		[Test]
		public void OnEnter_NoTargetWithContext_UsesContextGameObject()
		{
			var go = new GameObject("ContextObject");
			go.SetActive(false);
			try
			{
				var context = new StateMachineContext(go);
				var state = new SetActiveState
				{
					Target = null,
					Active = true,
					OnComplete = () => { }
				};
				state.Context = context;

				state.OnEnter();

				Assert.IsTrue(go.activeSelf);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void OnEnter_NoTargetNoContext_LogsWarning()
		{
			var state = new SetActiveState
			{
				Target = null,
				OnComplete = () => { }
			};

			LogAssert.Expect(LogType.Warning,
				"[SetActiveState] No target GameObject specified and no context GameObject available");
			state.OnEnter();
		}

		[Test]
		public void OnEnter_NoTargetNoContext_StillCallsOnComplete()
		{
			var completeCalled = false;
			var state = new SetActiveState
			{
				Target = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning,
				"[SetActiveState] No target GameObject specified and no context GameObject available");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullOnComplete_DoesNotThrow()
		{
			var target = new GameObject("Target");
			try
			{
				var state = new SetActiveState
				{
					Target = target,
					OnComplete = null
				};

				Assert.DoesNotThrow(() => state.OnEnter());
			}
			finally
			{
				Object.DestroyImmediate(target);
			}
		}

		[Test]
		public void OnEnter_TargetTakesPriorityOverContext()
		{
			var contextGo = new GameObject("Context");
			var targetGo = new GameObject("Target");
			targetGo.SetActive(false);
			try
			{
				var context = new StateMachineContext(contextGo);
				var state = new SetActiveState
				{
					Target = targetGo,
					Active = true,
					OnComplete = () => { }
				};
				state.Context = context;

				state.OnEnter();

				Assert.IsTrue(targetGo.activeSelf);
			}
			finally
			{
				Object.DestroyImmediate(contextGo);
				Object.DestroyImmediate(targetGo);
			}
		}

		[Test]
		public void OnEnter_ActiveTrue_OnAlreadyActiveTarget_RemainsActive()
		{
			var target = new GameObject("Target");
			target.SetActive(true);
			try
			{
				var state = new SetActiveState
				{
					Target = target,
					Active = true,
					OnComplete = () => { }
				};

				state.OnEnter();

				Assert.IsTrue(target.activeSelf);
			}
			finally
			{
				Object.DestroyImmediate(target);
			}
		}
	}
}

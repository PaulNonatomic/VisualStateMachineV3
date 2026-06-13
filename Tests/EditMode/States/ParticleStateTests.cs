using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class PlayParticleStateTests
	{

		[Test]
		public void DefaultParticleSystem_IsNull()
		{
			var state = new PlayParticleState();

			Assert.IsNull(state.ParticleSystem);
		}

		[Test]
		public void DefaultWithChildren_IsTrue()
		{
			var state = new PlayParticleState();

			Assert.IsTrue(state.WithChildren);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new PlayParticleState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(PlayParticleState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void ParticleSystem_HasDataInAttribute()
		{
			var field = typeof(PlayParticleState).GetField("ParticleSystem");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void WithChildren_HasDataInAttribute()
		{
			var field = typeof(PlayParticleState).GetField("WithChildren");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayParticleState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullParticleSystem_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayParticleState
			{
				ParticleSystem = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayParticleState] No ParticleSystem found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullParticleSystemNullCallback_DoesNotThrow()
		{
			var state = new PlayParticleState
			{
				ParticleSystem = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayParticleState] No ParticleSystem found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void WithChildren_CanBeSet()
		{
			var state = new PlayParticleState();

			state.WithChildren = false;

			Assert.IsFalse(state.WithChildren);
		}

	}

	[TestFixture]
	public class PlayParticleAndWaitStateTests
	{

		[Test]
		public void DefaultParticleSystem_IsNull()
		{
			var state = new PlayParticleAndWaitState();

			Assert.IsNull(state.ParticleSystem);
		}

		[Test]
		public void DefaultWithChildren_IsTrue()
		{
			var state = new PlayParticleAndWaitState();

			Assert.IsTrue(state.WithChildren);
		}

		[Test]
		public void DefaultDuration_IsZero()
		{
			var state = new PlayParticleAndWaitState();

			Assert.AreEqual(0f, state.Duration, 0.001f);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new PlayParticleAndWaitState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(PlayParticleAndWaitState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void ParticleSystem_HasDataInAttribute()
		{
			var field = typeof(PlayParticleAndWaitState).GetField("ParticleSystem");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void WithChildren_HasDataInAttribute()
		{
			var field = typeof(PlayParticleAndWaitState).GetField("WithChildren");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void Duration_HasDataOutAttribute()
		{
			var field = typeof(PlayParticleAndWaitState).GetField("Duration");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayParticleAndWaitState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullParticleSystem_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayParticleAndWaitState
			{
				ParticleSystem = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayParticleAndWaitState] No ParticleSystem found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullParticleSystemNullCallback_DoesNotThrow()
		{
			var state = new PlayParticleAndWaitState
			{
				ParticleSystem = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayParticleAndWaitState] No ParticleSystem found");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class StopParticleStateTests
	{

		[Test]
		public void DefaultParticleSystem_IsNull()
		{
			var state = new StopParticleState();

			Assert.IsNull(state.ParticleSystem);
		}

		[Test]
		public void DefaultWithChildren_IsTrue()
		{
			var state = new StopParticleState();

			Assert.IsTrue(state.WithChildren);
		}

		[Test]
		public void DefaultStopBehavior_IsStopEmitting()
		{
			var state = new StopParticleState();

			Assert.AreEqual(ParticleSystemStopBehavior.StopEmitting, state.StopBehavior);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new StopParticleState();

			Assert.IsNull(state.OnComplete);
		}



		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(StopParticleState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Violet, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon);
		}

		[Test]
		public void ParticleSystem_HasDataInAttribute()
		{
			var field = typeof(StopParticleState).GetField("ParticleSystem");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void WithChildren_HasDataInAttribute()
		{
			var field = typeof(StopParticleState).GetField("WithChildren");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void StopBehavior_HasDataInAttribute()
		{
			var field = typeof(StopParticleState).GetField("StopBehavior");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(StopParticleState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}



		[Test]
		public void OnEnter_NullParticleSystemNoContext_StillCallsOnComplete()
		{
			var completeCalled = false;
			var state = new StopParticleState
			{
				ParticleSystem = null,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullParticleSystemNullCallback_DoesNotThrow()
		{
			var state = new StopParticleState
			{
				ParticleSystem = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}


	[TestFixture]
	public class AllParticleStateTests
	{
		[Test]
		public void AllParticleStates_HaveVioletColor()
		{
			var stateTypes = new[]
			{
				typeof(PlayParticleState),
				typeof(PlayParticleAndWaitState),
				typeof(StopParticleState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.Violet, stateStyle.Color,
					$"{stateType.Name} should have Violet color");
			}
		}

		[Test]
		public void AllParticleStates_HaveSparksIcon()
		{
			var stateTypes = new[]
			{
				typeof(PlayParticleState),
				typeof(PlayParticleAndWaitState),
				typeof(StopParticleState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(NodeIcon.Sparks, stateStyle.Icon,
					$"{stateType.Name} should have Sparks icon");
			}
		}

		[Test]
		public void AllParticleStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(PlayParticleState),
				typeof(PlayParticleAndWaitState),
				typeof(StopParticleState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
				Assert.IsNotNull(stateInfo, $"{stateType.Name} should have StateInfoAttribute");
				Assert.IsFalse(string.IsNullOrEmpty(stateInfo.Tooltip),
					$"{stateType.Name} should have a tooltip");
			}
		}
	}

}

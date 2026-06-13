using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.Playables;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class PlayTimelineStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(PlayTimelineState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(PlayTimelineState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Pink, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Film, stateStyle.Icon);
		}

		[Test]
		public void HasAssetDataIn_Required()
		{
			var field = typeof(PlayTimelineState).GetField("Asset");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Asset", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasDirectorDataIn()
		{
			var field = typeof(PlayTimelineState).GetField("Director");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Director", attr.DisplayName);
		}

		[Test]
		public void HasWrapModeDataIn()
		{
			var field = typeof(PlayTimelineState).GetField("WrapMode");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Wrap Mode", attr.DisplayName);
		}

		[Test]
		public void HasSpeedDataIn()
		{
			var field = typeof(PlayTimelineState).GetField("Speed");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Speed", attr.DisplayName);
		}

		[Test]
		public void HasElapsedTimeDataOut()
		{
			var field = typeof(PlayTimelineState).GetField("ElapsedTime");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Elapsed Time", attr.DisplayName);
		}

		[Test]
		public void HasProgressDataOut()
		{
			var field = typeof(PlayTimelineState).GetField("Progress");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Progress", attr.DisplayName);
		}

		[Test]
		public void HasOnCompleteTransition()
		{
			var field = typeof(PlayTimelineState).GetField("OnComplete");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Complete", attr.DisplayName);
		}

		[Test]
		public void WrapModeDefaultsToNone()
		{
			var state = new PlayTimelineState();

			Assert.AreEqual(DirectorWrapMode.None, state.WrapMode);
		}

		[Test]
		public void SpeedDefaultsToOne()
		{
			var state = new PlayTimelineState();

			Assert.AreEqual(1f, state.Speed);
		}

		[Test]
		public void AssetDefaultsToNull()
		{
			var state = new PlayTimelineState();

			Assert.IsNull(state.Asset);
		}

		[Test]
		public void OnEnter_NullAsset_InvokesOnComplete()
		{
			var state = new PlayTimelineState { Asset = null };
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullAssetNullOnComplete_DoesNotThrow()
		{
			var state = new PlayTimelineState
			{
				Asset = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnUpdate_BeforeEnter_DoesNotThrow()
		{
			var state = new PlayTimelineState();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void OnExit_BeforeEnter_DoesNotThrow()
		{
			var state = new PlayTimelineState();

			Assert.DoesNotThrow(() => state.OnExit());
		}
	}

	[TestFixture]
	public class PauseTimelineStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(PauseTimelineState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(PauseTimelineState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Pink, stateStyle.Color);
		}

		[Test]
		public void HasDirectorDataIn_Required()
		{
			var field = typeof(PauseTimelineState).GetField("Director");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Director", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasOnCompleteTransition()
		{
			var field = typeof(PauseTimelineState).GetField("OnComplete");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void OnEnter_NullDirector_InvokesOnComplete()
		{
			var state = new PauseTimelineState { Director = null };
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullDirectorNullOnComplete_DoesNotThrow()
		{
			var state = new PauseTimelineState
			{
				Director = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class ResumeTimelineStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(ResumeTimelineState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(ResumeTimelineState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Pink, stateStyle.Color);
		}

		[Test]
		public void HasDirectorDataIn_Required()
		{
			var field = typeof(ResumeTimelineState).GetField("Director");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Director", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasSpeedDataIn()
		{
			var field = typeof(ResumeTimelineState).GetField("Speed");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Speed", attr.DisplayName);
		}

		[Test]
		public void SpeedDefaultsToOne()
		{
			var state = new ResumeTimelineState();

			Assert.AreEqual(1f, state.Speed);
		}

		[Test]
		public void HasOnCompleteTransition()
		{
			var field = typeof(ResumeTimelineState).GetField("OnComplete");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void OnEnter_NullDirector_InvokesOnComplete()
		{
			var state = new ResumeTimelineState { Director = null };
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullDirectorNullOnComplete_DoesNotThrow()
		{
			var state = new ResumeTimelineState
			{
				Director = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class StopTimelineStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(StopTimelineState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(StopTimelineState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Pink, stateStyle.Color);
		}

		[Test]
		public void HasDirectorDataIn_Required()
		{
			var field = typeof(StopTimelineState).GetField("Director");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Director", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasOnCompleteTransition()
		{
			var field = typeof(StopTimelineState).GetField("OnComplete");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void OnEnter_NullDirector_InvokesOnComplete()
		{
			var state = new StopTimelineState { Director = null };
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullDirectorNullOnComplete_DoesNotThrow()
		{
			var state = new StopTimelineState
			{
				Director = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class WaitForPlaybackTimeStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(WaitForPlaybackTimeState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(WaitForPlaybackTimeState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Pink, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Clock, stateStyle.Icon);
		}

		[Test]
		public void HasDirectorDataIn_Required()
		{
			var field = typeof(WaitForPlaybackTimeState).GetField("Director");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Director", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasTargetTimeDataIn()
		{
			var field = typeof(WaitForPlaybackTimeState).GetField("TargetTime");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Target Time", attr.DisplayName);
		}

		[Test]
		public void HasCurrentTimeDataOut()
		{
			var field = typeof(WaitForPlaybackTimeState).GetField("CurrentTime");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Current Time", attr.DisplayName);
		}

		[Test]
		public void HasOnReachedTransition()
		{
			var field = typeof(WaitForPlaybackTimeState).GetField("OnReached");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Reached", attr.DisplayName);
		}

		[Test]
		public void OnEnter_NullDirector_InvokesOnReached()
		{
			var state = new WaitForPlaybackTimeState { Director = null };
			var transitionInvoked = false;
			state.OnReached = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void OnEnter_NullDirectorNullOnReached_DoesNotThrow()
		{
			var state = new WaitForPlaybackTimeState
			{
				Director = null,
				OnReached = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnUpdate_BeforeEnter_DoesNotThrow()
		{
			var state = new WaitForPlaybackTimeState();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}
	}

	[TestFixture]
	public class TimelineStatesCommonTests
	{
		[Test]
		public void AllTimelineStates_HavePinkColor()
		{
			var stateTypes = new[]
			{
				typeof(PlayTimelineState),
				typeof(PauseTimelineState),
				typeof(ResumeTimelineState),
				typeof(StopTimelineState),
				typeof(WaitForPlaybackTimeState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.Pink, stateStyle.Color,
					$"{stateType.Name} should have Pink color");
			}
		}

		[Test]
		public void AllTimelineStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(PlayTimelineState),
				typeof(PauseTimelineState),
				typeof(ResumeTimelineState),
				typeof(StopTimelineState),
				typeof(WaitForPlaybackTimeState)
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

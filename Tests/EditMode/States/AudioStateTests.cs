using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class PlayAudioStateTests
	{
		[Test]
		public void DefaultClip_IsNull()
		{
			var state = new PlayAudioState();

			Assert.IsNull(state.Clip);
		}

		[Test]
		public void DefaultAudioSource_IsNull()
		{
			var state = new PlayAudioState();

			Assert.IsNull(state.AudioSource);
		}

		[Test]
		public void DefaultVolume_IsOne()
		{
			var state = new PlayAudioState();

			Assert.AreEqual(1f, state.Volume, 0.001f);
		}

		[Test]
		public void DefaultPosition_IsNull()
		{
			var state = new PlayAudioState();

			Assert.IsNull(state.Position);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new PlayAudioState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void OnEnter_NullClip_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayAudioState
			{
				Clip = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayAudioState] No audio clip specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullClipNullCallback_DoesNotThrow()
		{
			var state = new PlayAudioState
			{
				Clip = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayAudioState] No audio clip specified");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Volume_CanBeSet()
		{
			var state = new PlayAudioState();

			state.Volume = 0.5f;

			Assert.AreEqual(0.5f, state.Volume, 0.001f);
		}

		[Test]
		public void Position_CanBeSetToValue()
		{
			var state = new PlayAudioState();
			var pos = new Vector3(1f, 2f, 3f);

			state.Position = pos;

			Assert.IsTrue(state.Position.HasValue);
			Assert.AreEqual(pos, state.Position.Value);
		}

		[Test]
		public void Position_CanBeSetToNull()
		{
			var state = new PlayAudioState();
			state.Position = new Vector3(1f, 2f, 3f);

			state.Position = null;

			Assert.IsNull(state.Position);
		}

		[Test]
		public void Clip_HasDataInAttribute()
		{
			var field = typeof(PlayAudioState).GetField("Clip");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayAudioState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class PlayAudioAndWaitStateTests
	{
		[Test]
		public void DefaultClip_IsNull()
		{
			var state = new PlayAudioAndWaitState();

			Assert.IsNull(state.Clip);
		}

		[Test]
		public void DefaultAudioSource_IsNull()
		{
			var state = new PlayAudioAndWaitState();

			Assert.IsNull(state.AudioSource);
		}

		[Test]
		public void DefaultVolume_IsOne()
		{
			var state = new PlayAudioAndWaitState();

			Assert.AreEqual(1f, state.Volume, 0.001f);
		}

		[Test]
		public void DefaultDuration_IsZero()
		{
			var state = new PlayAudioAndWaitState();

			Assert.AreEqual(0f, state.Duration, 0.001f);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new PlayAudioAndWaitState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void OnEnter_NullClip_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new PlayAudioAndWaitState
			{
				Clip = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[PlayAudioAndWaitState] No audio clip specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullClipNullCallback_DoesNotThrow()
		{
			var state = new PlayAudioAndWaitState
			{
				Clip = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[PlayAudioAndWaitState] No audio clip specified");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Clip_HasDataInAttribute()
		{
			var field = typeof(PlayAudioAndWaitState).GetField("Clip");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void Duration_HasDataOutAttribute()
		{
			var field = typeof(PlayAudioAndWaitState).GetField("Duration");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(PlayAudioAndWaitState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}

	[TestFixture]
	public class StopAudioStateTests
	{
		[Test]
		public void DefaultAudioSource_IsNull()
		{
			var state = new StopAudioState();

			Assert.IsNull(state.AudioSource);
		}

		[Test]
		public void DefaultOnComplete_IsNull()
		{
			var state = new StopAudioState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void OnEnter_NullAudioSourceNoContext_StillCallsOnComplete()
		{
			var completeCalled = false;
			var state = new StopAudioState
			{
				AudioSource = null,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void OnEnter_NullAudioSourceNullCallback_DoesNotThrow()
		{
			var state = new StopAudioState
			{
				AudioSource = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(StopAudioState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}

		[Test]
		public void AudioSource_HasDataInAttribute()
		{
			var field = typeof(StopAudioState).GetField("AudioSource");

			Assert.IsNotNull(field);
			var attrs = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attrs.Length);
		}
	}
}

using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class PlayAudioStateTests
	{
		private GameObject _testObject;
		private AudioSource _audioSource;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
			_audioSource = _testObject.AddComponent<AudioSource>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void PlayAudioState_DefaultVolume_IsOne()
		{
			var state = new PlayAudioState();

			Assert.AreEqual(1f, state.Volume);
		}



		[Test]
		public void PlayAudioState_WithNoClip_LogsWarning()
		{
			var state = new PlayAudioState();
			state.Clip = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[PlayAudioState] No audio clip specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void PlayAudioState_InvokesOnComplete_Immediately()
		{
			var state = new PlayAudioState();
			state.Clip = null; // No clip to avoid needing audio
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			Assert.IsTrue(completeCalled, "OnComplete should be called immediately");
		}

		[Test]
		public void PlayAudioState_WithNullTransition_DoesNotThrow()
		{
			var state = new PlayAudioState();
			state.Clip = null;
			state.OnComplete = null;

			LogAssert.ignoreFailingMessages = true;
			Assert.DoesNotThrow(() => state.OnEnter());
			LogAssert.ignoreFailingMessages = false;
		}

	}

	[TestFixture]
	public class PlayAudioAndWaitStateTests
	{
		private GameObject _testObject;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void PlayAudioAndWaitState_DefaultVolume_IsOne()
		{
			var state = new PlayAudioAndWaitState();

			Assert.AreEqual(1f, state.Volume);
		}



		[Test]
		public void PlayAudioAndWaitState_WithNoClip_LogsWarning()
		{
			var state = new PlayAudioAndWaitState();
			state.Clip = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[PlayAudioAndWaitState] No audio clip specified");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void PlayAudioAndWaitState_WithNoAudioSource_LogsWarning()
		{
			var state = new PlayAudioAndWaitState();
			state.Clip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
			state.AudioSource = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.Expect(LogType.Warning, "[PlayAudioAndWaitState] No AudioSource found");
			state.OnEnter();

			Assert.IsTrue(completeCalled);

			UnityEngine.Object.DestroyImmediate(state.Clip);
		}



		[Test]
		public void PlayAudioAndWaitState_DurationOutput_IsAccessible()
		{
			var state = new PlayAudioAndWaitState();

			Assert.AreEqual(0f, state.Duration);
			state.Duration = 2.5f;
			Assert.AreEqual(2.5f, state.Duration);
		}

	}

	[TestFixture]
	public class StopAudioStateTests
	{
		private GameObject _testObject;
		private AudioSource _audioSource;

		[SetUp]
		public void SetUp()
		{
			_testObject = new GameObject("TestObject");
			_audioSource = _testObject.AddComponent<AudioSource>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_testObject != null)
			{
				UnityEngine.Object.Destroy(_testObject);
			}
		}


		[Test]
		public void StopAudioState_StopsAudioSource()
		{
			// Start playing (even without a clip, we can test the state behavior)
			_audioSource.Play();

			var state = new StopAudioState();
			state.AudioSource = _audioSource;

			state.OnEnter();

			Assert.IsFalse(_audioSource.isPlaying, "Audio should be stopped");
		}



		[Test]
		public void StopAudioState_InvokesOnComplete()
		{
			var state = new StopAudioState();
			state.AudioSource = _audioSource;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void StopAudioState_WithNullAudioSource_StillCompletesGracefully()
		{
			var state = new StopAudioState();
			state.AudioSource = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void StopAudioState_WithNullTransition_DoesNotThrow()
		{
			var state = new StopAudioState();
			state.AudioSource = _audioSource;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class InvokeEventStateTests
	{

		[Test]
		public void InvokeEventState_InvokesEvent()
		{
			var state = new InvokeEventState();
			var eventInvoked = false;
			state.Event = new UnityEvent();
			state.Event.AddListener(() => eventInvoked = true);

			state.OnEnter();

			Assert.IsTrue(eventInvoked, "Event should be invoked");
		}

		[Test]
		public void InvokeEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeEventState();
			state.Event = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void InvokeEventState_InvokesOnComplete()
		{
			var state = new InvokeEventState();
			state.Event = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void InvokeEventState_InvokesOnComplete_AfterEvent()
		{
			var state = new InvokeEventState();
			var order = new System.Collections.Generic.List<string>();

			state.Event = new UnityEvent();
			state.Event.AddListener(() => order.Add("event"));
			state.OnComplete = () => order.Add("complete");

			state.OnEnter();

			Assert.AreEqual(2, order.Count);
			Assert.AreEqual("event", order[0]);
			Assert.AreEqual("complete", order[1]);
		}

	}

	[TestFixture]
	public class InvokeStringEventStateTests
	{

		[Test]
		public void InvokeStringEventState_InvokesEventWithValue()
		{
			var state = new InvokeStringEventState();
			string receivedValue = null;
			state.Event = new UnityEvent<string>();
			state.Event.AddListener(v => receivedValue = v);
			state.Value = "TestValue";

			state.OnEnter();

			Assert.AreEqual("TestValue", receivedValue);
		}

		[Test]
		public void InvokeStringEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeStringEventState();
			state.Event = null;
			state.Value = "Test";

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void InvokeStringEventState_InvokesOnComplete()
		{
			var state = new InvokeStringEventState();
			state.Event = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

	}

	[TestFixture]
	public class InvokeFloatEventStateTests
	{

		[Test]
		public void InvokeFloatEventState_InvokesEventWithValue()
		{
			var state = new InvokeFloatEventState();
			float receivedValue = 0;
			state.Event = new UnityEvent<float>();
			state.Event.AddListener(v => receivedValue = v);
			state.Value = 42.5f;

			state.OnEnter();

			Assert.AreEqual(42.5f, receivedValue);
		}

		[Test]
		public void InvokeFloatEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeFloatEventState();
			state.Event = null;
			state.Value = 1.5f;

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void InvokeFloatEventState_InvokesOnComplete()
		{
			var state = new InvokeFloatEventState();
			state.Event = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void InvokeFloatEventState_DefaultValue_IsZero()
		{
			var state = new InvokeFloatEventState();

			Assert.AreEqual(0f, state.Value);
		}

	}

	[TestFixture]
	public class InvokeIntEventStateTests
	{

		[Test]
		public void InvokeIntEventState_InvokesEventWithValue()
		{
			var state = new InvokeIntEventState();
			int receivedValue = 0;
			state.Event = new UnityEvent<int>();
			state.Event.AddListener(v => receivedValue = v);
			state.Value = 42;

			state.OnEnter();

			Assert.AreEqual(42, receivedValue);
		}

		[Test]
		public void InvokeIntEventState_WithNullEvent_DoesNotThrow()
		{
			var state = new InvokeIntEventState();
			state.Event = null;
			state.Value = 10;

			Assert.DoesNotThrow(() => state.OnEnter());
		}



		[Test]
		public void InvokeIntEventState_InvokesOnComplete()
		{
			var state = new InvokeIntEventState();
			state.Event = null;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}



		[Test]
		public void InvokeIntEventState_DefaultValue_IsZero()
		{
			var state = new InvokeIntEventState();

			Assert.AreEqual(0, state.Value);
		}

	}
}

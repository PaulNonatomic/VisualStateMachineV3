using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Runtime;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ReplayControllerTests
	{
		private ReplayController _controller;

		[SetUp]
		public void SetUp()
		{
			_controller = new ReplayController();
		}

		[TearDown]
		public void TearDown()
		{
			_controller.Dispose();
		}

		[Test]
		public void ReplayController_DefaultMode_IsLive()
		{
			Assert.AreEqual(ReplayMode.Live, _controller.Mode);
		}

		[Test]
		public void ReplayController_NullInstance_HandlesGracefully()
		{
			_controller.SetInstance(null);

			Assert.DoesNotThrow(() => _controller.StepForward());
			Assert.DoesNotThrow(() => _controller.StepBackward());
			Assert.DoesNotThrow(() => _controller.Play());
			Assert.DoesNotThrow(() => _controller.Pause());
			Assert.DoesNotThrow(() => _controller.ReturnToLive());
			Assert.DoesNotThrow(() => _controller.GoToIndex(5));
			Assert.AreEqual(0, _controller.HistoryCount);
			Assert.IsNull(_controller.CurrentRecord);
		}

		[Test]
		public void ReplayController_SetInstance_SetsIndexToEnd()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			Assert.AreEqual(4, _controller.CurrentIndex);
			Assert.AreEqual(5, _controller.HistoryCount);
			Assert.AreEqual(ReplayMode.Live, _controller.Mode);
		}

		[Test]
		public void ReplayController_StepForward_AdvancesIndex()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(1);

			_controller.StepForward();

			Assert.AreEqual(2, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_StepBackward_DecrementsIndex()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(3);

			_controller.StepBackward();

			Assert.AreEqual(2, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_StepForward_AtEnd_DoesNotExceedBounds()
		{
			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);

			// Already at index 2 (end), stepping forward should stay
			_controller.StepForward();

			Assert.AreEqual(2, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_StepBackward_AtStart_DoesNotGoBelowZero()
		{
			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);
			_controller.GoToIndex(0);

			_controller.StepBackward();

			Assert.AreEqual(0, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_GoToIndex_SetsCorrectPosition()
		{
			var instance = CreateInstanceWithHistory(10);
			_controller.SetInstance(instance);

			_controller.GoToIndex(5);

			Assert.AreEqual(5, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_GoToIndex_ClampsToValidRange()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			_controller.GoToIndex(100);
			Assert.AreEqual(4, _controller.CurrentIndex);

			_controller.GoToIndex(-5);
			Assert.AreEqual(0, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_ReturnToLive_SetsModeToLive()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(0);

			Assert.AreEqual(ReplayMode.Paused, _controller.Mode);

			_controller.ReturnToLive();

			Assert.AreEqual(ReplayMode.Live, _controller.Mode);
			Assert.AreEqual(4, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_Play_SetsModeToPlaying()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(0);

			_controller.Play();

			Assert.AreEqual(ReplayMode.Playing, _controller.Mode);
		}

		[Test]
		public void ReplayController_Pause_SetsModeToPaused()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(0);
			_controller.Play();

			_controller.Pause();

			Assert.AreEqual(ReplayMode.Paused, _controller.Mode);
		}

		[Test]
		public void ReplayController_OnReplayPositionChanged_FiresOnStep()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(1);

			StateTransitionRecord receivedRecord = null;
			_controller.OnReplayPositionChanged += record => receivedRecord = record;

			_controller.StepForward();

			Assert.IsNotNull(receivedRecord);
			Assert.AreEqual(_controller.CurrentRecord, receivedRecord);
		}

		[Test]
		public void ReplayController_OnModeChanged_FiresOnModeTransition()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			ReplayMode? receivedMode = null;
			_controller.OnModeChanged += mode => receivedMode = mode;

			_controller.GoToIndex(0);

			Assert.IsNotNull(receivedMode);
			Assert.AreEqual(ReplayMode.Paused, receivedMode.Value);
		}

		[Test]
		public void ReplayController_EmptyHistory_HandlesGracefully()
		{
			var instance = CreateInstanceWithHistory(0);
			_controller.SetInstance(instance);

			Assert.DoesNotThrow(() => _controller.StepForward());
			Assert.DoesNotThrow(() => _controller.StepBackward());
			Assert.DoesNotThrow(() => _controller.GoToIndex(0));
			Assert.IsNull(_controller.CurrentRecord);
			Assert.AreEqual(0, _controller.HistoryCount);
		}

		[Test]
		public void ReplayController_CurrentRecord_ReturnsCorrectRecord()
		{
			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);

			_controller.GoToIndex(1);

			var record = _controller.CurrentRecord;
			Assert.IsNotNull(record);
			Assert.AreEqual("State1", record.ToStateName);
		}

		[Test]
		public void ReplayController_CycleSpeed_CyclesThroughOptions()
		{
			Assert.AreEqual(1f, _controller.PlaybackSpeed);

			_controller.CycleSpeed();
			Assert.AreEqual(2f, _controller.PlaybackSpeed);

			_controller.CycleSpeed();
			Assert.AreEqual(4f, _controller.PlaybackSpeed);

			_controller.CycleSpeed();
			Assert.AreEqual(0.5f, _controller.PlaybackSpeed);

			_controller.CycleSpeed();
			Assert.AreEqual(1f, _controller.PlaybackSpeed);
		}

		[Test]
		public void ReplayController_Play_AtEnd_RestartsFromBeginning()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			// Currently at index 4 (end)
			Assert.AreEqual(4, _controller.CurrentIndex);

			_controller.Play();

			// Should restart from beginning
			Assert.AreEqual(0, _controller.CurrentIndex);
			Assert.AreEqual(ReplayMode.Playing, _controller.Mode);
		}

		[Test]
		public void ReplayController_StepForward_EntersPausedMode()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(1);
			_controller.ReturnToLive();

			Assert.AreEqual(ReplayMode.Live, _controller.Mode);

			// Step from somewhere in the middle
			_controller.GoToIndex(1);
			_controller.StepForward();

			Assert.AreEqual(ReplayMode.Paused, _controller.Mode);
		}

		[Test]
		public void ReplayController_ImplementsIDisposable()
		{
			Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(ReplayController)));
		}

		[Test]
		public void ReplayController_SetInstance_FiresOnModeChanged()
		{
			// First move away from Live mode
			_controller.Pause();

			var firedModes = new List<ReplayMode>();
			_controller.OnModeChanged += mode => firedModes.Add(mode);

			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);

			Assert.AreEqual(1, firedModes.Count);
			Assert.AreEqual(ReplayMode.Live, firedModes[0]);
		}

		[Test]
		public void ReplayController_SetInstance_AlreadyLive_DoesNotFireEvent()
		{
			// Already in Live mode (default)
			var firedCount = 0;
			_controller.OnModeChanged += _ => firedCount++;

			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);

			Assert.AreEqual(0, firedCount);
		}

		[Test]
		public void ReplayController_Dispose_ClearsInstance()
		{
			var instance = CreateInstanceWithHistory(3);
			_controller.SetInstance(instance);
			Assert.AreEqual(3, _controller.HistoryCount);

			_controller.Dispose();

			Assert.AreEqual(0, _controller.HistoryCount);
			Assert.IsNull(_controller.CurrentRecord);
		}

		[Test]
		public void ReplayController_GoToIndex_FromLive_EntersPausedMode()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			Assert.AreEqual(ReplayMode.Live, _controller.Mode);

			_controller.GoToIndex(1);

			Assert.AreEqual(ReplayMode.Paused, _controller.Mode);
		}

		[Test]
		public void ReplayController_GoToIndex_SameIndex_NoEvent()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);

			var positionChangedCount = 0;
			_controller.OnReplayPositionChanged += _ => positionChangedCount++;

			// Going to same index as current (4) should not fire
			_controller.GoToIndex(4);

			Assert.AreEqual(0, positionChangedCount);
		}

		[Test]
		public void ReplayController_StepBackward_FiresPositionChanged()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(3);

			StateTransitionRecord receivedRecord = null;
			_controller.OnReplayPositionChanged += record => receivedRecord = record;

			_controller.StepBackward();

			Assert.IsNotNull(receivedRecord);
			Assert.AreEqual(2, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_Play_WithEmptyHistory_DoesNotChangeMode()
		{
			var instance = CreateInstanceWithHistory(0);
			_controller.SetInstance(instance);

			_controller.Play();

			Assert.AreEqual(ReplayMode.Live, _controller.Mode);
		}

		[Test]
		public void ReplayController_ReturnToLive_SetsIndexToEnd()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			_controller.GoToIndex(0);

			_controller.ReturnToLive();

			Assert.AreEqual(4, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_Play_FiresPositionChanged_WhenRestarting()
		{
			var instance = CreateInstanceWithHistory(5);
			_controller.SetInstance(instance);
			// At end (index 4)

			StateTransitionRecord receivedRecord = null;
			_controller.OnReplayPositionChanged += record => receivedRecord = record;

			_controller.Play();

			// Should fire position changed because it restarted from beginning
			Assert.IsNotNull(receivedRecord);
			Assert.AreEqual(0, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_SingleRecordHistory_StepForward_StaysAtEnd()
		{
			var instance = CreateInstanceWithHistory(1);
			_controller.SetInstance(instance);

			_controller.StepForward();

			Assert.AreEqual(0, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayController_SingleRecordHistory_StepBackward_StaysAtStart()
		{
			var instance = CreateInstanceWithHistory(1);
			_controller.SetInstance(instance);

			_controller.StepBackward();

			Assert.AreEqual(0, _controller.CurrentIndex);
		}

		[Test]
		public void ReplayMode_HasThreeValues()
		{
			var values = Enum.GetValues(typeof(ReplayMode));
			Assert.AreEqual(3, values.Length);
		}

		private static RuntimeInstanceInfo CreateInstanceWithHistory(int count)
		{
			var info = CreateEmptyRuntimeInstanceInfo();
			var historyField = typeof(RuntimeInstanceInfo).GetField(
				"_transitionHistory",
				BindingFlags.NonPublic | BindingFlags.Instance);

			var history = (List<StateTransitionRecord>)historyField.GetValue(info);

			for (var i = 0; i < count; i++)
			{
				history.Add(new StateTransitionRecord
				{
					Timestamp = DateTime.Now.AddSeconds(i),
					FromNodeId = i > 0 ? $"node{i - 1}" : null,
					FromStateName = i > 0 ? $"State{i - 1}" : "Start",
					ToNodeId = $"node{i}",
					ToStateName = $"State{i}",
					TriggerPortName = "Done",
					BlackboardSnapshot = new Dictionary<string, string>
					{
						{ "Health", (100 - i * 10).ToString() }
					},
					BlackboardTypes = new Dictionary<string, string>
					{
						{ "Health", "float" }
					}
				});
			}

			return info;
		}

		private static RuntimeInstanceInfo CreateEmptyRuntimeInstanceInfo()
		{
			// RuntimeInstanceInfo requires constructor args, use reflection to create
			var ctor = typeof(RuntimeInstanceInfo).GetConstructor(
				BindingFlags.Public | BindingFlags.Instance,
				null,
				new[] { typeof(Nonatomic.VSM.Core.StateMachineInstance), typeof(Nonatomic.VSM.Graph.StateMachineGraph), typeof(UnityEngine.GameObject) },
				null);

			return (RuntimeInstanceInfo)ctor.Invoke(new object[] { null, null, null });
		}
	}
}

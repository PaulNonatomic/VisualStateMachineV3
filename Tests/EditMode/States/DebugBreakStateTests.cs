using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class DebugBreakStateTests
	{
		[SetUp]
		public void SetUp()
		{
			DebugBreakState.ResetBreakpoints();
		}

		[Test]
		public void DebugBreakState_DefaultMessage_IsDebugBreakTriggered()
		{
			var state = new DebugBreakState();

			Assert.AreEqual("Debug break triggered", state.Message);
		}

		[Test]
		public void DebugBreakState_DefaultBreakOnce_IsFalse()
		{
			var state = new DebugBreakState();

			Assert.IsFalse(state.BreakOnce);
		}

		[Test]
		public void DebugBreakState_DefaultOnContinue_IsNull()
		{
			var state = new DebugBreakState();

			Assert.IsNull(state.OnContinue);
		}

		[Test]
		public void DebugBreakState_InvokesOnContinue()
		{
			var continueCalled = false;
			var state = new DebugBreakState
			{
				Message = "",
				OnContinue = () => continueCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(continueCalled);
		}

		[Test]
		public void DebugBreakState_WithNullOnContinue_DoesNotThrow()
		{
			var state = new DebugBreakState
			{
				Message = "",
				OnContinue = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void DebugBreakState_WithEmptyMessage_DoesNotLog()
		{
			var logReceived = false;
			var state = new DebugBreakState
			{
				Message = ""
			};

			Application.logMessageReceived += OnLog;
			state.OnEnter();
			Application.logMessageReceived -= OnLog;

			Assert.IsFalse(logReceived, "Empty message should not produce log output");

			void OnLog(string message, string stackTrace, LogType type)
			{
				if (type == LogType.Log && message.Contains("DebugBreak"))
				{
					logReceived = true;
				}
			}
		}

		[Test]
		public void DebugBreakState_WithNullMessage_DoesNotLog()
		{
			var logReceived = false;
			var state = new DebugBreakState
			{
				Message = null
			};

			Application.logMessageReceived += OnLog;
			state.OnEnter();
			Application.logMessageReceived -= OnLog;

			Assert.IsFalse(logReceived, "Null message should not produce log output");

			void OnLog(string message, string stackTrace, LogType type)
			{
				if (type == LogType.Log && message.Contains("DebugBreak"))
				{
					logReceived = true;
				}
			}
		}

		[Test]
		public void DebugBreakState_WithMessage_LogsMessage()
		{
			var state = new DebugBreakState
			{
				Message = "Test break"
			};

			LogAssert.Expect(LogType.Log, "[DebugBreak] Test break");
			state.OnEnter();
		}

		[Test]
		public void DebugBreakState_ResetBreakpoints_ClearsTriggeredSet()
		{
			// Just verify the static method doesn't throw
			Assert.DoesNotThrow(() => DebugBreakState.ResetBreakpoints());
		}

		[Test]
		public void DebugBreakState_ResetBreakpoints_CalledMultipleTimes_DoesNotThrow()
		{
			Assert.DoesNotThrow(() =>
			{
				DebugBreakState.ResetBreakpoints();
				DebugBreakState.ResetBreakpoints();
				DebugBreakState.ResetBreakpoints();
			});
		}
	}
}

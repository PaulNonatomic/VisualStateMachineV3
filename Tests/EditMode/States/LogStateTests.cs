using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class LogStateTests
	{
		[Test]
		public void LogState_DefaultMessage_IsStateReached()
		{
			var state = new LogState();

			Assert.AreEqual("State reached", state.Message);
		}

		[Test]
		public void LogState_DefaultLevel_IsLog()
		{
			var state = new LogState();

			Assert.AreEqual(LogLevel.Log, state.Level);
		}

		[Test]
		public void LogState_DefaultIncludeObjectName_IsTrue()
		{
			var state = new LogState();

			Assert.IsTrue(state.IncludeObjectName);
		}

		[Test]
		public void LogState_DefaultOnComplete_IsNull()
		{
			var state = new LogState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void LogState_WithLogLevel_LogsMessage()
		{
			var state = new LogState
			{
				Message = "Test message",
				Level = LogLevel.Log,
				IncludeObjectName = false
			};

			LogAssert.Expect(LogType.Log, "Test message");
			state.OnEnter();
		}

		[Test]
		public void LogState_WithWarningLevel_LogsWarning()
		{
			var state = new LogState
			{
				Message = "Warning message",
				Level = LogLevel.Warning,
				IncludeObjectName = false
			};

			LogAssert.Expect(LogType.Warning, "Warning message");
			state.OnEnter();
		}

		[Test]
		public void LogState_WithErrorLevel_LogsError()
		{
			var state = new LogState
			{
				Message = "Error message",
				Level = LogLevel.Error,
				IncludeObjectName = false
			};

			LogAssert.Expect(LogType.Error, "Error message");
			state.OnEnter();
		}

		[Test]
		public void LogState_WithIncludeObjectNameFalse_LogsPlainMessage()
		{
			var state = new LogState
			{
				Message = "Plain message",
				Level = LogLevel.Log,
				IncludeObjectName = false
			};

			LogAssert.Expect(LogType.Log, "Plain message");
			state.OnEnter();
		}

		[Test]
		public void LogState_WithIncludeObjectNameTrueAndNoContext_LogsPlainMessage()
		{
			var state = new LogState
			{
				Message = "No context message",
				Level = LogLevel.Log,
				IncludeObjectName = true
			};

			// When GameObject is null (no context), plain message is logged
			LogAssert.Expect(LogType.Log, "No context message");
			state.OnEnter();
		}

		[Test]
		public void LogState_InvokesOnComplete_AfterLogging()
		{
			var completeCalled = false;
			var state = new LogState
			{
				Message = "Test",
				Level = LogLevel.Log,
				IncludeObjectName = false,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Log, "Test");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void LogState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new LogState
			{
				Message = "Test",
				Level = LogLevel.Log,
				IncludeObjectName = false,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Log, "Test");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void LogState_WithEmptyMessage_LogsEmpty()
		{
			var state = new LogState
			{
				Message = "",
				Level = LogLevel.Log,
				IncludeObjectName = false
			};

			LogAssert.Expect(LogType.Log, "");
			state.OnEnter();
		}

		[Test]
		public void LogState_LoggedBeforeOnComplete()
		{
			var loggedFirst = false;
			var logSeen = false;

			var state = new LogState
			{
				Message = "Order test",
				Level = LogLevel.Log,
				IncludeObjectName = false,
				OnComplete = () => loggedFirst = logSeen
			};

			Application.logMessageReceived += OnLog;
			state.OnEnter();
			Application.logMessageReceived -= OnLog;

			Assert.IsTrue(loggedFirst, "Log should happen before OnComplete");

			void OnLog(string message, string stackTrace, LogType type)
			{
				if (message == "Order test") logSeen = true;
			}
		}
	}
}

using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class LogStatePlayModeTests
	{
		[UnityTest]
		public IEnumerator LogState_LogLevel_OutputsToDebugLog()
		{
			var state = new LogState();
			state.Message = "Test log message";
			state.Level = LogLevel.Log;
			state.IncludeObjectName = false;

			LogAssert.Expect(UnityEngine.LogType.Log, "Test log message");
			state.OnEnter();

			yield return null;
		}

		[UnityTest]
		public IEnumerator LogState_WarningLevel_OutputsToDebugLogWarning()
		{
			var state = new LogState();
			state.Message = "Test warning message";
			state.Level = LogLevel.Warning;
			state.IncludeObjectName = false;

			LogAssert.Expect(UnityEngine.LogType.Warning, "Test warning message");
			state.OnEnter();

			yield return null;
		}

		[UnityTest]
		public IEnumerator LogState_ErrorLevel_OutputsToDebugLogError()
		{
			var state = new LogState();
			state.Message = "Test error message";
			state.Level = LogLevel.Error;
			state.IncludeObjectName = false;

			LogAssert.Expect(UnityEngine.LogType.Error, "Test error message");
			state.OnEnter();

			yield return null;
		}

		[UnityTest]
		public IEnumerator LogState_InvokesOnComplete_AfterLogging()
		{
			var state = new LogState();
			state.Message = "Complete test";
			state.Level = LogLevel.Log;
			state.IncludeObjectName = false;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			LogAssert.ignoreFailingMessages = true;
			state.OnEnter();
			LogAssert.ignoreFailingMessages = false;

			yield return null;

			Assert.IsTrue(completeCalled, "OnComplete should be called after logging");
		}

		[UnityTest]
		public IEnumerator LogState_WithNullTransition_DoesNotThrow()
		{
			var state = new LogState();
			state.Message = "Null transition test";
			state.Level = LogLevel.Log;
			state.IncludeObjectName = false;
			state.OnComplete = null;

			LogAssert.Expect(UnityEngine.LogType.Log, "Null transition test");
			state.OnEnter();

			yield return null;
		}

		[UnityTest]
		public IEnumerator LogState_DefaultMessage_IsStateReached()
		{
			var state = new LogState();

			yield return null;

			Assert.AreEqual("State reached", state.Message);
		}

		[UnityTest]
		public IEnumerator LogState_DefaultLevel_IsLog()
		{
			var state = new LogState();

			yield return null;

			Assert.AreEqual(LogLevel.Log, state.Level);
		}

		[UnityTest]
		public IEnumerator LogState_DefaultIncludeObjectName_IsTrue()
		{
			var state = new LogState();

			yield return null;

			Assert.IsTrue(state.IncludeObjectName);
		}
	}
}

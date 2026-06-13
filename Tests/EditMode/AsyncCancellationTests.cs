using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class AsyncCancellationTests
	{

		private class CancellationAwareState : State
		{
			public CancellationToken ReceivedToken { get; private set; }
			public bool OnEnterAsyncCalled { get; private set; }
			public bool OnExitAsyncCalled { get; private set; }
			public bool OnUpdateAsyncCalled { get; private set; }

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				ReceivedToken = ct;
				OnEnterAsyncCalled = true;
				return Task.CompletedTask;
			}

			public override Task OnExitAsync(CancellationToken ct)
			{
				ReceivedToken = ct;
				OnExitAsyncCalled = true;
				return Task.CompletedTask;
			}

			public override Task OnUpdateAsync(CancellationToken ct)
			{
				ReceivedToken = ct;
				OnUpdateAsyncCalled = true;
				return Task.CompletedTask;
			}
		}

		private class LegacyAsyncState : State
		{
			public bool OnEnterAsyncCalled { get; private set; }
			public bool OnExitAsyncCalled { get; private set; }

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync()
			{
				OnEnterAsyncCalled = true;
				return Task.CompletedTask;
			}

			public override Task OnExitAsync()
			{
				OnExitAsyncCalled = true;
				return Task.CompletedTask;
			}
		}

		private class LongRunningAsyncState : State
		{
			public bool WasCancelled { get; private set; }
			public bool Completed { get; private set; }
			public TaskCompletionSource<bool> EnteredTcs { get; } = new TaskCompletionSource<bool>();

			[Transition("Done")] public Action done;

			public override async Task OnEnterAsync(CancellationToken ct)
			{
				EnteredTcs.TrySetResult(true);

				try
				{
					await Task.Delay(10000, ct);
					Completed = true;
				}
				catch (OperationCanceledException)
				{
					WasCancelled = true;
					throw;
				}
			}
		}

		private class ExceptionThrowingState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in OnEnterAsync");
			}
		}

		private class ExitExceptionState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				return Task.CompletedTask;
			}

			public override Task OnExitAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in OnExitAsync");
			}
		}

		private class UpdateExceptionState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				return Task.CompletedTask;
			}

			public override Task OnUpdateAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in OnUpdateAsync");
			}
		}



		[Test]
		public async Task OnEnterAsync_WithToken_ReceivesToken()
		{
			var state = new CancellationAwareState();
			using var cts = new CancellationTokenSource();

			await state.OnEnterAsync(cts.Token);

			Assert.IsTrue(state.OnEnterAsyncCalled);
			Assert.AreEqual(cts.Token, state.ReceivedToken);
		}

		[Test]
		public async Task OnExitAsync_WithToken_ReceivesToken()
		{
			var state = new CancellationAwareState();
			using var cts = new CancellationTokenSource();

			await state.OnExitAsync(cts.Token);

			Assert.IsTrue(state.OnExitAsyncCalled);
			Assert.AreEqual(cts.Token, state.ReceivedToken);
		}

		[Test]
		public async Task OnUpdateAsync_WithToken_ReceivesToken()
		{
			var state = new CancellationAwareState();
			using var cts = new CancellationTokenSource();

			await state.OnUpdateAsync(cts.Token);

			Assert.IsTrue(state.OnUpdateAsyncCalled);
			Assert.AreEqual(cts.Token, state.ReceivedToken);
		}



		[Test]
		public async Task OnEnterAsync_WithToken_DelegatesToParameterless_WhenNotOverridden()
		{
			var state = new LegacyAsyncState();

			await state.OnEnterAsync(CancellationToken.None);

			Assert.IsTrue(state.OnEnterAsyncCalled);
		}

		[Test]
		public async Task OnExitAsync_WithToken_DelegatesToParameterless_WhenNotOverridden()
		{
			var state = new LegacyAsyncState();

			await state.OnExitAsync(CancellationToken.None);

			Assert.IsTrue(state.OnExitAsyncCalled);
		}

		[Test]
		public async Task OnEnterAsync_Parameterless_StillWorks()
		{
			var state = new LegacyAsyncState();

			await state.OnEnterAsync();

			Assert.IsTrue(state.OnEnterAsyncCalled);
		}



		[Test]
		public async Task CancelledToken_CancelsAsyncOperation()
		{
			var state = new LongRunningAsyncState();
			using var cts = new CancellationTokenSource();

			var enterTask = state.OnEnterAsync(cts.Token);
			await state.EnteredTcs.Task;

			cts.Cancel();

			try
			{
				await enterTask;
			}
			catch (OperationCanceledException)
			{
				// Expected
			}

			Assert.IsTrue(state.WasCancelled);
			Assert.IsFalse(state.Completed);
		}

		[Test]
		public async Task PreCancelledToken_CancelsImmediately()
		{
			var state = new LongRunningAsyncState();
			using var cts = new CancellationTokenSource();
			cts.Cancel();

			try
			{
				await state.OnEnterAsync(cts.Token);
			}
			catch (OperationCanceledException)
			{
				// Expected
			}

			Assert.IsFalse(state.Completed);
		}



		private StateMachineGraph CreateTwoStateGraph()
		{
			var graph = UnityEngine.ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(CancellationAwareState).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(CancellationAwareState).AssemblyQualifiedName
			};

			graph.AddNode(entry);
			graph.AddNode(stateA);
			graph.AddNode(stateB);
			graph.EntryNodeId = "entry";

			graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "done",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			return graph;
		}

		[Test]
		public async Task StartAsync_ProvidesNonDefaultToken()
		{
			var graph = CreateTwoStateGraph();

			try
			{
				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var state = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(state);
				Assert.IsTrue(state.OnEnterAsyncCalled);
				Assert.IsTrue(state.ReceivedToken.CanBeCanceled);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public async Task StopAsync_CancelsInstanceToken()
		{
			var graph = CreateTwoStateGraph();

			try
			{
				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var state = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(state);
				var token = state.ReceivedToken;

				Assert.IsFalse(token.IsCancellationRequested);

				await instance.StopAsync();

				Assert.IsTrue(token.IsCancellationRequested);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public async Task Transition_CancelsOldStateToken()
		{
			var graph = CreateTwoStateGraph();

			try
			{
				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var stateA = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(stateA);
				var stateAToken = stateA.ReceivedToken;

				Assert.IsFalse(stateAToken.IsCancellationRequested);

				// Trigger transition to B via sync Action.Invoke() path
				stateA.done?.Invoke();

				// After transition, A's token should be cancelled
				Assert.IsTrue(stateAToken.IsCancellationRequested);

				// B should now be the current state
				Assert.IsInstanceOf<CancellationAwareState>(instance.CurrentState);
				Assert.AreNotSame(stateA, instance.CurrentState);

				await instance.StopAsync();
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public async Task ExternalToken_CancelsInstance()
		{
			var graph = CreateTwoStateGraph();

			try
			{
				using var externalCts = new CancellationTokenSource();
				var instance = new StateMachineInstance(graph);
				instance.SetExternalCancellationToken(externalCts.Token);

				await instance.StartAsync();

				var state = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(state);
				Assert.IsTrue(state.ReceivedToken.CanBeCanceled);

				externalCts.Cancel();

				Assert.IsTrue(state.ReceivedToken.IsCancellationRequested);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task LegacyState_WorksThroughAsyncPath()
		{
			var graph = UnityEngine.ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var entry = new EntryNode { Id = "entry" };
				var stateNode = new StateNode
				{
					Id = "stateA",
					StateTypeAssemblyQualifiedName = typeof(LegacyAsyncState).AssemblyQualifiedName
				};

				graph.AddNode(entry);
				graph.AddNode(stateNode);
				graph.EntryNodeId = "entry";

				graph.AddConnection(new Connection
				{
					Id = "c1",
					SourceNodeId = "entry",
					TargetNodeId = "stateA",
					ConnectionType = ConnectionType.Transition
				});

				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var state = instance.CurrentState as LegacyAsyncState;
				Assert.IsNotNull(state);
				Assert.IsTrue(state.OnEnterAsyncCalled);

				await instance.StopAsync();

				Assert.IsTrue(state.OnExitAsyncCalled);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task ExceptionInOnEnterAsync_IsHandledGracefully()
		{
			var graph = UnityEngine.ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var entry = new EntryNode { Id = "entry" };
				var stateNode = new StateNode
				{
					Id = "throwing",
					StateTypeAssemblyQualifiedName = typeof(ExceptionThrowingState).AssemblyQualifiedName
				};

				graph.AddNode(entry);
				graph.AddNode(stateNode);
				graph.EntryNodeId = "entry";

				graph.AddConnection(new Connection
				{
					Id = "c1",
					SourceNodeId = "entry",
					TargetNodeId = "throwing",
					ConnectionType = ConnectionType.Transition
				});

				var instance = new StateMachineInstance(graph);

				// Should not throw - exception should be caught and logged
				UnityEngine.TestTools.LogAssert.Expect(
					UnityEngine.LogType.Error,
					new System.Text.RegularExpressions.Regex("Exception in OnEnterAsync"));

				await instance.StartAsync();

				Assert.IsTrue(instance.IsRunning);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task ExceptionInOnExitAsync_IsHandledGracefully()
		{
			var graph = UnityEngine.ScriptableObject.CreateInstance<StateMachineGraph>();
			try
			{
				var stateANode = new StateNode { Id = "A", StateType = typeof(ExitExceptionState) };
				var stateBNode = new StateNode { Id = "B", StateType = typeof(CancellationAwareState) };
				var entryNode = new EntryNode { Id = "entry" };

				graph.AddNode(entryNode);
				graph.AddNode(stateANode);
				graph.AddNode(stateBNode);
				graph.EntryNodeId = "entry";
				graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "A", ConnectionType = ConnectionType.Transition });
				graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "A", SourcePortName = "done", TargetNodeId = "B", ConnectionType = ConnectionType.Transition });

				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var stateA = instance.CurrentState as ExitExceptionState;
				Assert.IsNotNull(stateA);

				// Transition should handle the exit exception gracefully
				UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Exception in OnExitAsync"));
				stateA.done?.Invoke();

				// Wait a frame for async transition
				await Task.Yield();

				Assert.IsTrue(instance.IsRunning);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task MultipleStartStopCycles_DoNotThrowOrLeak()
		{
			var graph = CreateTwoStateGraph();
			try
			{
				var instance = new StateMachineInstance(graph);

				for (var i = 0; i < 3; i++)
				{
					await instance.StartAsync();
					Assert.IsTrue(instance.IsRunning, $"Should be running after start cycle {i}");

					var state = instance.CurrentState as CancellationAwareState;
					Assert.IsNotNull(state, $"State should exist in cycle {i}");
					Assert.IsTrue(state.ReceivedToken.CanBeCanceled, $"Token should be cancellable in cycle {i}");

					await instance.StopAsync();
					Assert.IsFalse(instance.IsRunning, $"Should be stopped after stop cycle {i}");
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public async Task Restart_CancelsPreviousStateToken()
		{
			var graph = CreateTwoStateGraph();
			try
			{
				var instance = new StateMachineInstance(graph);

				await instance.StartAsync();
				var firstState = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(firstState);
				var firstToken = firstState.ReceivedToken;

				await instance.StopAsync();
				await instance.StartAsync();

				// The token from the first run should be cancelled
				Assert.IsTrue(firstToken.IsCancellationRequested);

				// The new run should have a fresh non-cancelled token
				var secondState = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(secondState);
				Assert.IsFalse(secondState.ReceivedToken.IsCancellationRequested);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task StopAsync_OnExitReceivesUsableToken()
		{
			var graph = CreateTwoStateGraph();
			try
			{
				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var state = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(state);

				await instance.StopAsync();

				// OnExitAsync should have been called with a token that was not yet cancelled
				Assert.IsTrue(state.OnExitAsyncCalled);
				// The token should be cancellable (instance token, not CancellationToken.None)
				Assert.IsTrue(state.ReceivedToken.CanBeCanceled);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public async Task PrepareForReuse_CleansUpCancellationTokens()
		{
			var graph = CreateTwoStateGraph();

			try
			{
				var instance = new StateMachineInstance(graph);
				await instance.StartAsync();

				var state = instance.CurrentState as CancellationAwareState;
				Assert.IsNotNull(state);
				var token = state.ReceivedToken;

				instance.PrepareForReuse();

				// The token from before reuse should be cancelled
				Assert.IsTrue(token.IsCancellationRequested);
				Assert.IsFalse(instance.IsRunning);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

	}
}

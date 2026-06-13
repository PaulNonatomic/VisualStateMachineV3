using System;
using System.Collections.Generic;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Object pool for StateMachineInstance objects.
	/// Pools instances per graph to enable reuse of state objects and blackboards.
	/// </summary>
	public class StateMachinePool
	{
		/// <summary>
		/// Fired when an instance is rented from the pool. Args: (instance, wasNewlyCreated)
		/// </summary>
		public event Action<StateMachineInstance, bool> OnRented;

		/// <summary>
		/// Fired when an instance is returned to the pool.
		/// </summary>
		public event Action<StateMachineInstance> OnReturned;

		/// <summary>
		/// Fired when the pool auto-grows beyond its initial capacity.
		/// Args: (graph, currentPoolSize)
		/// </summary>
		public event Action<StateMachineGraph, int> OnPoolGrew;

		private readonly Dictionary<StateMachineGraph, Queue<StateMachineInstance>> _pools = new();
		private readonly Dictionary<StateMachineGraph, int> _maxSizes = new();
		private readonly Dictionary<StateMachineGraph, int> _totalCreated = new();
		private readonly int _defaultMaxSize;

		/// <summary>
		/// Total number of pooled instances across all graphs.
		/// </summary>
		public int TotalPooled
		{
			get
			{
				var count = 0;
				foreach (var pool in _pools.Values)
				{
					count += pool.Count;
				}
				return count;
			}
		}

		/// <summary>
		/// Total number of instances created (includes rented and pooled).
		/// </summary>
		public int TotalCreated
		{
			get
			{
				var count = 0;
				foreach (var created in _totalCreated.Values)
				{
					count += created;
				}
				return count;
			}
		}

		/// <summary>
		/// Creates a new StateMachinePool.
		/// </summary>
		/// <param name="defaultMaxSize">
		/// Default maximum pool size per graph. 0 means unlimited.
		/// When a pool reaches max size, returned instances are discarded.
		/// </param>
		public StateMachinePool(int defaultMaxSize = 0)
		{
			_defaultMaxSize = defaultMaxSize;
		}

		/// <summary>
		/// Pre-warms the pool by creating instances for a specific graph.
		/// Call this during loading screens to avoid runtime allocation spikes.
		/// </summary>
		/// <param name="graph">The graph to create instances for.</param>
		/// <param name="count">Number of instances to pre-create.</param>
		/// <param name="context">Optional context for initialization (instances will be reset before pooling).</param>
		public void Prewarm(StateMachineGraph graph, int count, IStateMachineContext context = null)
		{
			if (graph == null) throw new ArgumentNullException(nameof(graph));
			if (count <= 0) return;

			EnsurePool(graph);

			for (var i = 0; i < count; i++)
			{
				var instance = new StateMachineInstance(graph, context);
				instance.PrepareForReuse();
				_pools[graph].Enqueue(instance);
				IncrementCreated(graph);
			}
		}

		/// <summary>
		/// Sets the maximum pool size for a specific graph.
		/// When a pool reaches max size, returned instances are discarded instead of pooled.
		/// </summary>
		/// <param name="graph">The graph to configure.</param>
		/// <param name="maxSize">Maximum number of pooled instances. 0 means unlimited.</param>
		public void SetMaxPoolSize(StateMachineGraph graph, int maxSize)
		{
			if (graph == null) throw new ArgumentNullException(nameof(graph));
			_maxSizes[graph] = maxSize;
		}

		/// <summary>
		/// Rents an instance from the pool or creates a new one.
		/// The returned instance is initialized and ready for Start().
		/// </summary>
		/// <param name="graph">The graph to get an instance for.</param>
		/// <param name="context">The context for this instance.</param>
		/// <param name="sharedContext">Optional typed shared context for State&lt;TContext&gt; states.</param>
		/// <param name="parent">Optional parent instance for sub-state machines.</param>
		/// <returns>A ready-to-use StateMachineInstance.</returns>
		public StateMachineInstance Rent(
			StateMachineGraph graph,
			IStateMachineContext context,
			object sharedContext = null,
			StateMachineInstance parent = null)
		{
			if (graph == null) throw new ArgumentNullException(nameof(graph));

			EnsurePool(graph);

			StateMachineInstance instance;
			bool wasNew;

			if (_pools[graph].Count > 0)
			{
				instance = _pools[graph].Dequeue();
				instance.Reinitialize(context, sharedContext, parent);
				wasNew = false;
			}
			else
			{
				instance = new StateMachineInstance(graph, context, sharedContext, parent);
				IncrementCreated(graph);
				OnPoolGrew?.Invoke(graph, GetTotalCreated(graph));
				wasNew = true;
			}

			OnRented?.Invoke(instance, wasNew);
			return instance;
		}

		/// <summary>
		/// Returns an instance to the pool for reuse.
		/// The instance is stopped and reset before being pooled.
		/// </summary>
		/// <param name="instance">The instance to return.</param>
		public void Return(StateMachineInstance instance)
		{
			if (instance == null) return;

			var graph = instance.Graph;
			if (graph == null) return;

			EnsurePool(graph);

			var maxSize = GetMaxSize(graph);
			if (maxSize > 0 && _pools[graph].Count >= maxSize)
			{
				// Pool is full, just prepare for reuse and discard
				instance.PrepareForReuse();
				return;
			}

			instance.PrepareForReuse();
			_pools[graph].Enqueue(instance);

			OnReturned?.Invoke(instance);
		}

		/// <summary>
		/// Gets the number of available pooled instances for a graph.
		/// </summary>
		public int GetPooledCount(StateMachineGraph graph)
		{
			if (graph == null || !_pools.TryGetValue(graph, out var pool)) return 0;
			return pool.Count;
		}

		/// <summary>
		/// Gets the total number of instances created for a graph (pooled + rented).
		/// </summary>
		public int GetTotalCreated(StateMachineGraph graph)
		{
			if (graph == null || !_totalCreated.TryGetValue(graph, out var count)) return 0;
			return count;
		}

		/// <summary>
		/// Clears all pooled instances for a specific graph.
		/// </summary>
		public void Clear(StateMachineGraph graph)
		{
			if (graph != null && _pools.TryGetValue(graph, out var pool))
			{
				pool.Clear();
			}
		}

		/// <summary>
		/// Clears all pooled instances for all graphs.
		/// </summary>
		public void ClearAll()
		{
			foreach (var pool in _pools.Values)
			{
				pool.Clear();
			}

			_pools.Clear();
			_totalCreated.Clear();
		}

		private void EnsurePool(StateMachineGraph graph)
		{
			if (!_pools.ContainsKey(graph))
			{
				_pools[graph] = new Queue<StateMachineInstance>();
			}
		}

		private void IncrementCreated(StateMachineGraph graph)
		{
			if (!_totalCreated.ContainsKey(graph))
			{
				_totalCreated[graph] = 0;
			}

			_totalCreated[graph]++;
		}

		private int GetMaxSize(StateMachineGraph graph)
		{
			if (_maxSizes.TryGetValue(graph, out var maxSize))
			{
				return maxSize;
			}

			return _defaultMaxSize;
		}
	}
}

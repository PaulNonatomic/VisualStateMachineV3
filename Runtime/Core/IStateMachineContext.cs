using UnityEngine;

namespace Nonatomic.VSM.Core
{
	/// <summary>
	/// Interface for state machine context objects.
	/// Context provides shared data and references that states can access.
	/// </summary>
	public interface IStateMachineContext
	{
		/// <summary>
		/// The GameObject that owns this state machine instance.
		/// </summary>
		GameObject GameObject { get; }

		/// <summary>
		/// The Transform of the owning GameObject.
		/// </summary>
		Transform Transform { get; }
	}

	/// <summary>
	/// Default implementation of IStateMachineContext.
	/// Provides basic GameObject and Transform references.
	/// </summary>
	public class StateMachineContext : IStateMachineContext
	{
		private readonly GameObject _gameObject;

		public GameObject GameObject => _gameObject;
		public Transform Transform => _gameObject?.transform;

		/// <summary>
		/// Creates a context without a GameObject.
		/// Useful for code-only state machines that don't need Unity references.
		/// </summary>
		public StateMachineContext()
		{
			_gameObject = null;
		}

		public StateMachineContext(GameObject gameObject)
		{
			_gameObject = gameObject;
		}
	}

	/// <summary>
	/// Generic context that can hold additional typed data.
	/// </summary>
	public class StateMachineContext<T> : StateMachineContext
	{
		private T _data;

		/// <summary>
		/// Custom data associated with this context.
		/// </summary>
		public T Data
		{
			get => _data;
			set => _data = value;
		}

		public StateMachineContext(GameObject gameObject) : base(gameObject)
		{
		}

		public StateMachineContext(GameObject gameObject, T data) : base(gameObject)
		{
			_data = data;
		}
	}
}

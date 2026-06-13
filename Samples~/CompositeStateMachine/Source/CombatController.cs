using UnityEngine;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.CompositeStateMachine
{
	/// <summary>
	/// Combat controller demonstrating sub-state machine composition.
	/// Manages combat state including attack and defend sequences.
	/// </summary>
	public class CombatController : MonoBehaviour, IStateMachineContext
	{
		[Header("Visual Feedback")]
		[SerializeField] private Renderer _characterRenderer;
		[SerializeField] private Transform _weaponTransform;

		[Header("Combat Settings")]
		[SerializeField] private float _attackWindupTime = 0.3f;
		[SerializeField] private float _attackStrikeTime = 0.2f;
		[SerializeField] private float _attackRecoveryTime = 0.4f;
		[SerializeField] private float _blockDuration = 2f;
		[SerializeField] private float _staggerDuration = 0.5f;

		[Header("Colors")]
		[SerializeField] private Color _idleColor = Color.white;
		[SerializeField] private Color _windupColor = Color.yellow;
		[SerializeField] private Color _strikeColor = Color.red;
		[SerializeField] private Color _recoveryColor = new Color(1f, 0.5f, 0f);
		[SerializeField] private Color _blockColor = Color.blue;
		[SerializeField] private Color _staggerColor = Color.magenta;

		private MaterialPropertyBlock _propertyBlock;
		private bool _attackPressed;
		private bool _defendPressed;
		private bool _defendReleased;
		private bool _wasHit;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;
		public Transform WeaponTransform => _weaponTransform;
		public float AttackWindupTime => _attackWindupTime;
		public float AttackStrikeTime => _attackStrikeTime;
		public float AttackRecoveryTime => _attackRecoveryTime;
		public float BlockDuration => _blockDuration;
		public float StaggerDuration => _staggerDuration;

		private void Awake()
		{
			_propertyBlock = new MaterialPropertyBlock();
		}

		private void Update()
		{
			// Mouse button 0 for attack
			if (Input.GetMouseButtonDown(0))
			{
				_attackPressed = true;
			}

			// Mouse button 1 for defend
			if (Input.GetMouseButtonDown(1))
			{
				_defendPressed = true;
			}

			if (Input.GetMouseButtonUp(1))
			{
				_defendReleased = true;
			}

			// Space simulates being hit (for testing stagger)
			if (Input.GetKeyDown(KeyCode.Space))
			{
				_wasHit = true;
			}
		}

		public bool ConsumeAttackInput()
		{
			if (_attackPressed)
			{
				_attackPressed = false;
				return true;
			}
			return false;
		}

		public bool ConsumeDefendInput()
		{
			if (_defendPressed)
			{
				_defendPressed = false;
				return true;
			}
			return false;
		}

		public bool ConsumeDefendRelease()
		{
			if (_defendReleased)
			{
				_defendReleased = false;
				return true;
			}
			return false;
		}

		public bool ConsumeHitInput()
		{
			if (_wasHit)
			{
				_wasHit = false;
				return true;
			}
			return false;
		}

		public void SetIdleVisual()
		{
			SetColor(_idleColor);
			ResetWeaponRotation();
		}

		public void SetWindupVisual()
		{
			SetColor(_windupColor);
			// Pull weapon back
			if (_weaponTransform != null)
			{
				_weaponTransform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
			}
		}

		public void SetStrikeVisual()
		{
			SetColor(_strikeColor);
			// Swing weapon forward
			if (_weaponTransform != null)
			{
				_weaponTransform.localRotation = Quaternion.Euler(45f, 0f, 0f);
			}
		}

		public void SetRecoveryVisual()
		{
			SetColor(_recoveryColor);
		}

		public void SetBlockVisual()
		{
			SetColor(_blockColor);
			// Raise weapon for block
			if (_weaponTransform != null)
			{
				_weaponTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
			}
		}

		public void SetStaggerVisual()
		{
			SetColor(_staggerColor);
			ResetWeaponRotation();
		}

		private void SetColor(Color color)
		{
			if (_characterRenderer == null) return;

			_characterRenderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetColor("_Color", color);
			_characterRenderer.SetPropertyBlock(_propertyBlock);
		}

		private void ResetWeaponRotation()
		{
			if (_weaponTransform != null)
			{
				_weaponTransform.localRotation = Quaternion.identity;
			}
		}
	}
}

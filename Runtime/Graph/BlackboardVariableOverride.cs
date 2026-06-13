using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Stores per-variable modifications for a graph variant.
	/// References a blackboard variable in the base graph by key and defines value overrides.
	/// </summary>
	[Serializable]
	public class BlackboardVariableOverride
	{
		[SerializeField] private string _key;
		[SerializeField] private string _defaultValueJsonOverride;
		[SerializeField] private bool _hasDefaultValueOverride;
		[SerializeField] private bool _isExposedOverride;
		[SerializeField] private bool _hasIsExposedOverride;

		public string Key
		{
			get => _key;
			set => _key = value;
		}

		public string DefaultValueJsonOverride
		{
			get => _defaultValueJsonOverride;
			set
			{
				_defaultValueJsonOverride = value;
				_hasDefaultValueOverride = true;
			}
		}

		public bool HasDefaultValueOverride => _hasDefaultValueOverride;

		public bool IsExposedOverride
		{
			get => _isExposedOverride;
			set
			{
				_isExposedOverride = value;
				_hasIsExposedOverride = true;
			}
		}

		public bool HasIsExposedOverride => _hasIsExposedOverride;

		public BlackboardVariableOverride()
		{
		}

		public BlackboardVariableOverride(string key)
		{
			_key = key;
		}

		public void ClearDefaultValueOverride()
		{
			_defaultValueJsonOverride = null;
			_hasDefaultValueOverride = false;
		}

		public void ClearIsExposedOverride()
		{
			_isExposedOverride = false;
			_hasIsExposedOverride = false;
		}

		public bool HasAnyOverrides => _hasDefaultValueOverride || _hasIsExposedOverride;
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Defines how an inherited node is modified in a graph variant.
	/// </summary>
	public enum OverrideAction
	{
		/// <summary>Change specific properties of the node.</summary>
		Modify,

		/// <summary>Replace the node's state type entirely.</summary>
		Replace,

		/// <summary>Disable the node (exclude from merged graph).</summary>
		Disable
	}

	/// <summary>
	/// Stores per-node modifications for a graph variant.
	/// References a node in the base graph by ID and defines what changes to apply.
	/// </summary>
	[Serializable]
	public class NodeOverride
	{
		[SerializeField] private string _nodeId;
		[SerializeField] private OverrideAction _action = OverrideAction.Modify;
		[SerializeField] private List<SerializedPropertyOverride> _propertyOverrides = new List<SerializedPropertyOverride>();
		[SerializeField] private string _replacementStateTypeAQN;
		[SerializeField] private Vector2 _positionOverride;
		[SerializeField] private bool _hasPositionOverride;
		[SerializeField] private string _displayNameOverride;
		[SerializeField] private bool _hasDisplayNameOverride;
		[SerializeField] private string _commentOverride;
		[SerializeField] private bool _hasCommentOverride;

		public string NodeId
		{
			get => _nodeId;
			set => _nodeId = value;
		}

		public OverrideAction Action
		{
			get => _action;
			set => _action = value;
		}

		public IReadOnlyList<SerializedPropertyOverride> PropertyOverrides => _propertyOverrides;

		public string ReplacementStateTypeAQN
		{
			get => _replacementStateTypeAQN;
			set => _replacementStateTypeAQN = value;
		}

		public Vector2 PositionOverride
		{
			get => _positionOverride;
			set
			{
				_positionOverride = value;
				_hasPositionOverride = true;
			}
		}

		public bool HasPositionOverride => _hasPositionOverride;

		public string DisplayNameOverride
		{
			get => _displayNameOverride;
			set
			{
				_displayNameOverride = value;
				_hasDisplayNameOverride = true;
			}
		}

		public bool HasDisplayNameOverride => _hasDisplayNameOverride;

		public string CommentOverride
		{
			get => _commentOverride;
			set
			{
				_commentOverride = value;
				_hasCommentOverride = true;
			}
		}

		public bool HasCommentOverride => _hasCommentOverride;

		public NodeOverride()
		{
		}

		public NodeOverride(string nodeId, OverrideAction action = OverrideAction.Modify)
		{
			_nodeId = nodeId;
			_action = action;
		}

		/// <summary>
		/// Gets a property override by field name.
		/// </summary>
		public SerializedPropertyOverride GetPropertyOverride(string fieldName)
		{
			return _propertyOverrides.Find(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Sets a property override value.
		/// </summary>
		public void SetPropertyOverride(string fieldName, object value, Type type)
		{
			var existing = GetPropertyOverride(fieldName);
			if (existing != null)
			{
				existing.SetValue(value, type);
			}
			else
			{
				var newOverride = new SerializedPropertyOverride(fieldName, value, type);
				_propertyOverrides.Add(newOverride);
			}
		}

		/// <summary>
		/// Removes a property override.
		/// </summary>
		public void RemovePropertyOverride(string fieldName)
		{
			_propertyOverrides.RemoveAll(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Clears the position override, reverting to base value.
		/// </summary>
		public void ClearPositionOverride()
		{
			_positionOverride = Vector2.zero;
			_hasPositionOverride = false;
		}

		/// <summary>
		/// Clears the display name override, reverting to base value.
		/// </summary>
		public void ClearDisplayNameOverride()
		{
			_displayNameOverride = null;
			_hasDisplayNameOverride = false;
		}

		/// <summary>
		/// Clears the comment override, reverting to base value.
		/// </summary>
		public void ClearCommentOverride()
		{
			_commentOverride = null;
			_hasCommentOverride = false;
		}

		/// <summary>
		/// Whether this override has any actual modifications.
		/// </summary>
		public bool HasAnyOverrides =>
			_action == OverrideAction.Disable ||
			_action == OverrideAction.Replace ||
			_propertyOverrides.Count > 0 ||
			_hasPositionOverride ||
			_hasDisplayNameOverride ||
			_hasCommentOverride;
	}
}

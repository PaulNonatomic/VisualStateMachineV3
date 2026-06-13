using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Marks a type for automatic registration as a blackboard variable type.
	/// When applied to an enum, struct, or class, the type will appear in the
	/// blackboard "Add Variable" menu and support serialization/deserialization.
	///
	/// For types in immutable packages where you cannot add this attribute,
	/// use the manual registration API instead:
	/// <code>
	/// BlackboardTypeRegistry.RegisterEnum&lt;SomeExternalEnum&gt;();
	/// BlackboardTypeEditorRegistry.RegisterEnum&lt;SomeExternalEnum&gt;();
	/// </code>
	/// </summary>
	/// <example>
	/// <code>
	/// [BlackboardType]
	/// public enum GamePhase { Menu, Playing, Paused, GameOver }
	///
	/// [BlackboardType(DisplayName = "AI State", Category = "AI")]
	/// public enum AIBehavior { Idle, Patrol, Chase, Attack }
	///
	/// // Serializable structs/classes are supported too — they serialize via
	/// // JsonUtility and get a reflected default-value editor in the blackboard.
	/// [Serializable]
	/// [BlackboardType(Category = "Combat")]
	/// public struct DamageInfo { public int Amount; public float Knockback; }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class,
		AllowMultiple = false, Inherited = false)]
	public class BlackboardTypeAttribute : Attribute
	{
		/// <summary>
		/// Display name in the blackboard menu. Defaults to the type name.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Category for organizing in the blackboard menu.
		/// Defaults to "Enums" for enum types, "Custom" for others.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Sort order within the category. Lower values appear first.
		/// </summary>
		public int Order { get; set; }
	}
}

using UnityEngine;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// Minimal input shim so the sample degrades gracefully across input backends rather than
	/// throwing. It uses the legacy Input Manager when that backend is enabled; under
	/// "Input System (New)" only, the methods become no-ops (and <see cref="Available"/> is false)
	/// so the sample still runs without interactivity instead of throwing an exception.
	///
	/// <para>
	/// Full Input System (New) support is a deliberate follow-up: it needs to be authored and
	/// verified against a project configured for the new backend, which this shim's structure
	/// leaves room for without changing call sites.
	/// </para>
	/// </summary>
	public static class SampleInput
	{
		/// <summary>True when an input backend this shim understands is available.</summary>
		public static bool Available =>
#if ENABLE_LEGACY_INPUT_MANAGER
			true;
#else
			false;
#endif

		public static bool GetMouseButton(int button)
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetMouseButton(button);
#else
			return false;
#endif
		}

		public static bool GetMouseButtonDown(int button)
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetMouseButtonDown(button);
#else
			return false;
#endif
		}

		public static float GetAxis(string axisName)
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetAxis(axisName);
#else
			return 0f;
#endif
		}

		public static bool GetKey(KeyCode key)
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetKey(key);
#else
			return false;
#endif
		}

		public static Vector3 MousePosition =>
#if ENABLE_LEGACY_INPUT_MANAGER
			Input.mousePosition;
#else
			Vector3.zero;
#endif
	}
}

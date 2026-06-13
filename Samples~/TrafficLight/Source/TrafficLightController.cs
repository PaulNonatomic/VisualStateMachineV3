using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight
{
	/// <summary>
	/// Controls a single traffic light housing with three lights (red, yellow, green).
	/// Each light's color comes from its material. Toggled via emission intensity.
	/// </summary>
	public class TrafficLightController : MonoBehaviour
	{
		[Header("Light References")]
		[SerializeField] private MeshRenderer _redLight;
		[SerializeField] private MeshRenderer _yellowLight;
		[SerializeField] private MeshRenderer _greenLight;

		[Header("Emission Settings")]
		[SerializeField] private float _onIntensity = 5f;
		[SerializeField] private float _offIntensity = -10f;

		private LightData _red;
		private LightData _yellow;
		private LightData _green;

		private void Awake()
		{
			_red = CreateLightData(_redLight);
			_yellow = CreateLightData(_yellowLight);
			_green = CreateLightData(_greenLight);

			TurnAllOff();
		}

		private void OnDestroy()
		{
			DestroyMaterial(_red.Material);
			DestroyMaterial(_yellow.Material);
			DestroyMaterial(_green.Material);
		}

		public void TurnAllOff()
		{
			SetLight(_red, false);
			SetLight(_yellow, false);
			SetLight(_green, false);
		}

		public void SetRed()
		{
			SetLight(_red, true);
			SetLight(_yellow, false);
			SetLight(_green, false);
		}

		public void SetYellow()
		{
			SetLight(_red, false);
			SetLight(_yellow, true);
			SetLight(_green, false);
		}

		public void SetGreen()
		{
			SetLight(_red, false);
			SetLight(_yellow, false);
			SetLight(_green, true);
		}

		public void SetRedYellow()
		{
			SetLight(_red, true);
			SetLight(_yellow, true);
			SetLight(_green, false);
		}

		public void SetGreenLight(bool on)
		{
			SetLight(_green, on);
		}

		private LightData CreateLightData(MeshRenderer renderer)
		{
			if (renderer == null) return default;

			var material = new Material(renderer.sharedMaterial);
			renderer.material = material;
			material.EnableKeyword("_EMISSION");

			return new LightData
			{
				Material = material,
				BaseColor = material.color
			};
		}

		private void DestroyMaterial(Material material)
		{
			if (material != null) Destroy(material);
		}

		private void SetLight(LightData data, bool isOn)
		{
			if (data.Material == null) return;

			var multiplier = Mathf.Pow(2f, isOn ? _onIntensity : _offIntensity);
			data.Material.SetColor("_EmissionColor", data.BaseColor * multiplier);
		}

		private struct LightData
		{
			public Material Material;
			public Color BaseColor;
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nonatomic.VSM.Editor.Services
{
	[InitializeOnLoad]
	public static class SampleMaterialProcessor
	{
		private const string PackageName = "com.nonatomic.visualstatemachinev3";
		private const string PackageDisplayName = "Visual State Machine V3";
		private const string LogPrefix = "[VSM Samples]";

		private const string BuiltInShaderName = "Standard";
		private const string UrpShaderName = "Universal Render Pipeline/Lit";
		private const string HdrpShaderName = "HDRP/Lit";

		private static readonly string[] SampleSearchPaths =
		{
			$"Packages/{PackageName}/Samples",
			$"Assets/Samples/{PackageDisplayName}"
		};

		static SampleMaterialProcessor()
		{
			EditorApplication.delayCall += ProcessSampleMaterialsIfNeeded;
		}

		[MenuItem("Tools/Visual State Machine/Fix Sample Materials", priority = 200)]
		public static void FixSampleMaterialsMenuItem()
		{
			var count = ProcessSampleMaterials(force: true);
			var pipeline = DetectPipeline();

			if (count > 0)
			{
				Debug.Log($"{LogPrefix} Fixed {count} sample material(s) for {pipeline} pipeline.");
			}
			else
			{
				Debug.Log($"{LogPrefix} All sample materials already match the {pipeline} pipeline.");
			}
		}

		private static void ProcessSampleMaterialsIfNeeded()
		{
			ProcessSampleMaterials(force: false);
		}

		private static int ProcessSampleMaterials(bool force)
		{
			var pipeline = DetectPipeline();
			var targetShader = FindShaderForPipeline(pipeline);

			if (targetShader == null)
			{
				Debug.LogWarning($"{LogPrefix} Could not find shader for {pipeline} pipeline. " +
					"Ensure the correct render pipeline package is installed.");
				return 0;
			}

			var materials = FindSampleMaterials();
			var fixedCount = 0;

			foreach (var material in materials)
			{
				if (material == null) continue;
				if (!force && !NeedsConversion(material, targetShader)) continue;

				ConvertMaterial(material, pipeline, targetShader);
				fixedCount++;
			}

			if (fixedCount > 0)
			{
				AssetDatabase.SaveAssets();
			}

			return fixedCount;
		}

		private static RenderPipelineType DetectPipeline()
		{
			var pipelineAsset = GraphicsSettings.defaultRenderPipeline;
			if (pipelineAsset == null) return RenderPipelineType.BuiltIn;

			var typeName = pipelineAsset.GetType().Name;

			if (typeName.Contains("Universal") || typeName.Contains("URP"))
			{
				return RenderPipelineType.URP;
			}

			if (typeName.Contains("HD"))
			{
				return RenderPipelineType.HDRP;
			}

			return RenderPipelineType.BuiltIn;
		}

		private static Shader FindShaderForPipeline(RenderPipelineType pipeline)
		{
			var shaderName = pipeline switch
			{
				RenderPipelineType.URP => UrpShaderName,
				RenderPipelineType.HDRP => HdrpShaderName,
				_ => BuiltInShaderName
			};

			return Shader.Find(shaderName);
		}

		internal static bool IsSampleMaterialPath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".mat")) return false;
			return SampleSearchPaths.Any(basePath => assetPath.StartsWith(basePath));
		}

		internal static void ProcessImportedMaterials(string[] importedAssets)
		{
			var pipeline = DetectPipeline();
			var targetShader = FindShaderForPipeline(pipeline);
			if (targetShader == null) return;

			var fixedCount = 0;

			foreach (var path in importedAssets)
			{
				if (!IsSampleMaterialPath(path)) continue;

				var material = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (material == null || !NeedsConversion(material, targetShader)) continue;

				ConvertMaterial(material, pipeline, targetShader);
				fixedCount++;
			}

			if (fixedCount > 0)
			{
				AssetDatabase.SaveAssets();
			}
		}

		private static List<Material> FindSampleMaterials()
		{
			var results = new List<Material>();

			foreach (var basePath in SampleSearchPaths)
			{
				var guids = AssetDatabase.FindAssets("t:Material", new[] { basePath });
				foreach (var guid in guids)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var material = AssetDatabase.LoadAssetAtPath<Material>(path);
					if (material != null)
					{
						results.Add(material);
					}
				}
			}

			return results;
		}

		private static bool NeedsConversion(Material material, Shader targetShader)
		{
			if (material.shader == null) return true;
			if (material.shader.name == "Hidden/InternalErrorShader") return true;

			return material.shader.name != targetShader.name;
		}

		private static void ConvertMaterial(Material material, RenderPipelineType targetPipeline, Shader targetShader)
		{
			var properties = ExtractProperties(material);

			material.shader = targetShader;

			ApplyProperties(material, targetPipeline, properties);

			EditorUtility.SetDirty(material);

			var path = AssetDatabase.GetAssetPath(material);
			Debug.Log($"{LogPrefix} Converted '{material.name}' at {path} to {targetPipeline}.");
		}

		private static MaterialProperties ExtractProperties(Material material)
		{
			var props = new MaterialProperties
			{
				Color = TryGetColor(material, "_BaseColor")
					?? TryGetColor(material, "_Color")
					?? Color.white,
				Smoothness = TryGetFloat(material, "_Smoothness")
					?? TryGetFloat(material, "_Glossiness")
					?? 0.5f,
				Metallic = TryGetFloat(material, "_Metallic") ?? 0f,
				MainTexture = TryGetTexture(material, "_BaseMap")
					?? TryGetTexture(material, "_BaseColorMap")
					?? TryGetTexture(material, "_MainTex"),
				NormalMap = TryGetTexture(material, "_BumpMap")
					?? TryGetTexture(material, "_NormalMap"),
				EmissionColor = TryGetColor(material, "_EmissionColor") ?? Color.black,
				OcclusionStrength = TryGetFloat(material, "_OcclusionStrength") ?? 1f,
				BumpScale = TryGetFloat(material, "_BumpScale") ?? 1f
			};

			return props;
		}

		private static void ApplyProperties(
			Material material,
			RenderPipelineType pipeline,
			MaterialProperties props)
		{
			switch (pipeline)
			{
				case RenderPipelineType.BuiltIn:
					material.SetColor("_Color", props.Color);
					material.SetFloat("_Glossiness", props.Smoothness);
					material.SetFloat("_Metallic", props.Metallic);
					if (props.MainTexture != null) material.SetTexture("_MainTex", props.MainTexture);
					if (props.NormalMap != null) material.SetTexture("_BumpMap", props.NormalMap);
					material.SetColor("_EmissionColor", props.EmissionColor);
					material.SetFloat("_OcclusionStrength", props.OcclusionStrength);
					material.SetFloat("_BumpScale", props.BumpScale);
					break;

				case RenderPipelineType.URP:
					material.SetColor("_BaseColor", props.Color);
					material.SetColor("_Color", props.Color);
					material.SetFloat("_Smoothness", props.Smoothness);
					material.SetFloat("_Metallic", props.Metallic);
					if (props.MainTexture != null)
					{
						material.SetTexture("_BaseMap", props.MainTexture);
						material.SetTexture("_MainTex", props.MainTexture);
					}
					if (props.NormalMap != null) material.SetTexture("_BumpMap", props.NormalMap);
					material.SetColor("_EmissionColor", props.EmissionColor);
					material.SetFloat("_OcclusionStrength", props.OcclusionStrength);
					material.SetFloat("_BumpScale", props.BumpScale);
					break;

				case RenderPipelineType.HDRP:
					material.SetColor("_BaseColor", props.Color);
					material.SetFloat("_Smoothness", props.Smoothness);
					material.SetFloat("_Metallic", props.Metallic);
					if (props.MainTexture != null) material.SetTexture("_BaseColorMap", props.MainTexture);
					if (props.NormalMap != null) material.SetTexture("_NormalMap", props.NormalMap);
					material.SetColor("_EmissionColor", props.EmissionColor);
					material.SetFloat("_BumpScale", props.BumpScale);
					break;
			}
		}

		private static Color? TryGetColor(Material material, string propertyName)
		{
			if (!material.HasProperty(propertyName)) return null;
			return material.GetColor(propertyName);
		}

		private static float? TryGetFloat(Material material, string propertyName)
		{
			if (!material.HasProperty(propertyName)) return null;
			return material.GetFloat(propertyName);
		}

		private static Texture TryGetTexture(Material material, string propertyName)
		{
			if (!material.HasProperty(propertyName)) return null;
			return material.GetTexture(propertyName);
		}

		private enum RenderPipelineType
		{
			BuiltIn,
			URP,
			HDRP
		}

		private struct MaterialProperties
		{
			public Color Color;
			public float Smoothness;
			public float Metallic;
			public Texture MainTexture;
			public Texture NormalMap;
			public Color EmissionColor;
			public float OcclusionStrength;
			public float BumpScale;
		}
	}

	public class SampleMaterialPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			var hasSampleMaterials = false;

			foreach (var path in importedAssets)
			{
				if (SampleMaterialProcessor.IsSampleMaterialPath(path))
				{
					hasSampleMaterials = true;
					break;
				}
			}

			if (hasSampleMaterials)
			{
				SampleMaterialProcessor.ProcessImportedMaterials(importedAssets);
			}
		}
	}
}

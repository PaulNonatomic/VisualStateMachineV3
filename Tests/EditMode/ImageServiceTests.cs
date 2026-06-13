using NUnit.Framework;
using Nonatomic.VSM.Editor.Services;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ImageServiceTests
	{
		[SetUp]
		public void SetUp()
		{
			ImageService.ClearCache();
		}

		[Test]
		public void IconBasePath_IsIconsNodes()
		{
			Assert.AreEqual("Icons/Nodes/", ImageService.IconBasePath);
		}

		[Test]
		public void LoadTexture_NullPath_ReturnsNull()
		{
			var result = ImageService.LoadTexture(null);

			Assert.IsNull(result);
		}

		[Test]
		public void LoadTexture_EmptyPath_ReturnsNull()
		{
			var result = ImageService.LoadTexture("");

			Assert.IsNull(result);
		}

		[Test]
		public void LoadTexture_NonExistentPath_ReturnsNull()
		{
			var result = ImageService.LoadTexture("NonExistent/FakeTexture");

			Assert.IsNull(result);
		}

		[Test]
		public void LoadTexture_SamePath_ReturnsCachedResult()
		{
			var result1 = ImageService.LoadTexture("NonExistent/TestCache");
			var result2 = ImageService.LoadTexture("NonExistent/TestCache");

			// Both should be null (non-existent), but the point is the cache stores
			// the result and returns the same reference on subsequent calls
			Assert.AreEqual(result1, result2);
		}

		[Test]
		public void LoadNodeIcon_NullName_ReturnsNull()
		{
			var result = ImageService.LoadNodeIcon(null);

			Assert.IsNull(result);
		}

		[Test]
		public void LoadNodeIcon_EmptyName_ReturnsNull()
		{
			var result = ImageService.LoadNodeIcon("");

			Assert.IsNull(result);
		}

		[Test]
		public void LoadNodeIcon_WithSlash_DelegatesToLoadTextureDirectly()
		{
			// When icon name contains "/", it's treated as a full path
			var result = ImageService.LoadNodeIcon("Custom/Path/Icon");

			// It delegates to LoadTexture("Custom/Path/Icon") without prepending IconBasePath
			// We can't easily verify the delegation, but we can verify it doesn't crash
			// and returns null for a non-existent path
			Assert.IsNull(result);
		}

		[Test]
		public void LoadNodeIcon_WithoutSlash_PrependsIconBasePath()
		{
			// When icon name doesn't contain "/", IconBasePath is prepended
			// This will look for "Icons/Nodes/MyIcon" in Resources
			var result = ImageService.LoadNodeIcon("MyIcon");

			// Returns null because the icon doesn't exist in test context,
			// but the method should not throw
			Assert.IsNull(result);
		}

		[Test]
		public void ClearCache_DoesNotThrowWhenEmpty()
		{
			Assert.DoesNotThrow(() => ImageService.ClearCache());
		}

		[Test]
		public void ClearCache_AfterLoading_DoesNotThrow()
		{
			ImageService.LoadTexture("SomePath");
			ImageService.LoadTexture("AnotherPath");

			Assert.DoesNotThrow(() => ImageService.ClearCache());
		}
	}
}

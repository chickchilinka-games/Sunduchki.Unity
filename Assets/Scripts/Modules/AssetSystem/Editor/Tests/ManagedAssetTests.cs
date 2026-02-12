using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using Modules.AssetSystem.Storage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Modules.AssetSystem.Editor.Tests
{
    [TestFixture]
    public class ManagedAssetTests
    {
        private AssetService _assetService;
        private MockAssetProvider _mockProvider;
        private AssetCache _cache;
        private GameObject _testAsset;

        [SetUp]
        public void SetUp()
        {
            _mockProvider = new MockAssetProvider();
            _cache = new AssetCache();
            _assetService = new AssetService(_mockProvider, _cache);
            _testAsset = new GameObject("TestAsset");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testAsset != null) UnityEngine.Object.DestroyImmediate(_testAsset);
            _mockProvider?.Clear();
        }

        [Test]
        public void Asset_AfterCreation_ReturnsOriginalAsset()
        {
            var managedAsset = new ManagedAsset<GameObject>(_testAsset, _assetService, "test1");

            Assert.AreEqual(_testAsset, managedAsset.Asset);
        }

        [Test]
        public async Task Dispose_CallsServiceReleaseAsset()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            _cache.CacheAsset("test1", _testAsset);
            var managedAsset = new ManagedAsset<GameObject>(_testAsset, _assetService, "test1");

            await managedAsset.DisposeAsync();
            await UniTask.Yield();

            Assert.AreEqual(1, _mockProvider.UnloadCallCount);
        }
        
        [Test]
        public async Task Asset_AfterDispose_ReturnsNull()
        {
            var managedAsset = new ManagedAsset<GameObject>(_testAsset, _assetService, "test1");

            await managedAsset.DisposeAsync();

            Assert.IsNull(managedAsset.Asset);
        }

        [Test]
        public async Task Dispose_WithServiceException_DoesNotThrow()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            _mockProvider.ThrowOnUnload = true;
            _cache.CacheAsset("test1", _testAsset);
            var managedAsset = new ManagedAsset<GameObject>(_testAsset, _assetService, "test1");

            LogAssert.Expect(LogType.Exception, "Exception: Mock provider unload error");
            
            Assert.DoesNotThrowAsync(async () => await managedAsset.DisposeAsync());
            await UniTask.Yield();
        }

        [Test]
        public async Task Integration_GetAndDispose_CompletesSuccessfully()
        {
            _mockProvider.SetAsset("test1", _testAsset);

            var managedAsset = await _assetService.Get<GameObject>("test1");
            Assert.IsNotNull(managedAsset);
            Assert.AreEqual(_testAsset, managedAsset.Asset);

            await managedAsset.DisposeAsync();
            await UniTask.Yield();

            Assert.IsNull(managedAsset.Asset);
            Assert.AreEqual(1, _mockProvider.GetCallCount);
            Assert.AreEqual(1, _mockProvider.UnloadCallCount);
        }

        [Test]
        public async Task Integration_MultipleReferencesDisposal_WorksCorrectly()
        {
            _mockProvider.SetAsset("test1", _testAsset);

            var managedAsset1 = await _assetService.Get<GameObject>("test1");
            var managedAsset2 = await _assetService.Get<GameObject>("test1");

            Assert.AreEqual(_testAsset, managedAsset1.Asset);
            Assert.AreEqual(_testAsset, managedAsset2.Asset);

            await managedAsset1.DisposeAsync();
            await UniTask.Yield();
            Assert.AreEqual(0, _mockProvider.UnloadCallCount);

            await managedAsset2.DisposeAsync();
            await UniTask.Yield();
            Assert.AreEqual(1, _mockProvider.UnloadCallCount);
        }
    }
}

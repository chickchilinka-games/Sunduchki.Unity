using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Exceptions;
using Modules.AssetSystem.Services;
using Modules.AssetSystem.Storage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Modules.AssetSystem.Editor.Tests
{
    [TestFixture]
    public class AssetServiceTests
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
        public async Task Get_AssetExists_ReturnsManagedAsset()
        {
            _mockProvider.SetAsset("test1", _testAsset);

            var result = await _assetService.Get<GameObject>("test1");

            Assert.IsNotNull(result);
            Assert.AreEqual(_testAsset, result.Asset);
            Assert.AreEqual(1, _mockProvider.GetCallCount);
        }

        [Test]
        public async Task Get_AssetCached_ReturnsCachedAssetWithoutProviderCall()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            await _assetService.Get<GameObject>("test1");

            var result = await _assetService.Get<GameObject>("test1");

            Assert.IsNotNull(result);
            Assert.AreEqual(_testAsset, result.Asset);
            Assert.AreEqual(1, _mockProvider.GetCallCount);
        }

        [Test]
        public async Task Get_AssetNotFound_ThrowsAssetNotFound()
        {
            try
            {
                await _assetService.Get<GameObject>("nonexistent");
                Assert.Fail("Expected AssetNotFoundException was not thrown");
            }
            catch (AssetNotFoundException ex)
            {
                Assert.IsTrue(ex.Message.Contains("not found"));
            }
        }

        [Test]
        public async Task Get_ProviderThrows_PropagatesException()
        {
            _mockProvider.ThrowOnGet = true;

            try
            {
                await _assetService.Get<GameObject>("test1");
                Assert.Fail("Expected Exception was not thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Mock provider get error"));
            }
        }

        [Test]
        public async Task ReleaseAsset_SingleReference_CallsProviderUnload()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            var managedAsset = await _assetService.Get<GameObject>("test1");

            await managedAsset.DisposeAsync();
            await UniTask.Yield();

            Assert.AreEqual(1, _mockProvider.UnloadCallCount);
            Assert.Contains("test1", _mockProvider.UnloadedAssets);
        }

        [Test]
        public async Task ReleaseAsset_MultipleReferences_DoesNotUnloadUntilLastReference()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            var managedAsset1 = await _assetService.Get<GameObject>("test1");
            var managedAsset2 = await _assetService.Get<GameObject>("test1");

            await managedAsset1.DisposeAsync();
            await UniTask.Yield();

            Assert.AreEqual(0, _mockProvider.UnloadCallCount);

            await managedAsset2.DisposeAsync();
            await UniTask.Yield();

            Assert.AreEqual(1, _mockProvider.UnloadCallCount);
        }

        [Test]
        public async Task ReleaseAsset_ProviderUnloadThrows_ContinuesExecution()
        {
            _mockProvider.SetAsset("test1", _testAsset);
            _mockProvider.ThrowOnUnload = true;
            var managedAsset = await _assetService.Get<GameObject>("test1");

            LogAssert.Expect(LogType.Exception, "Exception: Mock provider unload error");
            
            Assert.DoesNotThrowAsync(async () => await managedAsset.DisposeAsync());
            
            await UniTask.Yield();
        }

        [Test]
        public async Task Get_MultipleCallsSameId_ReturnsIndependentManagedAssets()
        {
            _mockProvider.SetAsset("test1", _testAsset);

            var result1 = await _assetService.Get<GameObject>("test1");
            var result2 = await _assetService.Get<GameObject>("test1");

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.AreEqual(_testAsset, result1.Asset);
            Assert.AreEqual(_testAsset, result2.Asset);
            Assert.AreNotSame(result1, result2);
        }
    }
}
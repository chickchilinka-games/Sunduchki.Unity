using System;
using Modules.AssetSystem.Storage;
using NUnit.Framework;
using UnityEngine;

namespace Modules.AssetSystem.Editor.Tests
{
    [TestFixture]
    public class AssetCacheTests
    {
        private AssetCache _cache;
        private GameObject _testAsset1;
        private GameObject _testAsset2;
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            _cache = new AssetCache();
            _testAsset1 = new GameObject("TestAsset1");
            _testAsset2 = new GameObject("TestAsset2");
            _testTexture = new Texture2D(1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testAsset1 != null) UnityEngine.Object.DestroyImmediate(_testAsset1);
            if (_testAsset2 != null) UnityEngine.Object.DestroyImmediate(_testAsset2);
            if (_testTexture != null) UnityEngine.Object.DestroyImmediate(_testTexture);
        }

        [Test]
        public void CacheAsset_NewAsset_StoresAssetWithRefCountOne()
        {
            _cache.CacheAsset("test1", _testAsset1);

            Assert.IsTrue(_cache.TryGetAsset<GameObject>("test1", out var asset));
            Assert.AreEqual(_testAsset1, asset);
        }

        [Test]
        public void CacheAsset_SameIdSameType_IncrementsRefCount()
        {
            _cache.CacheAsset("test1", _testAsset1);
            _cache.CacheAsset("test1", _testAsset1);

            Assert.IsTrue(_cache.TryGetAsset<GameObject>("test1", out var asset));
            Assert.AreEqual(_testAsset1, asset);
        }

        [Test]
        public void CacheAsset_SameIdDifferentType_ThrowsException()
        {
            _cache.CacheAsset("test1", _testAsset1);

            Assert.Throws<InvalidOperationException>(() => 
                _cache.CacheAsset("test1", _testTexture));
        }

        [Test]
        public void TryGetAsset_ExistingAsset_ReturnsTrue()
        {
            _cache.CacheAsset("test1", _testAsset1);

            var result = _cache.TryGetAsset<GameObject>("test1", out var asset);

            Assert.IsTrue(result);
            Assert.AreEqual(_testAsset1, asset);
        }

        [Test]
        public void TryGetAsset_NonExistingAsset_ReturnsFalse()
        {
            var result = _cache.TryGetAsset<GameObject>("nonexistent", out var asset);

            Assert.IsFalse(result);
            Assert.IsNull(asset);
        }

        [Test]
        public void TryGetAsset_WrongType_ReturnsFalse()
        {
            _cache.CacheAsset("test1", _testAsset1);

            var result = _cache.TryGetAsset<Texture2D>("test1", out var asset);

            Assert.IsFalse(result);
            Assert.IsNull(asset);
        }

        [Test]
        public void ReleaseAsset_SingleReference_RemovesFromCache()
        {
            _cache.CacheAsset("test1", _testAsset1);

            var count = _cache.ReleaseAsset("test1", out var releasedAsset);

            Assert.AreEqual(0, count);
            Assert.AreEqual(_testAsset1, releasedAsset);
            Assert.IsFalse(_cache.TryGetAsset<GameObject>("test1", out _));
        }

        [Test]
        public void ReleaseAsset_MultipleReferences_DecrementsRefCount()
        {
            _cache.CacheAsset("test1", _testAsset1);
            _cache.CacheAsset("test1", _testAsset1);

            var count = _cache.ReleaseAsset("test1", out var releasedAsset);

            Assert.AreEqual(1, count);
            Assert.IsNull(releasedAsset);
            Assert.IsTrue(_cache.TryGetAsset<GameObject>("test1", out _));
        }

        [Test]
        public void ReleaseAsset_NonExistingAsset_ReturnsZero()
        {
            var count = _cache.ReleaseAsset("nonexistent", out var releasedAsset);

            Assert.AreEqual(0, count);
            Assert.IsNull(releasedAsset);
        }

        [Test]
        public void TryGetAsset_AfterCaching_IncrementsRefCount()
        {
            _cache.CacheAsset("test1", _testAsset1);
            _cache.TryGetAsset<GameObject>("test1", out _);

            var count = _cache.ReleaseAsset("test1", out var releasedAsset);

            Assert.AreEqual(1, count);
            Assert.IsNull(releasedAsset);
        }
    }
}
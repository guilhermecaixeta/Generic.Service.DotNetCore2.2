﻿using System.Threading.Tasks;
using Generic.Repository.Cache;
using NUnit.Framework;

namespace Generic.Repository.UnitTest.Cache
{
    public abstract class CacheConfigurationTest<T>
        where T : class
    {
        protected readonly string NameType = typeof(T).Name;
        protected ICacheRepository Cache;
        protected string NameAttribute;
        protected string NameProperty;
        protected string NoCacheableProperty;
        protected string SomeKey = "ABDC";

        [TearDown]
        public void CacheTearDown()
        {
            Cache.ClearCache();
        }

        [SetUp]
        public async Task CacheUp()
        {
            Cache = new CacheRepository();
            await Cache.AddGet<T>(default);
            await Cache.AddSet<T>(default);
            await Cache.AddProperty<T>(default);
            await Cache.AddAttribute<T>(default);
        }
    }
}
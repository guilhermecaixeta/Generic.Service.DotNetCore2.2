using Generic.Repository.Attributes;
using Generic.Repository.Extension.Validation;
using Generic.Repository.ThrowError;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Generic.Repository.Cache
{
    public class CacheRepository : ICacheRepository
    {
        public CacheRepository()
        {
            InitCache();
        }

        private IDictionary<string, Dictionary<string, Dictionary<string, CustomAttributeTypedArgument>>> CacheAttribute { get; set; }

        private ICacheRepositoryFacade CacheFacade { get; } = new CacheRepositoryFacade();

        private IDictionary<string, Dictionary<string, Func<object, object>>> CacheGet { get; set; }

        private IDictionary<string, Dictionary<string, PropertyInfo>> CacheProperties { get; set; }

        private IDictionary<string, Dictionary<string, Action<object, object>>> CacheSet { get; set; }

        public async Task AddAttribute<TValue>(
            CancellationToken token)
        {
            await CacheFacade.RunActionInSemaphore(() =>
            {
                var values = GetValues<TValue>();

                var valid = CacheAttribute.ContainsKey(values.typeName);

                if (valid || !values.isCacheable)
                {
                    return;
                }

                var dictionary = new Dictionary<string, Dictionary<string, CustomAttributeTypedArgument>>(values.properties.Count());

                CacheAttribute.Add(values.typeName, dictionary);

                foreach (var property in values.properties)
                {
                    SetCacheAttributes(property, values.typeName);
                }
            }, token).ConfigureAwait(false);
        }

        public async Task AddGet<TValue>(
            CancellationToken token)
        {
            await CacheFacade.RunActionInSemaphore(() =>
            {
                var values = GetValues<TValue>();

                var valid = CacheGet.ContainsKey(values.typeName);

                if (valid || !values.isCacheable)
                {
                    return;
                }

                var dictionary = values.properties.
                    ToDictionary(g => g.Name, m => CacheFacade.CreateFunction<TValue>(m));
                CacheGet.Add(values.typeName, dictionary);
            }, token).ConfigureAwait(false);
        }

        public async Task AddProperty<TValue>(
            CancellationToken token)
        {
            await CacheFacade.RunActionInSemaphore(() =>
            {
                var values = GetValues<TValue>();

                var valid = CacheProperties.ContainsKey(values.typeName);

                if (valid || !values.isCacheable)
                {
                    return;
                }

                CacheProperties.Add(values.typeName, values.properties.ToDictionary(p => p.Name, p => p));
            }, token).ConfigureAwait(false);
        }

        public async Task AddSet<TValue>(
            CancellationToken token)
        {
            await CacheFacade.RunActionInSemaphore(() =>
            {
                var (typeName, properties, isCacheable) = GetValues<TValue>();

                var valid = CacheSet.ContainsKey(typeName);

                if (valid || !isCacheable)
                {
                    return;
                }

                var dictionary = properties.
                    ToDictionary(s => s.Name, m => CacheFacade.CreateAction<TValue>(m));

                CacheSet.Add(typeName, dictionary);
            }, token).ConfigureAwait(false);
        }

        public void ClearCache()
        {
            InitCache();
        }

        public async Task<CustomAttributeTypedArgument> GetAttribute(
            string objectKey,
            string propertyKey,
            string customAttributeKey,
            CancellationToken token)
        {
            var typeDictionary = await CacheFacade
            .GetData(
                CacheAttribute,
                objectKey, token).ConfigureAwait(false);

            DictionaryValidation(typeDictionary, nameof(GetDictionaryAttribute));

            var attributeDictionary = await CacheFacade
            .GetData(
                typeDictionary,
                propertyKey, token).ConfigureAwait(false);

            DictionaryValidation(attributeDictionary, nameof(GetDictionaryAttribute));

            var result = await CacheFacade
            .GetData(
                attributeDictionary,
                customAttributeKey, token).ConfigureAwait(false);

            return result;
        }

        public async Task<IDictionary<string, CustomAttributeTypedArgument>> GetDictionaryAttribute(
            string objectKey,
            string propertyKey,
            CancellationToken token)
        {
            var attributeDictionary = await CacheFacade.GetData(CacheAttribute, objectKey, token).
                ConfigureAwait(false);

            DictionaryValidation(attributeDictionary, nameof(GetDictionaryAttribute));

            var result = await CacheFacade.GetData(attributeDictionary, propertyKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<IDictionary<string, Dictionary<string, CustomAttributeTypedArgument>>> GetDictionaryAttribute(
            string objectKey,
            CancellationToken token)
        {
            var result = await CacheFacade.GetData(CacheAttribute, objectKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<IDictionary<string, Func<object, object>>> GetDictionaryMethodGet(
            string objectKey,
            CancellationToken token)
        {
            var dictionary = await CacheFacade.
                GetData(CacheGet, objectKey, token).ConfigureAwait(false);

            return dictionary;
        }

        public async Task<IDictionary<string, Action<object, object>>> GetDictionaryMethodSet(
            string objectKey,
            CancellationToken token)
        {
            var dictionary = await CacheFacade.GetData(CacheSet, objectKey, token).ConfigureAwait(false);
            return dictionary;
        }

        public async Task<IDictionary<string, PropertyInfo>> GetDictionaryProperties(
            string objectKey,
            CancellationToken token)
        {
            var result = await CacheFacade.
                GetData(CacheProperties, objectKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<Func<object, object>> GetMethodGet(
            string objectKey,
            string propertyKey,
            CancellationToken token)
        {
            var getMethodDictionary = await CacheFacade.
                GetData(CacheGet, objectKey, token).
                ConfigureAwait(false);

            DictionaryValidation(getMethodDictionary, nameof(GetMethodGet));

            var result = await CacheFacade.
                GetData(getMethodDictionary, propertyKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<Action<object, object>> GetMethodSet(
            string objectKey,
            string propertyKey,
            CancellationToken token)
        {
            var setMethodDictionary = await CacheFacade.
            GetData(CacheSet, objectKey, token).
            ConfigureAwait(false);

            DictionaryValidation(setMethodDictionary, nameof(GetMethodSet));

            var result = await CacheFacade.
                GetData(setMethodDictionary, propertyKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<PropertyInfo> GetProperty(
            string objectKey,
            string propertyKey,
            CancellationToken token)
        {
            var dictionary = await CacheFacade.
                GetData(CacheProperties, objectKey, token);

            DictionaryValidation(dictionary, nameof(GetProperty));

            var result = await CacheFacade.
                GetData(dictionary, propertyKey, token).
                ConfigureAwait(false);

            return result;
        }

        public async Task<bool> HasAttribute(
            CancellationToken token) =>
            await Task.Run(() => CacheAttribute.Any(), token).
                ConfigureAwait(false);

        public async Task<bool> HasMethodGet(
            CancellationToken token) =>
            await Task.Run(() => CacheGet.Any(), token).
                ConfigureAwait(false);

        public async Task<bool> HasMethodSet(
            CancellationToken token) =>
            await Task.Run(() => CacheSet.Any(), token).
                ConfigureAwait(false);

        public async Task<bool> HasProperty(
            CancellationToken token) =>
            await Task.Run(() => CacheProperties.Any(), token).
                ConfigureAwait(false);

        /// <summary>Gets the values from TValue</summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns></returns>
        private (string typeName, IEnumerable<PropertyInfo> properties, bool isCacheable) GetValues<TValue>()
        {
            var typeName = typeof(TValue).Name;

            var isCacheable = typeof(TValue).GetCustomAttribute<NoCacheableAttribute>() == null;

            if (!isCacheable)
            {
                return (typeName, default, false);
            }

            var properties = typeof(TValue).
                GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            ThrowErrorIf.IsNullOrEmptyList(properties, nameof(properties), string.Empty);

            var propertiesList = ValidateProperty(properties);

            return (typeName, propertiesList, true);
        }

        private void InitCache()
        {
            CacheProperties = new Dictionary<string, Dictionary<string, PropertyInfo>>();
            CacheGet = new Dictionary<string, Dictionary<string, Func<object, object>>>();
            CacheSet = new Dictionary<string, Dictionary<string, Action<object, object>>>();
            CacheAttribute = new Dictionary<string, Dictionary<string, Dictionary<string, CustomAttributeTypedArgument>>>();
        }

        private void SetCacheAttributes(MemberInfo propertyInfo, string typeName)
        {
            var propertyName = propertyInfo.Name;

            CacheAttribute[typeName].Add(propertyName, propertyInfo.
                GetCustomAttributesData().
                SelectMany(x => x.NamedArguments).
                ToDictionary(x => x.MemberName, x => x.TypedValue));
        }

        /// <summary>Validates the property.</summary>
        /// <param name="properties">The property.</param>
        /// <returns></returns>
        private IEnumerable<PropertyInfo> ValidateProperty(IEnumerable<PropertyInfo> properties)
        {
            foreach (var property in properties)
            {
                var isCacheable = property.GetCustomAttribute<NoCacheableAttribute>() == null;

                var type = property.PropertyType;

                var isPrimitive = type.IsSubclassOf(typeof(ValueType)) ||
                                          type == typeof(string) ||
                                          type == typeof(StringBuilder) ||
                                          type == typeof(StringDictionary);

                if (!isPrimitive || !isCacheable)
                {
                    continue;
                }

                yield return property;
            }
        }

        private void DictionaryValidation<TDic>(TDic dictionary, string message)
        {
            if (dictionary.IsNull())
            {
                throw new KeyNotFoundException(message);
            }
        }
    }
}
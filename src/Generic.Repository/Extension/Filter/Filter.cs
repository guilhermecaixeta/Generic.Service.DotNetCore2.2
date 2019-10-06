using Generic.Repository.Cache;
using Generic.Repository.Enums;
using Generic.Repository.Extension.Error;
using Generic.Repository.Extension.Filter.Facade;
using Generic.Repository.Extension.Validation;
using Generic.Repository.Models.Filter;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Generic.Repository.Extension.Filter
{
    /// <summary>
    /// Extension Filter to generate lambda.
    /// </summary>
    internal static class Filter
    {
        private static readonly ExpressionTypeFacade FilterFacade = new ExpressionTypeFacade();

        private const string NameProperty = nameof(NameProperty);

        private const string MethodOption = nameof(MethodOption);

        private const string MergeOption = nameof(MergeOption);

        /// <summary>Generates the predicate.</summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TFilter">The type of the filter.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="cacheRepository">The cache repository.</param>
        /// <returns></returns>
        public static Expression<Func<TValue, bool>> GeneratePredicate<TValue, TFilter>(
        this TFilter filter,
        ICacheRepository cacheRepository)
        where TValue : class
        where TFilter : class, IFilter
        {
            var predicate = (Expression<Func<TValue, bool>>)null;
            var parameter = Expression.Parameter(typeof(TValue));
            var filterName = typeof(TFilter).Name;
            var mergeOption = LambdaMerge.And;

            var methodGetDictionary = cacheRepository.GetDictionaryMethodGet(filterName);

            foreach (var methodGetFilter in methodGetDictionary)
            {
                var key = methodGetFilter.Key;
                var value = methodGetFilter.Value(filter);

                if (IsValidValue(value))
                {

                    var attributeCached = cacheRepository.GetDictionaryAttribute(filterName);

                    if (!attributeCached.TryGetValue(key, out var attributes))
                    {
                        return null;
                    }

                    attributes.TryGetValue(MethodOption, out var attributeMethod);

                    attributeMethod.ThrowErrorNullValue(nameof(attributeMethod), nameof(GeneratePredicate));

                    var methodOption = (LambdaMethod)attributeMethod.Value;

                    attributes.TryGetValue(NameProperty, out var attributeName);

                    var property = cacheRepository.GetProperty(typeof(TValue).Name, (string)attributeName.Value ?? key);

                    var expression = methodOption.CreateExpressionPerType(parameter, property, value);

                    expression.ThrowErrorNullValue(nameof(expression), nameof(GeneratePredicate));

                    predicate = ExpressionMergeFacade.CreateExpression(predicate, expression, parameter, mergeOption);

                    attributes.TryGetValue(MergeOption, out var attributeMerge);

                    mergeOption = !attributeMerge.Value.IsNull() ? (LambdaMerge)attributeMerge.Value : LambdaMerge.And;
                }
            }
            return predicate;
        }

        /// <summary>
        /// Create an expression per type;
        /// </summary>
        /// <param name="type">Type expression to create</param>
        /// <param name="parameter">Parameter to will be used to make an expression</param>
        /// <param name="property">Property to will be used to make an expression</param>
        /// <param name="value">Value to will be used to make an expression</param>
        /// <returns></returns>
        private static Expression CreateExpressionPerType(
            this LambdaMethod type,
            ParameterExpression parameter,
            PropertyInfo property,
            object value)
        {
            parameter.ThrowErrorNullValue(nameof(parameter), nameof(CreateExpressionPerType));

            var memberExpression = Expression.Property(parameter, property);
            var constant = Expression.Constant(value);

            switch (type)
            {
                case LambdaMethod.Contains:
                    var result = FilterFacade.Contains(constant, memberExpression, value);
                    return result;
                case LambdaMethod.GreaterThan:
                    result = FilterFacade.GreaterThan(constant, memberExpression, value);
                    return result;
                case LambdaMethod.LessThan:
                    result = FilterFacade.LessThan(constant, memberExpression, value);
                    return result;
                case LambdaMethod.GreaterThanOrEqual:
                    result = FilterFacade.GreaterThanOrEqual(constant, memberExpression, value);
                    return result;
                case LambdaMethod.LessThanOrEqual:
                    result = FilterFacade.LessThanOrEqual(constant, memberExpression, value);
                    return result;
                default:
                    result = FilterFacade.Equal(constant, memberExpression, value);
                    return result;
            }
        }

        private static bool IsValidValue(object value) =>
            value.IsNotEqualDateTimeMaxMinValue() || value.IsStringNotNullOrEmpty();
    }
}

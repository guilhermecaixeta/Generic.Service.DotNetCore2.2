﻿using Generic.Repository.Cache;
using Generic.Repository.Exceptions;
using Generic.Repository.Extension.Validation;
using System;
using System.Collections.Generic;

namespace Generic.Repository.ThrowError
{
    /// <summary>Throw an error if the condition is attempt.</summary>
    public static class ThrowErrorIf
    {
        /// <summary>
        /// Fields the is not equals.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="comparable">The comparable.</param>
        /// <exception cref="NotEqualsFieldException"></exception>
        public static void FieldNoHasSameValue(
            object value,
            object comparable)
        {
            if (value != comparable)
            {
                throw new NotEqualsFieldException(value.ToString(), comparable.ToString());
            }
        }

        /// <summary>
        /// Fields the is not equals.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="comparable">The comparable.</param>
        /// <exception cref="TException"></exception>
        public static void FieldNoHasSameValue<TException>(
            object value,
            object comparable)
        where TException : Exception, new()
        {
            if (value != comparable)
            {
                throw new TException();
            }
        }

        /// <summary>
        /// Determines whether [is empty or null string] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="nameParameter">The name parameter.</param>
        /// <param name="nameMethod">The name method.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void IsEmptyOrNullString(
            string value,
            string nameParameter,
            string nameMethod)
        {
            var result = !value.IsStringNotNullOrEmpty();
            if (result)
            {
                throw new ArgumentNullException($"{nameParameter} MethodName > {nameMethod}");
            }
        }

        /// <summary>Determines whether [is empty or null string] [the specified object].</summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="obj">The object.</param>
        /// <exception cref="TException"></exception>
        public static void IsEmptyOrNullString<TException>(string obj)
        where TException : Exception, new()
        {
            var result = !obj.IsStringNotNullOrEmpty();
            if (result)
            {
                throw new TException();
            }
        }

        /// <summary>Determines whether [is less than or equals zero] [the specified value].</summary>
        /// <param name="value">The value.</param>
        /// <exception cref="LessThanOrEqualsZeroException">val &lt;= 0</exception>
        public static void IsLessThanOrEqualsZero(int value)
        {
            if (value <= 0)
            {
                throw new LessThanOrEqualsZeroException(value.ToString());
            }
        }

        /// <summary>Determines whether [is less than zero] [the specified value].</summary>
        /// <param name="value">The value.</param>
        /// <exception cref="LessThanZeroException"></exception>
        public static void IsLessThanZero(int value)
        {
            if (value < 0)
            {
                throw new LessThanZeroException(value.ToString());
            }
        }

        /// <summary>Determines whether [is null or empty list] [the specified object].</summary>
        /// <typeparam name="TList">The type of the list.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="nameParameter">The name parameter.</param>
        /// <param name="nameMethod">The name method.</param>
        /// <exception cref="ListNullOrEmptyException"></exception>
        public static void IsNullOrEmptyList<TList>(
            IEnumerable<TList> obj,
            string nameParameter,
            string nameMethod)
        {
            var result = obj.HasAny();
            if (!result)
            {
                throw new ListNullOrEmptyException($"{nameParameter} MethodName > {nameMethod}");
            }
        }

        /// <summary>Determines whether [is null or empty list] [the specified object].</summary>
        /// <typeparam name="TList">The type of the list.</typeparam>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="obj">The object.</param>
        /// <exception cref="TException"></exception>
        public static void IsNullOrEmptyList<TList, TException>(IEnumerable<TList> obj)
        where TException : Exception, new()
        {
            var result = obj.HasAny();
            if (!result)
            {
                throw new TException();
            }
        }

        /// <summary>Determines whether [is null value] [the specified object].</summary>
        /// <param name="obj">The object.</param>
        /// <param name="nameParameter">The name parameter.</param>
        /// <param name="nameMethod">The name method.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void IsNullValue(
            object obj,
            string nameParameter,
            string nameMethod)
        {
            var result = obj.IsNull();
            if (result)
            {
                throw new ArgumentNullException($"{nameParameter} MethodName > {nameMethod}");
            }
        }

        /// <summary>Determines whether [is null value] [the specified object].</summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="obj">The object.</param>
        /// <exception cref="TException"></exception>
        public static void IsNullValue<TException>(object obj)
        where TException : Exception, new()
        {
            var result = obj.IsNull();
            if (result)
            {
                throw new TException();
            }
        }

        /// <summary>Throws the error type is not equal to T.</summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="obj">The object.</param>
        /// <exception cref="InvalidTypeException"></exception>
        public static void IsTypeNotEquals<T>(object obj)
        {
            var isTypeValid = obj.IsType<T>();

            if (!isTypeValid)
            {
                throw new InvalidTypeException(obj.GetType().Name);
            }
        }

        /// <summary>Throws the error if the type is not allowed.</summary>
        /// <typeparam name="T">Type not allowed.</typeparam>
        /// <param name="obj">The object.</param>
        /// <exception cref="InvalidTypeException"></exception>
        public static void TypeIsNotAllowed<T>(object obj)
        {
            var isValidType = obj.IsType<T>();
            if (isValidType)
            {
                throw new InvalidTypeException(obj.GetType().Name);
            }
        }

        /// <summary>Check if cache was initialized.</summary>
        /// <param name="cacheRepository">The cache repository.</param>
        /// <exception cref="CacheNotInitializedException">ThrowErrorIf</exception>
        internal static void HasNoCache(ICacheRepository cacheRepository, string value)
        {
            if (cacheRepository.IsNull())
            {
                throw new CacheNotInitializedException(value);
            }
        }
    }
}
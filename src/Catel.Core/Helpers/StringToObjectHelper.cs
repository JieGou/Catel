﻿namespace Catel
{
    using System;
    using System.Globalization;
    using System.Text;
    using Catel.Data;
    using Collections;
    using Logging;
    using Reflection;

    /// <summary>
    /// String to object helper class that converts a string to the right object if possible.
    /// </summary>
    public static class StringToObjectHelper
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes static members of the <see cref="StringToObjectHelper"/> class.
        /// </summary>
        static StringToObjectHelper()
        {
            DefaultCulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Gets or sets the default culture to use for parsing.
        /// </summary>
        /// <value>The default culture.</value>
        public static CultureInfo? DefaultCulture { get; set; }

        /// <summary>
        /// Converts a string to a boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The boolean value of the string.</returns>
        public static bool ToBool(string value)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(bool);
            }

            if (string.Equals("0", value, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals("1", value, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(value, "true", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            return bool.Parse(value);
        }

        /// <summary>
        /// Converts a string to a byte.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The byte array value of the string.</returns>
        public static byte ToByte(string value)
        {
            var intValue = ToInt(value);
            return (byte)intValue;
        }

        /// <summary>
        /// Converts a string to a byte array.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The byte array value of the string.</returns>
        public static byte[] ToByteArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<byte>();
            }

            var encoding = UTF8Encoding.UTF8;
            return encoding.GetBytes(value);
        }

        /// <summary>
        /// Converts a string to a date/time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The date/time value of the string.</returns>
        public static DateTime ToDateTime(string value)
        {
            return ToDateTime(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a date/time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The date/time value of the string.</returns>
        public static DateTime ToDateTime(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return DateTime.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a timespan.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The timespan value of the string.</returns>
        public static TimeSpan ToTimeSpan(string value)
        {
            return ToTimeSpan(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a timespan.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The timespan value of the string.</returns>
        public static TimeSpan ToTimeSpan(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(TimeSpan);
            }

            return TimeSpan.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a decimal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The decimal value of the string.</returns>
        public static decimal ToDecimal(string value)
        {
            return ToDecimal(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a decimal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The decimal value of the string.</returns>
        public static decimal ToDecimal(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(decimal);
            }

            return decimal.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a double.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The double value of the string.</returns>
        public static double ToDouble(string value)
        {
            return ToDouble(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a double.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The double value of the string.</returns>
        public static double ToDouble(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(double);
            }

            return double.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a float.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The float value of the string.</returns>
        public static float ToFloat(string value)
        {
            return ToFloat(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a float.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The float value of the string.</returns>
        public static float ToFloat(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(float);
            }

            return float.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a guid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The guid value of the string.</returns>
        public static Guid ToGuid(string value)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(Guid);
            }

            return new Guid(value);
        }

        /// <summary>
        /// Converts a string to a short.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The short value of the string.</returns>
        public static short ToShort(string value)
        {
            return ToShort(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a short.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The short value of the string.</returns>
        public static short ToShort(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(short);
            }

            return short.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to an unsigned short.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The unsigned short value of the string.</returns>
        public static ushort ToUShort(string value)
        {
            return ToUShort(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to an unsigned short.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The unsigned short value of the string.</returns>
        public static ushort ToUShort(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(ushort);
            }

            return ushort.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to an integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The integer value of the string.</returns>
        public static int ToInt(string value)
        {
            return ToInt(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to an integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The integer value of the string.</returns>
        public static int ToInt(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(int);
            }

            return int.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to an unsigned integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The unsigned integer value of the string.</returns>
        public static uint ToUInt(string value)
        {
            return ToUInt(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to an unsigned integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The unsigned integer value of the string.</returns>
        public static uint ToUInt(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(uint);
            }

            return uint.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a long.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The long value of the string.</returns>
        public static long ToLong(string value)
        {
            return ToLong(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to a long.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The long value of the string.</returns>
        public static long ToLong(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(long);
            }

            return long.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to an unsigned long.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The unsigned long value of the string.</returns>
        public static ulong ToULong(string value)
        {
            return ToULong(value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to an unsigned long.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The unsigned long value of the string.</returns>
        public static ulong ToULong(string value, CultureInfo? cultureInfo)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default(ulong);
            }

            return ulong.Parse(value, cultureInfo);
        }

        /// <summary>
        /// Converts a string to a Uri.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The Uri value of the string.</returns>
        public static Uri? ToUri(string value)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return new Uri(value, UriKind.RelativeOrAbsolute);
        }

        /// <summary>
        /// Converts a string to a Type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The Type value of the string.</returns>
        public static Type? ToType(string value)
        {
            value = CleanString(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return Type.GetType(value);
        }

        /// <summary>
        /// Converts a string to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The string value of the string.</returns>
        public static string? ToString(string value)
        {
            return value;
        }

        /// <summary>
        /// Converts a string to the right target type, such as <see cref="string"/>, <see cref="bool"/> and <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The value to convert to the specified target type.</param>
        /// <returns>The converted value. If the <paramref name="value"/> is <c>null</c>, this method will return <c>null</c>.</returns>
        public static TTarget? ToRightType<TTarget>(string value)
        {
            return (TTarget?)ToRightType(typeof(TTarget), value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to the right target type, such as <see cref="string"/>, <see cref="bool"/> and <see cref="DateTime"/>.
        /// </summary>
        /// <param name="targetType">The target type to convert to.</param>
        /// <param name="value">The value to convert to the specified target type.</param>
        /// <returns>The converted value. If the <paramref name="value"/> is <c>null</c>, this method will return <c>null</c>.</returns>
        /// <exception cref="NotSupportedException">The specified <paramref name="targetType"/> is not supported.</exception>
        public static object? ToRightType(Type targetType, string value)
        {
            return ToRightType(targetType, value, DefaultCulture);
        }

        /// <summary>
        /// Converts a string to the right target type, such as <see cref="string" />, <see cref="bool" /> and <see cref="DateTime" />.
        /// </summary>
        /// <param name="targetType">The target type to convert to.</param>
        /// <param name="value">The value to convert to the specified target type.</param>
        /// <param name="cultureInfo">The culture information.</param>
        /// <returns>The converted value. If the <paramref name="value" /> is <c>null</c>, this method will return <c>null</c>.</returns>
        /// <exception cref="NotSupportedException">The specified <paramref name="targetType" /> is not supported.</exception>
        public static object? ToRightType(Type targetType, string value, CultureInfo? cultureInfo)
        {
            if (value is null)
            {
                return null;
            }

            if (targetType == typeof(string))
            {
                return value;
            }

            if (targetType == typeof(bool) ||
                targetType == typeof(bool?))
            {
                return BoxingCache<bool>.Default.GetBoxedValue(ToBool(value));
            }

            if (targetType == typeof(byte))
            {
                return BoxingCache<byte>.Default.GetBoxedValue(ToByte(value));
            }

            if (targetType == typeof(byte[]))
            {
                return ToByteArray(value);
            }

            if (targetType == typeof(DateTime) ||
                targetType == typeof(DateTime?))
            {
                return BoxingCache<DateTime>.Default.GetBoxedValue(ToDateTime(value, cultureInfo));
            }

            if (targetType == typeof(TimeSpan) ||
                targetType == typeof(TimeSpan?))
            {
                return BoxingCache<TimeSpan>.Default.GetBoxedValue(ToTimeSpan(value, cultureInfo));
            }

            if (targetType == typeof(decimal) ||
                targetType == typeof(decimal?))
            {
                return BoxingCache<decimal>.Default.GetBoxedValue(ToDecimal(value, cultureInfo));
            }

            if (targetType == typeof(double) ||
                targetType == typeof(double?))
            {
                return BoxingCache<double>.Default.GetBoxedValue(ToDouble(value, cultureInfo));
            }

            if (targetType == typeof(float) ||
                targetType == typeof(float?))
            {
                return BoxingCache<float>.Default.GetBoxedValue(ToFloat(value, cultureInfo));
            }

            if (targetType == typeof(Guid) ||
                targetType == typeof(Guid?))
            {
                return BoxingCache<Guid>.Default.GetBoxedValue(ToGuid(value));
            }

            if (targetType == typeof(short) ||
                targetType == typeof(short?))
            {
                return BoxingCache<short>.Default.GetBoxedValue(ToShort(value, cultureInfo));
            }

            if (targetType == typeof(ushort) ||
                targetType == typeof(ushort?))
            {
                return BoxingCache<ushort>.Default.GetBoxedValue(ToUShort(value, cultureInfo));
            }

            if (targetType == typeof(int) ||
                targetType == typeof(int?))
            {
                return BoxingCache<int>.Default.GetBoxedValue(ToInt(value, cultureInfo));
            }

            if (targetType == typeof(uint) ||
                targetType == typeof(uint?))
            {
                return BoxingCache<uint>.Default.GetBoxedValue(ToUInt(value, cultureInfo));
            }

            if (targetType == typeof(long) ||
                targetType == typeof(long?))
            {
                return BoxingCache<long>.Default.GetBoxedValue(ToLong(value, cultureInfo));
            }

            if (targetType == typeof(ulong) ||
                targetType == typeof(ulong?))
            {
                return BoxingCache<ulong>.Default.GetBoxedValue(ToULong(value, cultureInfo));
            }

            if (targetType == typeof(Uri))
            {
                return ToUri(value);
            }

            if (targetType == typeof(Type))
            {
                return ToType(value);
            }

            if (targetType.IsEnumEx())
            {
                return Enum.Parse(targetType, value, false);
            }

            throw Log.ErrorAndCreateException<NotSupportedException>($"Type '{targetType.FullName}' is not yet supported");
        }

        /// <summary>
        /// Converts a string to an enum value. If the value cannot be converted for any reason, the <paramref name="defaultValue"/>
        /// will be returned.
        /// </summary>
        /// <typeparam name="TEnumValue">The type of the enum.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The enum value representing the string.</returns>
        public static TEnumValue ToEnum<TEnumValue>(string value, TEnumValue defaultValue)
            where TEnumValue : struct, IComparable, IFormattable
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            TEnumValue enumValue;
            if (!Enum<TEnumValue>.TryParse(value, out enumValue))
            {
                enumValue = defaultValue;
            }

            return enumValue;
        }

        /// <summary>
        /// Cleans up the string, for example by removing the braces.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The cleaned up string.</returns>
        private static string CleanString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            while (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }
    }
}

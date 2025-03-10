﻿namespace Catel.MVVM.Converters
{
    using System;
    using Collections;
    using Logging;
    using Reflection;

    /// <summary>
    /// Converts the result of a method to a value. This makes it possible to bind to a method.
    /// </summary>
    /// <example>
    /// {Binding MyObject, Converter={StaticResource MethodToValueConverter}, ConverterParameter='MyMethod'}
    /// </example>
    /// <remarks>
    /// Code originally comes from http://stackoverflow.com/questions/502250/bind-to-a-method-in-wpf.
    /// <para />
    /// Original license: CC BY-SA 2.5, compatible with the MIT license.
    /// </remarks>
    [System.Windows.Data.ValueConversion(typeof(string), typeof(object))]
    public class MethodToValueConverter : ValueConverterBase
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        protected override object? Convert(object? value, Type targetType, object? parameter)
        {
            var methodName = parameter as string;
            if (value is null || methodName is null)
            {
                return value;
            }

            var bindingFlags = BindingFlagsHelper.GetFinalBindingFlags(true, true);
            var methodInfo = value.GetType().GetMethodEx(methodName, Array.Empty<Type>(), bindingFlags);
            if (methodInfo is null)
            {
                return value;
            }

            return methodInfo.Invoke(value, Array.Empty<object>());
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <returns>The value to be passed to the source object.</returns>
        /// <remarks>
        /// By default, this method returns <see cref="ConverterHelper.UnsetValue"/>. This method only has
        /// to be overridden when it is actually used.
        /// </remarks>
        protected override object? ConvertBack(object? value, Type targetType, object? parameter)
        {
            throw Log.ErrorAndCreateException<NotSupportedException>("MethodToValueConverter can only be used for one way conversion");
        }
    }
}

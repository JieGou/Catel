﻿namespace Catel.MVVM.Converters
{
    using System;
    using System.Globalization;
    using Logging;
    using Reflection;
    using System.ComponentModel;
    using System.Windows.Markup;

    /// <summary>
    /// Base class for value converters which makes it compatible between .NET and WinRT.
    /// </summary>
    public abstract class ValueConverterBase : ValueConverterBase<object>
    {
    }

    /// <summary>
    /// Base class for value converters which makes it compatible between .NET and WinRT.
    /// </summary>
    /// <typeparam name="TConvert">The type of the convert input.</typeparam>
    public abstract class ValueConverterBase<TConvert> : ValueConverterBase<TConvert, object>
    {
    }

    /// <summary>
    /// Base class for value converters which makes it compatible between .NET and WinRT.
    /// </summary>
    /// <typeparam name="TConvert">The type of the convert input.</typeparam>
    /// <typeparam name="TConvertBack">The type of the convert back input.</typeparam>
    public abstract class ValueConverterBase<TConvert, TConvertBack> : MarkupExtension, IValueConverter
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueConverterBase{TConvert, TConvertBack}"/> class.
        /// </summary>
        public ValueConverterBase()
        {
            CurrentCulture = CultureInfo.CurrentUICulture;
            SupportInversionUsingCommandParameter = true;
        }

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        /// <value>The current culture.</value>
        protected CultureInfo CurrentCulture { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this converter should support inversion of the behavior using the command parameter.
        /// </summary>
        /// <value>
        ///   <c>true</c> if inversion is supported; otherwise, <c>false</c>.
        /// </value>
        public bool SupportInversionUsingCommandParameter { get; set; }

        /// <summary>
        /// Gets or sets the linked value converter. This way it is possible to chain up several converters.
        /// </summary>
        /// <value>The link.</value>
        public IValueConverter? Link { get; set; }

        /// <summary>
        /// Gets or sets an optional <see cref="System.Type"/> value to pass to the <see cref="Convert(TConvert,System.Type,object)"/> method of the chained converter if the <see cref="Link"/>
        /// property is set.
        /// </summary>
        /// <remarks>
        /// Normally this value is ignored as it is in most implementations of <c>Convert</c>.
        /// </remarks>
        [TypeConverter(typeof(StringToTypeConverter))]
        public Type? OverrideType { get; set; }

        /// <summary>
        /// Gets or sets an optional <see cref="System.Type"/> value to pass to the <see cref="ConvertBack(TConvertBack,System.Type,object)"/> method of this instance if the <see cref="Link"/>
        /// property is set.
        /// </summary>
        /// <remarks>
        /// Normally this value is ignored as it is in most implementations of <c>ConvertBack</c>.
        /// </remarks>
        [TypeConverter(typeof(StringToTypeConverter))]
        public Type? BackOverrideType { get; set; }

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (culture is not null)
            {
                CurrentCulture = culture;
            }

            var returnValue = value;

            if (Link is not null)
            {
                var cultureToUse = culture;

                // Linked converter is set, this is not the last in the chain
                // call the linked converter, i.e. the next in the chain
                returnValue = Link.Convert(returnValue, OverrideType ?? targetType, parameter, cultureToUse);
            }

            if (!IsConvertable<TConvert>(value))
            {
                Log.Warning("Cannot convert value of type '{0}', expected type '{1}', ignoring converter results",
                    ObjectToStringHelper.ToTypeString(returnValue), typeof(TConvert));

                return ConverterHelper.UnsetValue;
            }

            returnValue = Convert((TConvert?)returnValue, targetType, parameter);

            return returnValue;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>The value to be passed to the source object.</returns>
        public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (culture is not null)
            {
                CurrentCulture = culture;
            }

            var returnValue = value;

            if (!IsConvertable<TConvertBack>(value))
            {
                Log.Warning("Cannot convert back value of type '{0}', expected type '{1}', ignoring converter results",
                    ObjectToStringHelper.ToTypeString(returnValue), typeof(TConvertBack));

                returnValue = ConverterHelper.UnsetValue;
            }

            // Call ConvertBack first because we are doing this in reverse order
            returnValue = ConvertBack((TConvertBack?)returnValue, targetType, parameter);

            if (Link is not null)
            {
                var cultureToUse = culture;

                returnValue = Link.ConvertBack(returnValue, BackOverrideType ?? targetType, parameter, cultureToUse);
            }

            return returnValue;
        }

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type" /> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        protected abstract object? Convert(TConvert? value, Type targetType, object? parameter);

        /// <summary>
        /// Determines whether the specified value is convertable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the specified value is convertable; otherwise, <c>false</c>.</returns>
        protected virtual bool IsConvertable<T>(object? value)
        {
            var canConvert = true;
            if (ReferenceEquals(value, null))
            {
                if (!typeof(T).IsNullableType())
                {
                    canConvert = false;
                }
            }
            else
            {
                canConvert = value is T;
            }

            return canConvert;
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
        protected virtual object? ConvertBack(TConvertBack? value, Type targetType, object? parameter)
        {
            return ConverterHelper.UnsetValue;
        }

        /// <summary>
        /// When implemented in a derived class, returns an object that is set as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>The object value to set on the property where the extension is applied.</returns>
        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}

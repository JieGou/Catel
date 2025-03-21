﻿namespace System.ComponentModel
{
    using System;
    using System.Collections;
    using System.ComponentModel;

    /// <summary>
    /// Interface that is based on the <see cref="INotifyDataErrorInfo"/> interface, but supports warnings instead of errors.
    /// </summary>
    public interface INotifyDataWarningInfo
    {
        /// <summary>
        /// Gets a value indicating whether this object contains any field or business warnings.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has warnings; otherwise, <c>false</c>.
        /// </value>
        bool HasWarnings { get; }

        /// <summary>
        /// Occurs when the warnings have changed.
        /// </summary>
        event EventHandler<DataErrorsChangedEventArgs>? WarningsChanged;

        /// <summary>
        /// Gets the warnings for the specific property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><see cref="IEnumerable"/> of warnings.</returns>
        IEnumerable GetWarnings(string? propertyName);
    }
}

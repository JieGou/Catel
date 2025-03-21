﻿namespace Catel.Windows.Interactivity
{
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using UIEventArgs = System.EventArgs;
    using UIKeyEventArgs = System.Windows.Input.KeyEventArgs;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using Catel.Logging;
    using Catel.Windows.Input;

    /// <summary>
    /// Behavior to only allow numeric input on a <see cref="TextBox"/>.
    /// </summary>
    public class NumericTextBox : BehaviorBase<TextBox>
    {
        /// <summary>
        /// The log.
        /// </summary>
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private const string MinusCharacter = "-";
        private const string PeriodCharacter = ".";
        private const string CommaCharacter = ",";

        private static readonly HashSet<Key> AllowedKeys = new HashSet<Key>
        {
            Key.Back,
            Key.CapsLock,
            Key.LeftCtrl,
            Key.RightCtrl,
            Key.Down,
            Key.End,
            Key.Enter,
            Key.Escape,
            Key.Home,
            Key.Insert,
            Key.Left,
            Key.PageDown,
            Key.PageUp,
            Key.Right,
            Key.LeftShift,
            Key.RightShift,
            Key.Tab,
            Key.Up
        };

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            DataObject.AddPastingHandler(AssociatedObject, OnPaste);

            AssociatedObject.KeyDown += OnAssociatedObjectKeyDown;
            AssociatedObject.TextChanged += OnAssociatedObjectTextChanged;
        }

        /// <summary>
        /// Uninitializes this instance.
        /// </summary>
        protected override void Uninitialize()
        {
            DataObject.RemovePastingHandler(AssociatedObject, OnPaste);

            AssociatedObject.KeyDown -= OnAssociatedObjectKeyDown;
            AssociatedObject.TextChanged -= OnAssociatedObjectTextChanged;

            base.Uninitialize();
        }

        /// <summary>
        /// Gets or sets a value indicating whether negative values are allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allow negative]; otherwise, <c>false</c>.
        /// </value>
        public bool IsNegativeAllowed
        {
            get { return (bool)GetValue(IsNegativeAllowedProperty); }
            set
            {
#pragma warning disable WPF0036
                if (value)
                {
                    AllowedKeys.Add(Key.OemMinus);
                }
                else
                {
                    if (AllowedKeys.Contains(Key.OemMinus))
                    {
                        AllowedKeys.Remove(Key.OemMinus);
                    }
                }
#pragma warning restore WPF0036

                SetValue(IsNegativeAllowedProperty, value);
            }
        }

        /// <summary>
        /// Are negative numbers allowed
        /// </summary>
        public static readonly DependencyProperty IsNegativeAllowedProperty =
            DependencyProperty.Register(nameof(IsNegativeAllowed), typeof(bool), typeof(NumericTextBox), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether decimal values are allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if decimal values are allowed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDecimalAllowed
        {
            get { return (bool)GetValue(IsDecimalAllowedProperty); }
            set { SetValue(IsDecimalAllowedProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for IsDecimalAllowed.  This enables animation, styling, binding, etc... 
        /// </summary>
        public static readonly DependencyProperty IsDecimalAllowedProperty =
            DependencyProperty.Register(nameof(IsDecimalAllowed), typeof(bool), typeof(NumericTextBox), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the binding should be updated whenever the text changes.
        /// </summary>
        /// <value><c>true</c> if the binding should be updated; otherwise, <c>false</c>.</value>
        public bool UpdateBindingOnTextChanged
        {
            get { return (bool)GetValue(UpdateBindingOnTextChangedProperty); }
            set { SetValue(UpdateBindingOnTextChangedProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for UpdateBindingOnTextChanged.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty UpdateBindingOnTextChangedProperty = DependencyProperty.Register(nameof(UpdateBindingOnTextChanged),
            typeof(bool), typeof(NumericTextBox), new PropertyMetadata(true));

        /// <summary>
        /// Called when the <see cref="UIElement.KeyDown"/> occurs.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>        
        private void OnAssociatedObjectKeyDown(object? sender, UIKeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox is null)
            {
                return;
            }

            var notAllowed = true;
            var keyValue = GetKeyValue(e);

            var numberDecimalSeparator = GetDecimalSeparator();

            if (keyValue == numberDecimalSeparator && IsDecimalAllowed)
            {
                notAllowed = AssociatedObject.Text.Contains(numberDecimalSeparator);
            }
            else if (keyValue == MinusCharacter && IsNegativeAllowed)
            {
                notAllowed = textBox.CaretIndex > 0;
            }
            else if (AllowedKeys.Contains(e.Key) || IsDigit(e.Key))
            {
                notAllowed = (e.Key == Key.OemMinus && textBox.CaretIndex > 0 && IsNegativeAllowed);
            }

            e.Handled = notAllowed;
        }

        /// <summary>
        /// Called when the <c>TextBox.TextChanged</c> occurs.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The text change event args instance containing the event data.</param>
        private void OnAssociatedObjectTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (!UpdateBindingOnTextChanged)
            {
                return;
            }

            var binding = AssociatedObject.GetBindingExpression(TextBox.TextProperty);
            if (binding is null)
            {
                return;
            }

            var update = true;
            var text = AssociatedObject.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                // CTL-1000 NumericTextBox behavior doesn't allow some values (e.g. 2.05)
                var separator = Math.Max(text.IndexOf(CommaCharacter), text.IndexOf(PeriodCharacter));
                if (separator >= 0)
                {
                    var resetUpdate = true;

                    for (int i = separator + 1; i < text.Length; i++)
                    {
                        if (text[i] != '0')
                        {
                            resetUpdate = false;
                            break;
                        }
                    }

                    if (resetUpdate)
                    {
                        update = false;
                    }
                }

                // CTL-761
                if (string.Equals(text, "-0"))
                {
                    // User is typing -0 (whould would result in 0, which we don't want yet, maybe they are typing -0.5)
                    update = false;
                }

                if (text.StartsWith(CommaCharacter) || text.EndsWith(CommaCharacter) ||
                    text.StartsWith(PeriodCharacter) || text.EndsWith(PeriodCharacter))
                {
                    // User is typing a . or , don't update
                    update = false;
                }

                if (text.StartsWith(CommaCharacter) || text.EndsWith(CommaCharacter) ||
                    text.StartsWith(PeriodCharacter) || text.EndsWith(PeriodCharacter))
                {
                    // User is typing a . or , don't update
                    update = false;
                }
            }

            if (update)
            {
                binding.UpdateSource();
            }
        }

        /// <summary>
        /// Called when text is pasted into the TextBox.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataObjectPastingEventArgs"/> instance containing the event data.</param>
        private void OnPaste(object? sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsDecimalAllowed && !IsDigitsOnly(text))
                {
                    Log.Warning("Pasted text '{0}' contains decimal separator which is not allowed, paste is not allowed", text);

                    e.CancelCommand();
                }
                else if (!IsNegativeAllowed && text.Contains(MinusCharacter))
                {
                    Log.Warning("Pasted text '{0}' contains negative value which is not allowed, paste is not allowed", text);

                    e.CancelCommand();
                }

                if (!double.TryParse(text, NumberStyles.Any, Culture, out var tempDouble))
                {
                    Log.Warning("Pasted text '{0}' could not be parsed as double (wrong culture?), paste is not allowed", text);

                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Gets the decimal separator.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetDecimalSeparator()
        {
            var numberDecimalSeparator = Culture.NumberFormat.NumberDecimalSeparator;

            return numberDecimalSeparator;
        }

        /// <summary>
        /// Determines whether the input string only consists of digits.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns><c>true</c> if the input string only consists of digits; otherwise, <c>false</c>.</returns>
        private static bool IsDigitsOnly(string input)
        {
            foreach (var c in input)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified key is a digit.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key is digit; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsDigit(Key key)
        {
            bool isDigit;

            var isShiftKey = KeyboardHelper.AreKeyboardModifiersPressed(ModifierKeys.Shift);

            if (key >= Key.D0 && key <= Key.D9 && !isShiftKey)
            {
                isDigit = true;
            }
            else
            {
                isDigit = key >= Key.NumPad0 && key <= Key.NumPad9;
            }

            return isDigit;
        }

        /// <summary>
        /// Gets the Key to a string value.
        /// </summary>
        /// <param name="e">The key event args instance containing the event data.</param>
        /// <returns></returns>
        private string GetKeyValue(UIKeyEventArgs e)
        {
            var keyValue = string.Empty;

            if (e.Key == Key.Decimal)
            {
                keyValue = GetDecimalSeparator();
            }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                keyValue = MinusCharacter;
            }
            else if (e.Key == Key.OemComma)
            {
                keyValue = CommaCharacter;
            }
            else if (e.Key == Key.OemPeriod)
            {
                keyValue = PeriodCharacter;
            }

            return keyValue;
        }
    }
}

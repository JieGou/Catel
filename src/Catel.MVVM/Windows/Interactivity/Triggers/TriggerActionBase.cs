﻿namespace Catel.Windows.Interactivity
{
    using System;
    using System.Windows;
    using Microsoft.Xaml.Behaviors;
    using UIEventArgs = System.EventArgs;

    /// <summary>
    /// TriggerAction base class that handles a safe unsubscribe and clean up because the default
    /// TriggerAction class does not always call <c>OnDetaching</c>.
    /// <para />
    /// This class also adds some specific features such as <see cref="ValidateRequiredProperties"/>
    /// which is automatically called when the trigger action is attached.
    /// </summary>
    /// <typeparam name="T">The <see cref="UIElement"/> this trigger action should attach to.</typeparam>
    public abstract class TriggerActionBase<T> : TriggerAction<T> 
        where T : FrameworkElement
    {
        private bool _isClean = true;
        private int _loadCounter;

        private bool _isSubscribedToLoadedEvent = false;
        private bool _isSubscribedToUnloadedEvent = false;

        /// <summary>
        /// Gets a value indicating whether the <c>TriggerActionBase{T}.AssociatedObject</c> is loaded.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <c>TriggerActionBase{T}.AssociatedObject</c> is loaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsAssociatedObjectLoaded { get { return _loadCounter > 0; } }

        /// <summary>
        /// Gets a value indicating whether this instance is in design mode.
        /// </summary>
        /// <value><c>true</c> if this instance is in design mode; otherwise, <c>false</c>.</value>
        protected bool IsInDesignMode
        {
            get { return CatelEnvironment.IsInDesignMode; }
        }

        /// <summary>
        /// Called after the action is attached to an AssociatedObject.
        /// </summary>
        protected override void OnAttached()
        {
            if (IsInDesignMode)
            {
                return;
            }

            base.OnAttached();

            if (!_isSubscribedToLoadedEvent)
            {
                AssociatedObject.Loaded += OnAssociatedObjectLoadedInternal;
                _isSubscribedToLoadedEvent = true;
            }

            _isClean = false;

            ValidateRequiredProperties();

            Initialize();
        }

        /// <summary>
        /// Called when the action is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        protected override void OnDetaching()
        {
            if (IsInDesignMode)
            {
                return;
            }

            CleanUp();

            if (AssociatedObject is not null)
            {
                AssociatedObject.Loaded -= OnAssociatedObjectLoadedInternal;
                _isSubscribedToLoadedEvent = false;
            }

            base.OnDetaching();
        }

        /// <summary>
        /// Validates the required properties. This method is called when the object is attached, but before
        /// the <see cref="Initialize"/> is invoked.
        /// </summary>
        protected virtual void ValidateRequiredProperties()
        {
        }

        /// <summary>
        /// Initializes the trigger action. This method is called instead of the <see cref="OnAttached"/> which is sealed
        /// to protect the additional trigger action.
        /// </summary>
        protected virtual void Initialize()
        {

        }

        /// <summary>
        /// Uninitializes the behavior. This method is called when <see cref="OnDetaching"/> is called, or when the
        /// <see cref="TriggerAction{T}.AssociatedObject"/> is unloaded.
        /// <para />
        /// If dependency properties are used, it is very important to use <see cref="DependencyObject.ClearValue(System.Windows.DependencyProperty)"/> 
        /// to clear the value
        /// of the dependency properties in this method.
        /// </summary>
        protected virtual void Uninitialize()
        {
        }

        /// <summary>
        /// Called when the <see cref="TriggerAction{T}.AssociatedObject"/> is loaded. This method is introduced to prevent
        /// double initialization when the <see cref="TriggerAction{T}.AssociatedObject"/> is already loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnAssociatedObjectLoadedInternal(object? sender, UIEventArgs e)
        {
            _loadCounter++;

            // Yes, 1, because we just increased the counter
            if (_loadCounter != 1)
            {
                return;
            }

            if (!_isSubscribedToUnloadedEvent)
            {
                AssociatedObject.Unloaded += OnAssociatedObjectUnloadedInternal;
                _isSubscribedToUnloadedEvent = true;
            }

            OnAssociatedObjectLoaded();
        }

        /// <summary>
        /// Called when the AssociatedObject is loaded.
        /// </summary>
        protected virtual void OnAssociatedObjectLoaded()
        {
        }

        /// <summary>
        /// Called when the <see cref="TriggerAction{T}.AssociatedObject"/> is unloaded. This method is introduced to prevent
        /// double uninitialization when the <see cref="TriggerAction{T}.AssociatedObject"/> is already unloaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnAssociatedObjectUnloadedInternal(object? sender, UIEventArgs e)
        {
            _loadCounter--;

            if (_loadCounter != 0)
            {
                return;
            }

            OnAssociatedObjectUnloaded();

            CleanUp();
        }

        /// <summary>
        /// Called when the AssociatedObject is unloaded.
        /// </summary>
        protected virtual void OnAssociatedObjectUnloaded()
        {
        }

        /// <summary>
        /// Actually cleans up the trigger action because <see cref="OnDetaching"/> is not always called.
        /// </summary>
        private void CleanUp()
        {
            if (_isClean)
            {
                return;
            }

            _isClean = true;

            if (AssociatedObject is not null)
            {
                AssociatedObject.Unloaded -= OnAssociatedObjectUnloadedInternal;
                _isSubscribedToUnloadedEvent = false;
            }

            Uninitialize();
        }
    }
}

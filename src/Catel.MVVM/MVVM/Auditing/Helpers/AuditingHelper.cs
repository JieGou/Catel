﻿namespace Catel.MVVM.Auditing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Catel.Data;
    using Catel.IoC;
    using Reflection;
    using Threading;

    /// <summary>
    /// Helper for auditing which handles the complete subscription of an <see cref="IViewModel"/> instance
    /// to the current auditing manager.
    /// </summary>
    public static class AuditingHelper
    {
        private static readonly HashSet<string> KnownIgnoredPropertyNames = new HashSet<string>();
        private static readonly IObjectAdapter ObjectAdapter = ServiceLocator.Default.ResolveRequiredType<IObjectAdapter>();

        /// <summary>
        /// Initializes static members of the <see cref="AuditingHelper"/> class.
        /// </summary>
        static AuditingHelper()
        {
            KnownIgnoredPropertyNames.Add("IDataWarningInfo.Warning");
            KnownIgnoredPropertyNames.Add("INotifyDataWarningInfo.HasWarnings");
            KnownIgnoredPropertyNames.Add("IDataErrorInfo.Error");
            KnownIgnoredPropertyNames.Add("INotifyDataErrorInfo.HasErrors");
        }

        /// <summary>
        /// Registers the view model to the <see cref="AuditingManager"/>.
        /// <para />
        /// This helper will automatically unsubscribe from all events when the view model is closed.
        /// </summary>
        /// <param name="viewModel">The view model to register.</param>
        /// <remarks>
        /// This helper will call the <see cref="AuditingManager.OnViewModelCreating"/> and <see cref="AuditingManager.OnViewModelCreated"/>
        /// automatically.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        public static void RegisterViewModel(IViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            var isAuditingEnabled = AuditingManager.IsAuditingEnabled;
            if (isAuditingEnabled)
            {
                AuditingManager.OnViewModelCreating(viewModel.GetType());
            }

            SubscribeEvents(viewModel);

            if (isAuditingEnabled)
            {
                AuditingManager.OnViewModelCreated(viewModel);
            }
        }

        /// <summary>
        /// Subscribes to all events of the view model.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        private static void SubscribeEvents(IViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.CommandExecutedAsync += OnViewModelCommandExecutedAsync;
            viewModel.InitializedAsync += OnViewModelInitializedAsync;
            viewModel.SavingAsync += OnViewModelSavingAsync;
            viewModel.SavedAsync += OnViewModelSavedAsync;
            viewModel.CancelingAsync += OnViewModelCancelingAsync;
            viewModel.CanceledAsync += OnViewModelCanceledAsync;
            viewModel.ClosingAsync += OnViewModelClosingAsync;
            viewModel.ClosedAsync += OnViewModelClosedAsync;
        }

        /// <summary>
        /// Unsubscribes from all events of the view model.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="viewModel" /> is <c>null</c>.</exception>
        private static void UnsubscribeEvents(IViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.CommandExecutedAsync -= OnViewModelCommandExecutedAsync;
            viewModel.InitializedAsync -= OnViewModelInitializedAsync;
            viewModel.SavingAsync -= OnViewModelSavingAsync;
            viewModel.SavedAsync -= OnViewModelSavedAsync;
            viewModel.CancelingAsync -= OnViewModelCancelingAsync;
            viewModel.CanceledAsync -= OnViewModelCanceledAsync;
            viewModel.ClosingAsync -= OnViewModelClosingAsync;
            viewModel.ClosedAsync -= OnViewModelClosedAsync;
        }

        private static void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return;
            }

            object? propertyValue = null;
            if (!string.IsNullOrEmpty(e.PropertyName) && !KnownIgnoredPropertyNames.Contains(e.PropertyName))
            {
                ObjectAdapter.TryGetMemberValue(viewModel, e.PropertyName, out propertyValue);
            }

            AuditingManager.OnPropertyChanged(viewModel, e.PropertyName, propertyValue);
        }

        private static Task OnViewModelCommandExecutedAsync(object? sender, CommandExecutedEventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnCommandExecuted(viewModel, e.CommandPropertyName, e.Command, e.CommandParameter);

            return Task.CompletedTask;
        }

        private static Task OnViewModelInitializedAsync(object? sender, EventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelInitialized(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelSavingAsync(object? sender, SavingEventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelSaving(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelSavedAsync(object? sender, EventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelSaved(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelCancelingAsync(object? sender, CancelingEventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelCanceling(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelCanceledAsync(object? sender, EventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelCanceled(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelClosingAsync(object? sender, EventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelClosing(viewModel);

            return Task.CompletedTask;
        }

        private static Task OnViewModelClosedAsync(object? sender, EventArgs e)
        {
            if (!AuditingManager.IsAuditingEnabled)
            {
                return Task.CompletedTask;
            }

            var viewModel = sender as IViewModel;
            if (viewModel is null)
            {
                return Task.CompletedTask;
            }

            AuditingManager.OnViewModelClosed(viewModel);

            UnsubscribeEvents(viewModel);

            return Task.CompletedTask;
        }
    }
}

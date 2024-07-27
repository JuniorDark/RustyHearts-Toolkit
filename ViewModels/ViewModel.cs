using Wpf.Ui.Controls;

namespace RHToolkit.ViewModels
{
    public abstract partial class ViewModel : ObservableObject, INavigationAware
    {
        public virtual async Task OnNavigatedToAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedTo, cts.Token);
        }

        /// <summary>
        /// Handles the event that is fired after the component is navigated to.
        /// </summary>
        public virtual void OnNavigatedTo() { }

        public virtual async Task OnNavigatedFromAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        /// <summary>
        /// Handles the event that is fired before the component is navigated from.
        /// </summary>
        public virtual void OnNavigatedFrom() { }

        /// <summary>
        /// Dispatches the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to be dispatched.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected static async Task DispatchAsync(Action action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(action);
        }
    }
}

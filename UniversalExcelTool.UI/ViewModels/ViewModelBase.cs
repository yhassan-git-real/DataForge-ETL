using CommunityToolkit.Mvvm.ComponentModel;

namespace DataForgeETL.UI.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels in the application
    /// </summary>
    public partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyMessage = string.Empty;

        /// <summary>
        /// Called when the view model is navigated to
        /// </summary>
        public virtual void OnNavigatedTo(object? parameter = null)
        {
        }

        /// <summary>
        /// Called when the view model is navigated from
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
        }
    }
}

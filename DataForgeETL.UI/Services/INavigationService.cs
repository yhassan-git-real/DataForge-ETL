using System.Threading.Tasks;
using DataForgeETL.UI.ViewModels;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Service for navigating between views in the application
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to a specific view
        /// </summary>
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

        /// <summary>
        /// Navigates to a view with parameters
        /// </summary>
        void NavigateTo<TViewModel>(object parameter) where TViewModel : ViewModelBase;

        /// <summary>
        /// Gets the current view model
        /// </summary>
        ViewModelBase? CurrentViewModel { get; }

        /// <summary>
        /// Goes back to previous view
        /// </summary>
        void GoBack();

        /// <summary>
        /// Checks if can go back
        /// </summary>
        bool CanGoBack { get; }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DataForgeETL.UI.Models;
using DataForgeETL.UI.Services;

namespace DataForgeETL.UI.ViewModels
{
    /// <summary>
    /// View model for About dialog
    /// </summary>
    public partial class AboutViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _version = "2.0.0";

        [ObservableProperty]
        private string _buildDate = "October 2025";

        [ObservableProperty]
        private string _description = "Modern desktop ETL manager for DataForge ETL with Avalonia UI";

        public AboutViewModel()
        {
            // Initialize view model
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataForgeETL.UI.ViewModels;

namespace DataForgeETL.UI.Views
{
    public partial class DatabaseConfigWindow : Window
    {
        public DatabaseConfigWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public DatabaseConfigWindow(SettingsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
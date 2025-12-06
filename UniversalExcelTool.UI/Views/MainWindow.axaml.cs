using Avalonia.Controls;
using DataForgeETL.UI.Services;

namespace DataForgeETL.UI.Views
{
    public partial class MainWindow : Window
    {
        public static AvaloniaNotificationService? NotificationService { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize notification service
            NotificationService = new AvaloniaNotificationService();
            NotificationService.Initialize(this);
        }
    }
}

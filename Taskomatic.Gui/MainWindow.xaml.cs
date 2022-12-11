using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Taskomatic.Core;

namespace Taskomatic.Gui
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            InitializeComponent();
        }

        private async void InitializeComponent() // TODO[#21]: NoAwait
        {
            var config = await ConfigService.LoadConfig();
            DataContext = new ApplicationViewModel(config);
        }
    }
}

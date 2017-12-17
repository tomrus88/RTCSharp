using System.Windows;

namespace RTCSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RTCViewModel model;

        public MainWindow()
        {
            InitializeComponent();

            model = new RTCViewModel();

            DataContext = model;
        }
    }
}

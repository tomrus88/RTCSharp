using System;
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

            try
            {
                model = new RTCViewModel();
            }
            catch (Exception exc)
            {
                MessageBox.Show(string.Format("Driver initialization error:\n{0}", exc.Message), "Fatal Error!");
                Close();
            }

            DataContext = model;
        }
    }
}

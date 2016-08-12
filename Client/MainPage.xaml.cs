using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        smartChairController m_smartChairController;
        public MainPage()
        {
            m_smartChairController = new smartChairController();
            this.DataContext = m_smartChairController;
            this.InitializeComponent();
            
        }


        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Frame.Navigate(typeof(initializeChair));
            m_smartChairController.onInitialize();
        }

        private void HyperlinkButton2_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(viewWeeklySummary));
        }

        private void HyperlinkButton3_Click(object sender, RoutedEventArgs e)
        {
            //navigate to login
        }
    }
}

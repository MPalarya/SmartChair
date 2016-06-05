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
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class viewWeeklySummary : Page
    {
        public viewWeeklySummary()
        {
            this.DataContext = new weeklySummaryController();
            this.InitializeComponent();
            this.Loaded += ViewWeeklySummary_Loaded;
        }

        private void ViewWeeklySummary_Loaded(object sender, RoutedEventArgs e)
        {
            List<alertData> data = new List<alertData>();

            data.Add(new alertData() { time = new DateTime(2016, 6, 5, 10, 15, 0), alertScale = 1});
            data.Add(new alertData() { time = new DateTime(2016, 6, 2, 14, 23, 0) ,alertScale = 5});
            data.Add(new alertData() { time = new DateTime(2016, 6, 1, 14, 20, 0) ,alertScale = 22 });

            (weeklySummary.Series[0] as LineSeries).ItemsSource = data;
            
        }
    }
}

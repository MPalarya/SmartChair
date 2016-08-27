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
        smartChairServerClient smartClientServerClient = smartChairServerClient.Instance;
        public viewWeeklySummary()
        {
            this.InitializeComponent();
            smartClientServerClient.dayData += SmartClientServerClient_dayData;
        }

        private void SmartClientServerClient_dayData(object sender, dayDataEventArgs e)
        {
            List<alertData> data = new List<alertData>();
            foreach (var item in e.DayDataPoints)
            {
                data.Add(new alertData() { time = item.datetime, alertScale = item.pressure });
            }

            (weeklySummary.Series[0] as LineSeries).ItemsSource = data;
        }
      

        private void HyperlinkButton2_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            smartClientServerClient.getLogsByDateTimeBounds(fromDate.Date.DateTime, toDate.Date.DateTime);
        }
        
    }
}

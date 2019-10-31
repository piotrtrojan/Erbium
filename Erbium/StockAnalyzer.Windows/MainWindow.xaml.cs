using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Navigation;
using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Ticker.Focus();
        }

        // Using async/await while calling WebClient.
        private async void Search_Click_v1(object sender, RoutedEventArgs e)
        {
            #region Before loading stock data
            var watch = new Stopwatch();
            watch.Start();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
            #endregion
            try
            {
                await GetStocks();
            }
            catch (Exception ex)
            {
                Notes.Text = ex.Message;
            }
            
            
            #region After stock data is loaded
            StocksStatus.Text = $"Loaded stocks for {Ticker.Text} in {watch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
            #endregion
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            #region Before loading stock data
            var watch = new Stopwatch();
            watch.Start();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
            #endregion

            var lines = File.ReadAllLines(@"C:\GIT\Erbium\Data\StockPrices_Small.csv");
            var data = new List<StockPrice>();
            foreach (var line in lines.Skip(1))
            {
                var segments = line.Split(',');

                for (var i = 0; i < segments.Length; i++) segments[i] = segments[i].Trim('\'', '"');
                var price = new StockPrice
                {
                    Ticker = segments[0],
                    TradeDate = DateTime.ParseExact(segments[1], "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture),
                    Volume = Convert.ToInt32(segments[6], CultureInfo.InvariantCulture),
                    Change = Convert.ToDecimal(segments[7], CultureInfo.InvariantCulture),
                    ChangePercent = Convert.ToDecimal(segments[8], CultureInfo.InvariantCulture),
                };
                data.Add(price);
            }
            Stocks.ItemsSource = data.Where(p => p.Ticker == Ticker.Text);


            #region After stock data is loaded
            StocksStatus.Text = $"Loaded stocks for {Ticker.Text} in {watch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
            #endregion
        }


        private void KeyUpHandler(object sender, RoutedEventArgs e)
        {
            var keyPressed = ((KeyEventArgs)e).Key;
            if (keyPressed == Key.Enter)
            {
                Search.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        public async Task GetStocks()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"http://localhost:61363/api/stocks/{Ticker.Text}");

                try
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
                    Stocks.ItemsSource = data;
                }
                catch (Exception ex)
                {
                    Notes.Text += ex.Message;
                }
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

using AbataLibrary;
using AbataLibrary.Controllers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Abajure
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        SongProvider _songProvider;
        private InAppNotification _test;
        public TestPage()
        {
            this.InitializeComponent();
            Loaded += (sender, e) => { this.OnXamlRendered(this); };
            _songProvider = SongProvider.GetSongProvider();
            _songProvider.ScanComplete += _songProvider_ScanComplete;
        }

        public void OnXamlRendered(FrameworkElement control)
        {
            _test = control.FindChild<InAppNotification>();
        }

        private void _songProvider_ScanComplete(object sender, SongScanEventArgs e)
        {
            if (e.Songs?.Count > 0)
            {
                _test.Show($"Scan complete. New songs added: {e.Songs.Count}");
            }
            else
            {
                _test.Show("⚠ No new songs found");
            }
            this.btnTest.IsEnabled = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.btnTest.IsEnabled = false;
            SongProvider sp = SongProvider.GetSongProvider();
            sp.ScanLib();
            while (!sp.IsScanComplete)
                await Task.Delay(1000);

        }

        private void BtnLoadSongs_Click(object sender, RoutedEventArgs e)
        {
            AbataProvider aba = AbataProvider.GetProvider();
            var songs = aba.GetSongs();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

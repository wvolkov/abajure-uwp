using AbataLibrary;
using AbataLibrary.Controllers;
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
        public TestPage()
        {
            this.InitializeComponent();
            TestFunction();
        }

        private async void TestFunction()
        {
            SongProvider sp = SongProvider.GetSongProvider();
            sp.ScanLib();
            while (!sp.IsScanComplete)
                await Task.Delay(1000);
            AbataProvider aba = AbataProvider.GetProvider();
            aba.InsertSongs(sp.SongSet);
        }
    }
}

using Abajure.Controllers;
using Abajure.Entities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Abajure.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AbajureLyrics : Page
    {
        Song _song;
        PlayerProvider _provider;
        LyricLineSet _lyrics;

        public AbajureLyrics()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var song = e.Parameter as Song;
            _provider = await PlayerProvider.GetPlayerProvider();
            if (song != null)
            {
                _song = song;
                SetAlbumCover();
                var lyrics = await TryGetLyrics();
                if (lyrics != null)
                {
                    _lyrics = lyrics;
                    lvLyrics.ItemsSource = _lyrics;
                    if (_lyrics.Timed)
                    {
                        cmdAutoScroll.IsEnabled = true;
                        cmdAutoScroll.IsChecked = true;
                        _provider.MediaTimeChanged += _provider_MediaTimeChanged;
                    }
                }
                else
                {
                    lvLyrics.Visibility = Visibility.Collapsed;
                    tbNotFound.Visibility = Visibility.Visible;
                }
            }

            // Register for hardware and software back request from the system
            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += OnBackRequested;
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private async void SetAlbumCover()
        {
            using (StorageItemThumbnail thumb = await _song.GetThumbNail())
            {
                if (thumb != null && thumb.Type == ThumbnailType.Image)
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(thumb);
                    AlbumPlaceHolder.Source = bitmapImage;
                }
            }
        }

        private async void _provider_MediaTimeChanged(PlayerProvider sender, MediaTimeChangedEventArgs e)
        {
            if (_lyrics != null)
            {
                var lyricLine = _lyrics[e.ElapsedTime];
                int inx;
                if (lyricLine != null)
                {
                    inx = _lyrics.IndexOf(lyricLine);
                    var item = lvLyrics.Items[inx];

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        try
                        {
                            lvLyrics.ScrollIntoView(lvLyrics.Items[inx], ScrollIntoViewAlignment.Leading);
                            lvLyrics.SelectedIndex = inx;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });
                }
            }
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            // Mark event as handled so we don't get bounced out of the app.
            e.Handled = true;
            // Page above us will be our master view.
            // Make sure we are using the "drill out" animation in this transition.
            if (_lyrics != null && _lyrics.Timed && cmdAutoScroll.IsPressed)
                _provider.MediaTimeChanged -= _provider_MediaTimeChanged;
            Frame.Navigate(typeof(PlaylistPage), "Back", new EntranceNavigationTransitionInfo());
        }

        private async Task<LyricLineSet> TryGetLyrics()
        {
            return await LyricsProvider.GetLyricsAsync(_song.Title, _song.Artist, _song.Album);
        }

        private void CmdAutoScroll_Click(object sender, RoutedEventArgs e)
        {
            if (_lyrics != null && _lyrics.Timed)
            {
                var toggleButton = ((AppBarToggleButton)sender);
                if (toggleButton.IsChecked.Value)
                    _provider.MediaTimeChanged += _provider_MediaTimeChanged;
                else
                    _provider.MediaTimeChanged -= _provider_MediaTimeChanged;
            }
        }
    }
}

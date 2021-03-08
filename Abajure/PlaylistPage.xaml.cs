using AbataLibrary.Controllers;
using AbataLibrary.Entities;
using AbataLibrary.Entities.UI;
using Abajure.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Abajure
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistPage : Page
    {
        PlayerProvider _playerProvider;
        SongProvider _songProvider;

        public PlaylistPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitPlayerProvider();
            InitSongProvider();

            if (tbHeader.Text != "")
                FilterSongsBy(tbHeader.Text);
        }

        private async Task<bool> InitPlayerProvider()
        {
            _playerProvider = await PlayerProvider.GetPlayerProvider();
            UpdateUIcmdXspeed(String.Format("x{0:0.0}", _playerProvider.Settings.PlayBackRate));
            UpdateUIslider();
            _playerProvider.MediaOpenOperationCompleted += _provider_MediaOpenOperationCompleted;
            _playerProvider.MediaTimeChanged += _provider_MediaTimeChanged;
            _playerProvider.MediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;

            return true;
        }

        private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void InitSongProvider()
        {
            _songProvider = SongProvider.GetSongProvider();
            _songProvider.ScanComplete += _songProvider_ScanComplete;
            lvMusicFiles.ItemsSource = _songProvider.SongSet;
        }

        private void _songProvider_ScanComplete(object sender, EventArgs e)
        {
            lvMusicFiles.IsItemClickEnabled = true;
            _progressRing.IsActive = false;
             _playerProvider.SetMediaPlaybackList(_songProvider.SongSet);
        }

        private void UpdateUIslider()
        {
            if (_playerProvider.MediaSource != null)
            {
                InitSlidersValues(0, _playerProvider.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds, 1);
                _tbTimeFrom.Text = "00:00";
                _tbTimeTo.Text = _playerProvider.CurrentDuration.ToString("mm\\:ss");
            }
        }

        private async void _provider_MediaTimeChanged(PlayerProvider sender, MediaTimeChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _positionSlider.Value = e.TotalSeconds;
                _tbTimeFrom.Text = e.ElapsedTime.ToString("mm\\:ss");
            });
        }

        private async void _provider_MediaOpenOperationCompleted(object sender, EventArgs e)
        {
            await Task.Delay(500);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateUIslider);
        }


        private void LvMusicFiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Song selItem)
            {
                _playerProvider.ABRefresh();
                SetABButtonGlyph(_playerProvider.ABCurrentStatus);
                _playerProvider.ChangeCurrentPlayingSong(selItem);
                //_playerProvider.SetMediaSourceAsync(selItem);
                abBtnPlayPause.Icon = new SymbolIcon(Symbol.Pause);

            }
        }



        private void InitSlidersValues(double min, double max, double step)
        {
            _positionSlider.Minimum = min;
            _positionSlider.Maximum = max;
            _positionSlider.StepFrequency = step;

            _sliderMarkA.Minimum = min;
            _sliderMarkA.Maximum = max;
            _sliderMarkA.StepFrequency = step;

            _sliderMarkB.Minimum = min;
            _sliderMarkB.Maximum = max;
            _sliderMarkB.StepFrequency = step;
        }


        private void AbBtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (abBtnPlayPause.Icon is SymbolIcon si)
                switch (si.Symbol)
                {
                    case Symbol.Pause:
                        _playerProvider.MediaPlayer.Pause();
                        abBtnPlayPause.Icon = new SymbolIcon(Symbol.Play);
                        break;
                    case Symbol.Play:
                        _playerProvider.MediaPlayer.Play();
                        abBtnPlayPause.Icon = new SymbolIcon(Symbol.Pause);
                        break;
                }

        }


        private void AbBtnFF_Click(object sender, RoutedEventArgs e)
        {
            _playerProvider.MediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(10);
        }

        private void AbBtnBF_Click(object sender, RoutedEventArgs e)
        {
            _playerProvider.MediaPlayer.PlaybackSession.Position -= TimeSpan.FromSeconds(10);
        }


        private async void AbBtnXspeed_Click(object sender, RoutedEventArgs e)
        {
            PopupMenu popUp = new PopupMenu();
            popUp.Commands.Add(new UICommand("x4.0", command => _playerProvider.MediaPlayer.PlaybackSession.PlaybackRate = 4.0));
            popUp.Commands.Add(new UICommand("x2.0", command => _playerProvider.MediaPlayer.PlaybackSession.PlaybackRate = 2.0));
            popUp.Commands.Add(new UICommand("x1.5", command => _playerProvider.MediaPlayer.PlaybackSession.PlaybackRate = 1.5));
            popUp.Commands.Add(new UICommand("x1.0", command => _playerProvider.MediaPlayer.PlaybackSession.PlaybackRate = 1.0));
            popUp.Commands.Add(new UICommand("x0.5", command => _playerProvider.MediaPlayer.PlaybackSession.PlaybackRate = 0.5));

            Button button = (Button)sender;
            GeneralTransform transform = button.TransformToVisual(null);
            Point point = transform.TransformPoint(new Point(button.ActualWidth / 2, 0));

            IUICommand result = await popUp.ShowAsync(point);
            if (result != null)
            {
                UpdateUIcmdXspeed(result.Label);
                _playerProvider.Settings.PlayBackRate = Convert.ToDouble(result.Label.Replace("x", ""));
                _playerProvider.Settings.Save();
            }
        }

        private void UpdateUIcmdXspeed(string label)
        {
            switch (label)
            {
                case "x1.0":
                    _abBtnXspeedIconText.Text = "";
                    break;
                default:
                    _abBtnXspeedIconText.Text = label;
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSongsBy(tbHeader.Text);
        }

        private void FilterSongsBy(string pattern)
        {
            _songProvider.Search(pattern);
            if (pattern != "" && _songProvider.SongSet != null)
                lvMusicFiles.IsItemClickEnabled = true;
            else
                lvMusicFiles.IsItemClickEnabled = _songProvider.IsScanComplete;
            lvMusicFiles.ItemsSource = _songProvider.SongSet;
            _playerProvider.SetMediaPlaybackList(_songProvider.SongSet);
        }

        private void AbBtnAB_Click(object sender, RoutedEventArgs e)
        {
            ABEnum status = _playerProvider.ABGetNextStatus();
            SetABButtonGlyph(status);
        }

        private void SetABButtonGlyph(ABEnum abEnum)
        {
            _playerProvider.ABSetMark(abEnum);
            switch (abEnum)
            {
                case ABEnum.APressed:
                    _abFontIconOne.Glyph = "\uE884";
                    _abFontIconTwo.Glyph = "";
                    _sliderMarkA.Visibility = Visibility.Visible;
                    _sliderMarkA.Value = _playerProvider.ABMarkA.TotalSeconds;
                    break;
                case ABEnum.BPressed:
                    _abFontIconOne.Glyph = "\uE884";
                    _abFontIconTwo.Glyph = "\uE882";
                    _sliderMarkB.Visibility = Visibility.Visible;
                    _sliderMarkB.Value = _playerProvider.ABMarkB.TotalSeconds;
                    break;
                case ABEnum.Released:
                    _abFontIconOne.Glyph = "";
                    _abFontIconTwo.Glyph = "";
                    _sliderMarkA.Visibility = Visibility.Collapsed;
                    _sliderMarkB.Visibility = Visibility.Collapsed;
                    _sliderMarkA.Value = 0;
                    _sliderMarkB.Value = 0;
                    break;
            }
        }

        private void AbBtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AbajureSettingsPage), null, new DrillInNavigationTransitionInfo());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            switch (e.Parameter)
            {
                case "":
                    break;
                case "Back":
                    break;
                default:
                    return;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _playerProvider.MediaOpenOperationCompleted -= _provider_MediaOpenOperationCompleted;
            _playerProvider.MediaTimeChanged -= _provider_MediaTimeChanged;
            _playerProvider.MediaPlaybackList.CurrentItemChanged -= MediaPlaybackList_CurrentItemChanged;
        }

        private void AbBtnLyrics_Click(object sender, RoutedEventArgs e)
        {
            if (_playerProvider.CurrentSong != null)
                Frame.Navigate(typeof(AbajureLyrics), _playerProvider.CurrentSong, new DrillInNavigationTransitionInfo());
        }
    }
}

using Abajure.Controllers;
using Abajure.Entities;
using Abajure.Entities.UI;
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
        PlayerProvider _provider;

        public PlaylistPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        private async Task<bool> InitPlayerProvider()
        {
            _provider = await PlayerProvider.GetPlayerProvider();
            UpdateUIcmdXspeed(String.Format("x{0:0.0}", _provider.Settings.PlayBackRate));
            UpdateUIslider();
            _provider.MediaOpenOperationCompleted += _provider_MediaOpenOperationCompleted;
            _provider.MediaTimeChanged += _provider_MediaTimeChanged;
            _provider.SongProvider.SongSet.CollectionChanged += SongSet_CollectionChanged;

            return true;

        }

        private void UpdateUIslider()
        {
            if (_provider.MediaSource != null)
            {
                InitSlidersValues(0, _provider.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds, 1);
                _tbTimeFrom.Text = "00:00";
                _tbTimeTo.Text = _provider.CurrentDuration.ToString("mm\\:ss");
            }
        }

        private void SongSet_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_provider.SongProvider.SongSet.Count > 5)
                _progressRing.IsActive = false;
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
            await System.Threading.Tasks.Task.Delay(500);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateUIslider);
        }


        private void lvMusicFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var s = e.OriginalSource;
            var selItem = e.AddedItems?.FirstOrDefault() as Song;
            if (selItem != null)
            {
                _provider.ABRefresh();
                SetABButtonGlyph(_provider.ABCurrentStatus);
                _provider.SetMediaSourceAsync(selItem);
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


        private void abBtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            var si = abBtnPlayPause.Icon as SymbolIcon;
            if (si != null)
                switch (si.Symbol)
                {
                    case Symbol.Pause:
                        _provider.MediaPlayer.Pause();
                        abBtnPlayPause.Icon = new SymbolIcon(Symbol.Play);
                        break;
                    case Symbol.Play:
                        _provider.MediaPlayer.Play();
                        abBtnPlayPause.Icon = new SymbolIcon(Symbol.Pause);
                        break;
                }

        }


        private void abBtnFF_Click(object sender, RoutedEventArgs e)
        {
            _provider.MediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(10);
        }

        private void abBtnBF_Click(object sender, RoutedEventArgs e)
        {
            _provider.MediaPlayer.PlaybackSession.Position -= TimeSpan.FromSeconds(10);
        }


        private async void AbBtnXspeed_Click(object sender, RoutedEventArgs e)
        {
            var popUp = new PopupMenu();
            popUp.Commands.Add(new UICommand("x4.0", command => _provider.MediaPlayer.PlaybackSession.PlaybackRate = 4.0));
            popUp.Commands.Add(new UICommand("x2.0", command => _provider.MediaPlayer.PlaybackSession.PlaybackRate = 2.0));
            popUp.Commands.Add(new UICommand("x1.5", command => _provider.MediaPlayer.PlaybackSession.PlaybackRate = 1.5));
            popUp.Commands.Add(new UICommand("x1.0", command => _provider.MediaPlayer.PlaybackSession.PlaybackRate = 1.0));
            popUp.Commands.Add(new UICommand("x0.5", command => _provider.MediaPlayer.PlaybackSession.PlaybackRate = 0.5));

            var button = (Button)sender;
            var transform = button.TransformToVisual(null);
            var point = transform.TransformPoint(new Point(button.ActualWidth / 2, 0));

            IUICommand result = await popUp.ShowAsync(point);
            if (result != null)
            {
                UpdateUIcmdXspeed(result.Label);
                _provider.Settings.PlayBackRate = Convert.ToDouble(result.Label.Replace("x", ""));
                _provider.Settings.Save();
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
            _provider.SongProvider.Search(pattern);
            lvMusicFiles.ItemsSource = _provider.SongProvider.SongSet;
        }

        private void AbBtnAB_Click(object sender, RoutedEventArgs e)
        {
            var status = _provider.ABGetNextStatus();
            SetABButtonGlyph(status);
        }

        private void SetABButtonGlyph(ABEnum abEnum)
        {
            _provider.ABSetMark(abEnum);
            switch (abEnum)
            {
                case ABEnum.APressed:
                    _abFontIconOne.Glyph = "\uE884";
                    _abFontIconTwo.Glyph = "";
                    _sliderMarkA.Visibility = Visibility.Visible;
                    _sliderMarkA.Value = _provider.ABMarkA.TotalSeconds;
                    break;
                case ABEnum.BPressed:
                    _abFontIconOne.Glyph = "\uE884";
                    _abFontIconTwo.Glyph = "\uE882";
                    _sliderMarkB.Visibility = Visibility.Visible;
                    _sliderMarkB.Value = _provider.ABMarkB.TotalSeconds;
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitPlayerProvider();
            if (tbHeader.Text != "")
                FilterSongsBy(tbHeader.Text);
            else
                lvMusicFiles.ItemsSource = _provider.SongProvider.SongSet;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _provider.MediaOpenOperationCompleted -= _provider_MediaOpenOperationCompleted;
            _provider.MediaTimeChanged -= _provider_MediaTimeChanged;
            _provider.SongProvider.SongSet.CollectionChanged -= SongSet_CollectionChanged;
        }

        private void AbBtnLyrics_Click(object sender, RoutedEventArgs e)
        {
            if (_provider.CurrentSong != null)
                Frame.Navigate(typeof(AbajureLyrics), _provider.CurrentSong, new DrillInNavigationTransitionInfo());
        }
    }
}

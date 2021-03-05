using Abajure.Entities;
using Abajure.Entities.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;

namespace Abajure.Controllers
{
    class PlayerProvider
    {
        public MediaPlayer MediaPlayer { get; private set; }
        public MediaPlaybackItem MediaPlaybackItem { get; private set; }
        public SongProvider SongProvider { get; private set; }
        public AbajureSettings Settings { get; private set; }
        public MediaSource MediaSource { get; private set; }
        public Song CurrentSong { get; private set; }
        public ABEnum ABCurrentStatus { get; private set; } = ABEnum.Released;
        public TimeSpan CurrentDuration { get; private set; }
        public TimeSpan ABMarkA { get; private set; } = TimeSpan.Zero;
        public TimeSpan ABMarkB { get; private set; } = TimeSpan.Zero;
        public SongSet SongSet { get { return SongProvider.SongSet; } }
        public void ABSetMark(ABEnum ab)
        {
            switch (ab)
            {
                case ABEnum.APressed:
                    ABMarkA = MediaPlayer.PlaybackSession.Position;
                    break;
                case ABEnum.BPressed:
                    ABMarkB = MediaPlayer.PlaybackSession.Position;
                    break;
                case ABEnum.Released:
                    ABMarkA = TimeSpan.Zero;
                    ABMarkB = TimeSpan.Zero;
                    break;
            }
        }

        public delegate void MediaTimeChangedHandler(PlayerProvider sender, MediaTimeChangedEventArgs e);

        public event MediaTimeChangedHandler MediaTimeChanged;
        public event EventHandler MediaOpenOperationCompleted;

        protected virtual void OnMediaTimeChanged(MediaTimeChangedEventArgs e)
        {
            MediaTimeChanged?.Invoke(this, e);
        }
        protected virtual void OnMediaOpenOperationCompleted(EventArgs e)
        {
            MediaOpenOperationCompleted?.Invoke(this, e);
        }



        private PlayerProvider() { }

        private async Task<bool> Init()
        {
            SongProvider = new SongProvider();
            SongProvider.ScanLib();

            Settings = await AbajureSettings.GetSettingsAsync();

            MediaPlayer = new MediaPlayer();
            if (Settings.AudioDeviceId != "")
            {
                var audioDevice = await DeviceInformation.CreateFromIdAsync(Settings.AudioDeviceId);
                MediaPlayer.AudioDevice = audioDevice;
            }
            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;

            return true;
        }

        public async void SetMediaSourceAsync(Song s)
        {
            CurrentSong = s;
            StorageFile sf = await StorageFile.GetFileFromPathAsync(s.SongPath);
            if (MediaSource != null)
            {
                MediaPlayer.Pause();
                MediaPlayer.Dispose();
                MediaPlayer = new MediaPlayer();
                MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
                MediaSource.OpenOperationCompleted -= _currentMedia_OpenOperationCompleted;
                MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                MediaPlayer.Source = null;
            }

            MediaSource = MediaSource.CreateFromStorageFile(sf);
            MediaSource.OpenOperationCompleted += _currentMedia_OpenOperationCompleted;

            MediaPlaybackItem = new MediaPlaybackItem(MediaSource);

            MediaPlayer.Source = MediaPlaybackItem;

            UpdateDisplayMeta(s);
        }

        public void SetMediaDevice(DeviceInformation audioDevice)
        {
            MediaPlayer.Pause();
            MediaPlayer.AudioDevice = audioDevice;
            MediaPlayer.Play();
        }

        private void _currentMedia_OpenOperationCompleted(MediaSource sender, MediaSourceOpenOperationCompletedEventArgs args)
        {
            CurrentDuration = sender.Duration.GetValueOrDefault();
            OnMediaOpenOperationCompleted(EventArgs.Empty);
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            MediaPlayer.PlaybackSession.PlaybackRate = Settings.PlayBackRate;
            MediaPlayer.Play();
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (MediaSource != null && MediaSource.Duration != TimeSpan.Zero)
            {

                double elapsedSecs = sender.Position.TotalSeconds;
                TimeSpan ts = new TimeSpan(0, 0, (int)elapsedSecs);
                if (ABCurrentStatus == ABEnum.BPressed && ABMarkB != TimeSpan.Zero)
                {
                    if (ts > ABMarkB)
                    {
                        MediaPlayer.PlaybackSession.Position = ABMarkA;
                        OnMediaTimeChanged(new MediaTimeChangedEventArgs(ABMarkA.TotalSeconds, ts));
                    }
                }
                OnMediaTimeChanged(new MediaTimeChangedEventArgs(elapsedSecs, ts));
            }
        }

        private void UpdateDisplayMeta(Song song)
        {
            MediaItemDisplayProperties props = MediaPlaybackItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Artist = song.Artist;
            props.MusicProperties.Title = song.Title;
            props.MusicProperties.AlbumTitle = song.Album;
            MediaPlaybackItem.ApplyDisplayProperties(props);
        }

        public void ABRefresh()
        {
            ABStatus.Reset();
            ABCurrentStatus = ABEnum.Released;
        }

        public ABEnum ABGetNextStatus()
        {
            ABCurrentStatus = ABStatus.GetNextStatus();
            return ABCurrentStatus;
        }

        private static PlayerProvider _provider;

        public static async Task<PlayerProvider> GetPlayerProvider()
        {
            if (_provider == null)
            {
                _provider = new PlayerProvider();
                await _provider.Init();
            }
            return _provider;
        }
    }

    class MediaTimeChangedEventArgs : EventArgs
    {
        public double TotalSeconds { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }

        public MediaTimeChangedEventArgs(double totalSeconds, TimeSpan elapsedTime)
        {
            TotalSeconds = totalSeconds;
            ElapsedTime = elapsedTime;
        }
    }

}

using Abajure.Entities;
using Abajure.Entities.UI;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Abajure.Controllers
{
    class PlayerProvider
    {
        public MediaPlayer MediaPlayer { get; private set; }
        public MediaPlaybackList MediaPlaybackList { get; private set; }
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
            if (Settings.AudioDeviceId != null)
            {
                var audioDevice = await DeviceInformation.CreateFromIdAsync(Settings.AudioDeviceId);
                MediaPlayer.AudioDevice = audioDevice;
            }
            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;

            MediaPlaybackList = new MediaPlaybackList();
            MediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            MediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
            MediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;

            return true;
        }

        private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            
        }

        private void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
        {
            CurrentDuration = sender.CurrentItem.Source.Duration.GetValueOrDefault();
            OnMediaOpenOperationCompleted(EventArgs.Empty);
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            MediaPlayer.PlaybackSession.PlaybackRate = Settings.PlayBackRate;
            var inx = sender.CurrentItemIndex;
            if (inx >= 0)
                UpdateDisplayMeta(_provider.SongSet[(int)inx]);
        }

        private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            var inx = sender.CurrentItemIndex;
            if (inx >= 0)
            {
                CurrentSong = _provider.SongSet[(int)inx];
                UpdateDisplayMeta(CurrentSong);
            }
        }

        public async void SetMediaSourceAsync(Song s, SongSet currentList)
        {
            CurrentSong = s;
            StorageFile sf = await StorageFile.GetFileFromPathAsync(s.SongPath);
            if (MediaSource != null)
            {
                MediaPlayer.Pause();
                MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                MediaPlayer.Dispose();
                MediaPlayer = new MediaPlayer();
                MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
                MediaSource.OpenOperationCompleted -= _currentMedia_OpenOperationCompleted;
                MediaPlayer.Source = null;
            }

            MediaSource = MediaSource.CreateFromStorageFile(sf);
            MediaSource.OpenOperationCompleted += _currentMedia_OpenOperationCompleted;

            var songFile = await s.AsStorageFileAsync();
            if (MediaPlaybackList != null)
                MediaPlaybackList.Items.Clear();

            if (currentList != null)
            {
                var playBackList = await currentList.AsMediaPlayBackListAsync();
                foreach (var playBackItem in playBackList.Items)
                    MediaPlaybackList.Items.Add(playBackItem);
            }
            MediaPlaybackList.MoveTo((uint)currentList.IndexOf(s));
            MediaPlayer.Source = MediaPlaybackList;
            MediaPlayer.Play();
            

            //UpdateDisplayMeta(s);
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

        private async void UpdateDisplayMeta(Song song)
        {
            var thumb = await song.GetThumbNail();
            {

                MediaItemDisplayProperties props = MediaPlaybackList.CurrentItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Artist = song.Artist;
                props.MusicProperties.Title = song.Title;
                props.MusicProperties.AlbumTitle = song.Album;


                var _systemMediaTransportControls = MediaPlayer.SystemMediaTransportControls;
                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Artist = song.Artist;
                updater.MusicProperties.AlbumArtist = song.AlbumArtist;
                updater.MusicProperties.Title = song.Title;

                if (thumb != null && thumb.Type == ThumbnailType.Image)
                {
                    var thumbStream = RandomAccessStreamReference.CreateFromStream(thumb);
                    props.Thumbnail = thumbStream;
                    updater.Thumbnail = thumbStream;
                }
                MediaPlaybackList.CurrentItem.ApplyDisplayProperties(props);
                updater.Update();
            }
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

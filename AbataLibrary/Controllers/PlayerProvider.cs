using AbataLibrary.Entities;
using AbataLibrary.Entities.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace AbataLibrary.Controllers
{
    public class PlayerProvider
    {
        public MediaPlayer MediaPlayer { get; private set; }
        public MediaPlaybackItem MediaPlaybackItem { get; private set; }
        public AbajureSettings Settings { get; private set; }
        public MediaSource MediaSource { get; private set; }
        public MediaPlaybackList MediaPlaybackList { get; private set; }
        public Song CurrentSong { get; private set; }
        public ABEnum ABCurrentStatus { get; private set; } = ABEnum.Released;
        public TimeSpan CurrentDuration { get; private set; }
        public TimeSpan ABMarkA { get; private set; } = TimeSpan.Zero;
        public TimeSpan ABMarkB { get; private set; } = TimeSpan.Zero;
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

        private SongSet _currentSongsSet;

        private PlayerProvider() { }

        private async Task<bool> Init()
        {
            Settings = await AbajureSettings.GetSettingsAsync();

            MediaPlayer = new MediaPlayer()
            {
                AudioCategory = MediaPlayerAudioCategory.Media
            };
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            MediaPlaybackList = new MediaPlaybackList()
            {
                MaxPlayedItemsToKeepOpen = 3
            };
            MediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            MediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;

            if (Settings.AudioDeviceId != null)
            {
                DeviceInformation audioDevice = await DeviceInformation.CreateFromIdAsync(Settings.AudioDeviceId);
                try
                {
                    MediaPlayer.AudioDevice = audioDevice;
                }
                catch { }
            }
            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;

            return true;
        }

        private void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
        {
            Debug.WriteLine("ItemOpened {0}", args.Item.Source.Duration);

        }

        private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            Debug.WriteLine("CurrentItemChanged {0}", args.NewItem.Source.Duration);
            CurrentDuration = args.NewItem.Source.Duration.GetValueOrDefault();
            OnMediaOpenOperationCompleted(EventArgs.Empty);
            MediaPlayer.PlaybackSession.PlaybackRate = Settings.PlayBackRate;
            int inx = (int)MediaPlaybackList.CurrentItemIndex;

            if (inx >= 0 && args.OldItem != null)
            {
                CurrentSong = _currentSongsSet[inx];
                UpdateDisplayMeta(_currentSongsSet[inx]);
            }
        }

        public void ChangeCurrentPlayingSong(Song song)
        {
            int inx = _currentSongsSet.IndexOf(song);
            if (MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                MediaPlayer.Play();
            if (inx >= 0)
            {
                MediaPlaybackList.MoveTo((uint)inx);
                //UpdateDisplayMeta(_currentSongsSet[inx]);
            }
        }

        public void SetMediaPlaybackList(SongSet songs)
        {
            _currentSongsSet = songs;

            if (MediaPlaybackList.Items.Count > 0)
                MediaPlaybackList.Items.Clear();
            foreach (Song song in songs)
                MediaPlaybackList.Items.Add(song.MediaPlaybackItem);
            MediaPlayer.Pause();
            MediaPlayer.Source = MediaPlaybackList;
        }

        public async void SetMediaSourceAsync(Song s)
        {
            CurrentSong = s;
            StorageFile sf = await StorageFile.GetFileFromPathAsync(s.SongPath);
            if (MediaSource != null)
            {
                MediaPlayer.Pause();
                MediaPlayer.Dispose();
                MediaPlayer = new MediaPlayer()
                {
                    AudioCategory = MediaPlayerAudioCategory.Media
                };                
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
            MediaSource = MediaPlaybackList.CurrentItem.Source;
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
            StorageItemThumbnail thumb = await song.GetThumbNail();
            {

                MediaItemDisplayProperties props = song.MediaPlaybackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Artist = song.Artist;
                props.MusicProperties.Title = song.Title;
                props.MusicProperties.AlbumTitle = song.Album;


                SystemMediaTransportControls _systemMediaTransportControls = MediaPlayer.SystemMediaTransportControls;
                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Artist = song.Artist;
                updater.MusicProperties.AlbumArtist = song.AlbumArtist;
                updater.MusicProperties.Title = song.Title;

                if (thumb != null && thumb.Type == ThumbnailType.Image)
                {
                    RandomAccessStreamReference thumbStream = RandomAccessStreamReference.CreateFromStream(thumb);
                    props.Thumbnail = thumbStream;
                    updater.Thumbnail = thumbStream;
                }
                song.MediaPlaybackItem.ApplyDisplayProperties(props);
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

    public class MediaTimeChangedEventArgs : EventArgs
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

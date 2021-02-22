using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Abajure.Entities
{
    [DataContract]
    class AbajureSettings
    {
        private static AbajureSettings _abajureSettings;

        [DataMember]
        private string _audioDeviceId;

        [DataMember(EmitDefaultValue =false)]
        private double _playBackRate = 1;

        public string AudioDeviceId
        {
            get
            {
                return _audioDeviceId;
            }
            set
            {
                _audioDeviceId = value;
            }
        }

        public double PlayBackRate
        {
            get
            {
                return _playBackRate;
            }
            set
            {
                _playBackRate = value;
            }
        }

        public async void Save()
        {
            IBuffer buffMsg;

            DataContractSerializer sessionSerializer = new DataContractSerializer(typeof(AbajureSettings));
            using (var stream = new MemoryStream())
            {
                sessionSerializer.WriteObject(stream, this);

                buffMsg = stream.ToArray().AsBuffer();
            }
            StorageFile sessionFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("AbajureSettings.xml", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBufferAsync(sessionFile, buffMsg);
        }

        private AbajureSettings() { }

        public static async Task<AbajureSettings> GetSettingsAsync()
        {

            if (_abajureSettings == null)
            {
                _abajureSettings = await TryReadSettingsAsync();
                if (_abajureSettings == null)
                    _abajureSettings = new AbajureSettings();
            }
            return _abajureSettings;

        }

        private static async Task<AbajureSettings> TryReadSettingsAsync()
        {
            try
            {
                DataContractSerializer sessionSerializer = new DataContractSerializer(typeof(AbajureSettings));
                using (var sessionFileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("AbajureSettings.xml"))
                {
                    var obj = sessionSerializer.ReadObject(sessionFileStream);
                    if (obj != null)
                        return (AbajureSettings)obj;
                }
            }
            catch { }
            return null;
        }
    }
}

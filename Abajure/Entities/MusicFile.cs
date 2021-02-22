using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Abajure.Entities
{
    [DataContract]
    class MusicFile
    {
        private StorageFile _file;

        public string FileName
        {
            get
            {
                return _file.Name;
            }
        }

        public string FilePath
        {
            get
            {
                return _file.Path;
            }
        }

        public string Duration
        {
            get
            {
                int h = Properties.Duration.Hours;
                return Properties.Duration.ToString(h > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
            }
        }

        public MusicProperties Properties { get; private set; }

        public BasicProperties BasicProperties { get; private set; }

        private MusicFile(StorageFile file)
        {
            _file = file;
        }

        public async Task<MusicProperties> GetMusicProperties()
        {
            return await _file.Properties.GetMusicPropertiesAsync();
        }


        public StorageFile GetFile()
        {            
            return _file;
        }

        public string GetHash()
        {
            // Create a string that contains the name of the hashing algorithm to use.
            String strAlgName = HashAlgorithmNames.Sha512;

            // Create a HashAlgorithmProvider object.
            HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm(strAlgName);

            // Create a CryptographicHash object. This object can be reused to continually
            // hash new messages.
            CryptographicHash objHash = objAlgProv.CreateHash();

            objHash.Append(CryptographicBuffer.ConvertStringToBinary(Properties.Title, BinaryStringEncoding.Utf16BE));
            objHash.Append(CryptographicBuffer.ConvertStringToBinary(FileName, BinaryStringEncoding.Utf16BE));
            objHash.Append(CryptographicBuffer.ConvertStringToBinary(Properties.Duration.Ticks.ToString(), BinaryStringEncoding.Utf16BE));

             

            IBuffer buffHash = objHash.GetValueAndReset();
            byte[] byteHashArray;
            CryptographicBuffer.CopyToByteArray(buffHash, out byteHashArray);
            int hsh = BitConverter.ToInt32(byteHashArray, 0);
            // Convert the hashes to string values (for display);
            return CryptographicBuffer.EncodeToBase64String(buffHash);//BitConverter.ToInt32(byteHashArray, 0);
        }

        public static async Task<MusicFile> CreateFile(StorageFile file)
        {
            
            MusicProperties mp = await file.Properties.GetMusicPropertiesAsync();
            BasicProperties bp = await file.GetBasicPropertiesAsync();            
            MusicFile res = new MusicFile(file) { Properties = mp, BasicProperties = bp };
            return res;
        }
    }

    class MusicFiles : ObservableCollection<MusicFile>
    {
        public MusicFiles() { }

        public MusicFiles(IEnumerable<MusicFile> col)
            : base(col) { }

        public static async Task<MusicFiles> CreateCollectionAsync(IEnumerable<StorageFile> files)
        {
            MusicFiles mf = new MusicFiles();
            foreach(StorageFile sf in files)
            {
                mf.Add(await MusicFile.CreateFile(sf));
            }
            return mf;
        }

        public static async void UpdateCollectionAsync(MusicFiles mf, IEnumerable<StorageFile> files)
        {
            foreach (StorageFile sf in files)
            {

                mf.Add(await MusicFile.CreateFile(sf));
            }
        }
    }
}

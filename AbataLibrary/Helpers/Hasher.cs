using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AbataLibrary.Helpers
{
    static class Hasher
    {
        public static async Task<string> GetHash(StorageFile file, string algorithm = "MD5")
        {
            IBuffer filebuffer = await FileIO.ReadBufferAsync(file);
            HashAlgorithmProvider hashProvider = HashAlgorithmProvider.OpenAlgorithm(algorithm);
            IBuffer buffHash = hashProvider.HashData(filebuffer);
            return CryptographicBuffer.EncodeToHexString(buffHash);
        }
    }
}

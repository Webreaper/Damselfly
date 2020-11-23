using System;
using System.IO;
using System.Security.Cryptography;

namespace Damselfly.Core.Utils
{
    public static class MD5Hash
    {
        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);

                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}

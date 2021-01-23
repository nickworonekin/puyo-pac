using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace PuyoPac.HedgeLib
{
    /// <summary>
    /// A collection of "helper" methods that aim to be useful.
    /// </summary>
    public static class Helpers
    {
        // Methods
		public static string GetFileHash(string filePath)
		{
			using (var fileStream = File.OpenRead(filePath))
			{
				var md5 = MD5.Create();
				var hash = md5.ComputeHash(fileStream);
                return BitConverter.ToString(hash);
            }
		}

        public static string CombinePaths(params string[] paths)
        {
            string combinedPath = "";
            for (int i = 0; i < paths.Length; i++)
            {
                // The only OS that doesn't use this type of slash is Windows, which doesn't care.
                if (i > 0) combinedPath += '/';

				string pth = paths[i];
				if (pth.EndsWith("/") || pth.EndsWith("\\"))
					pth = pth.Substring(0, pth.Length - 1);

                combinedPath += pth;
            }

            return combinedPath;
        }
    }
}
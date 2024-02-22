using NLog;
using SharpMeter.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpMeter
{
    /// <summary>
    /// The library constants.
    /// </summary>
    public static class LibraryConstants
    {
        /// <summary>
        /// Gets or sets the log.
        /// </summary>
        public static Logger Log { get; set; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Type of logging list.
        /// </summary>
        public static List<string> LogLevels = new List<string> { "Off", "Info", "Debug", "Trace" };

        /// <summary>
        /// Stored passwords.
        /// </summary>
        public static List<string> Passwords = LoadPasswords();

        /// <summary>
        /// Loads the passwords.
        /// </summary>
        /// <returns>A list of string.</returns>
        private static List<string> LoadPasswords()
        {
            var passwords = new List<string>();
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var specificFolder = Path.Combine(folder, "SharpMeter");
            var secretord = specificFolder + @"\passwords.SM";
            const string pass = "*?w#59qxx#PKTaj2";

            if (!Directory.Exists(specificFolder))
            {
                Log.Info($"Creating library directory in {specificFolder}");
                Directory.CreateDirectory(specificFolder);
            }

            if (!File.Exists(secretord))
            {
                File.Create(secretord);

                File.SetAttributes(secretord, File.GetAttributes(secretord) | FileAttributes.Hidden);
            }

            var length = new System.IO.FileInfo(secretord).Length;
            if (length <= 0) return passwords.Distinct().ToList();
            using (var fs = new FileStream(secretord, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            {
                // Use while not null pattern in while loop.
                var lines = new List<string>();
                while (sr.ReadLine() is { } line)
                {
                    passwords.Add(Crypto.Decrypt(line, pass));
                }
            }

            return passwords.Distinct().ToList();
        }
    }
}
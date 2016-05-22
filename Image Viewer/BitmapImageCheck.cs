using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace ImageViewer
{
    /// <summary>
    /// Provides methods for checking whether a file can likely be opened as a BitmapImage, based upon its file extension
    /// </summary>
    public static class BitmapImageCheck
    {
        #region class variables

        private static readonly RegistryKey baseKey;
        private const string WICDecoderCategory = "{7ED96837-96F0-4812-B211-F13C24117ED3}";

        #endregion

        #region constructors

        static BitmapImageCheck()
        {
            baseKey = Registry.ClassesRoot.OpenSubKey("CLSID", false);
            recalculateExtensions();
        }
        #endregion

        #region properties
        /// <summary>
        /// File extensions that are supported by decoders found elsewhere on the system
        /// </summary>
        public static ReadOnlyCollection<string> CustomSupportedExtensions { get; private set; }

        /// <summary>
        /// File extensions that are supported natively by .NET
        /// </summary>
        public static ReadOnlyCollection<string> NativeSupportedExtensions { get; private set; }

        /// <summary>
        /// File extensions that are supported both natively by NET, and by decoders found elsewhere on the system
        /// </summary>
        public static ReadOnlyCollection<string> AllSupportedExtensions { get; private set; }

        #endregion

        #region public methods
        /// <summary>
        /// Check whether a file is likely to be supported by BitmapImage based upon its extension
        /// </summary>
        /// <param name="extension">File extension (with or without leading full stop), file name or file path</param>
        /// <returns>True if extension appears to contain a supported file extension, false if no suitable extension was found</returns>
        public static bool IsExtensionSupported(string extension)
        {
            if (extension == null) return false;
            //prepare extension, should a full path be given
            if (extension.Contains("."))
            {
                extension = extension.Substring(extension.LastIndexOf('.') + 1);
            }
            extension = extension.ToUpper(CultureInfo.InvariantCulture);
            extension = extension.Insert(0, ".");

            return AllSupportedExtensions.Contains(extension);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Re-calculate which extensions are available on this system. It's unlikely this ever needs to be called outside of the constructor.
        /// </summary>
        private static void recalculateExtensions()
        {
            var cse = GetSupportedExtensions().ToArray();
            var nse = new[] { ".BMP", ".GIF", ".ICO", ".JPEG", ".PNG", ".TIFF", ".DDS", ".JPG", ".JXR", ".HDP", ".WDP" };
            CustomSupportedExtensions = new ReadOnlyCollection<string>(cse);
            NativeSupportedExtensions = new ReadOnlyCollection<string>(nse);

            var ase = new string[cse.Length + nse.Length];
            Array.Copy(nse, ase, nse.Length);
            Array.Copy(cse, 0, ase, nse.Length, cse.Length);
            AllSupportedExtensions = new ReadOnlyCollection<string>(ase);
        }

        /// <summary>
        /// Represents information about a WIC decoder
        /// </summary>
        private struct DecoderInfo
        {
            public string FileExtensions;
        }

        /// <summary>
        /// Gets a list of additionally registered WIC decoders
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<DecoderInfo> GetAdditionalDecoders()
        {
            return GetCodecKeys().Select(codecKey => new DecoderInfo
            {
                FileExtensions = Convert.ToString(codecKey.GetValue("FileExtensions", ""), CultureInfo.InvariantCulture)
            }).ToList();
        }

        private static List<string> GetSupportedExtensions()
        {
            var decoders = GetAdditionalDecoders();
            var rtnlist = new List<string>();

            foreach (var decoder in decoders)
            {
                var extensions = decoder.FileExtensions.Split(',');
                rtnlist.AddRange(extensions);
            }
            return rtnlist;
        }

        private static IEnumerable<RegistryKey> GetCodecKeys()
        {
            var result = new List<RegistryKey>();

            var categoryKey = baseKey?.OpenSubKey(WICDecoderCategory + "\\instance", false);
            if (categoryKey == null) return result;
            // Read the guids of the registered decoders
            result.AddRange(
                GetCodecGuids()
                    .Select(codecGuid => baseKey.OpenSubKey(codecGuid))
                    .Where(codecKey => codecKey != null));

            return result;
        }

        private static IEnumerable<string> GetCodecGuids()
        {
            var categoryKey = baseKey?.OpenSubKey(WICDecoderCategory + "\\instance", false);
            // Read the guids of the registered decoders
            return categoryKey?.GetSubKeyNames();
        }

        #endregion
    }
}
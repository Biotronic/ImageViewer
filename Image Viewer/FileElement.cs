using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell;

namespace ImageViewer
{
    public class FileElement
    {
        private readonly Mutex _mutex = new Mutex();

        public HashSet<string> Tags {
            get
            {
                if (_tags != null) return _tags;
                _mutex.WaitOne();
                if (_tags != null) return _tags;
                _tags = GetTags(file.Properties.System.Keywords.Value);
                _mutex.ReleaseMutex();
                return _tags;
            }
        }

        private static HashSet<string> GetTags(string[] tags)
        {
            var result = new HashSet<string>();
            if (tags == null) return result;

            foreach (var t in tags)
            {
                result.Add(t.Trim());
            }
            return result;
        }

        public string FileName { get; }
        public int Index { get; }
        public Bitmap Thumbnail => file.Thumbnail.Bitmap;
        public bool Exists => File.Exists(FileName);

        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }

        public bool Delete(bool toTheVoid)
        {
            file.Dispose();
            file = null;
            return toTheVoid ?
                FileOperationAPIWrapper.SendToVoid(FileName) : 
                FileOperationAPIWrapper.SendToRecycleBin(FileName);
        }

        private ShellFile file;
        private HashSet<string> _tags;

        public FileElement(string fileName, int index)
        {
            Index = index;
            FileName = fileName;
            file = ShellFile.FromFilePath(FileName);
        }

        public bool AddTag(string tag)
        {
            try
            {
                Tags.Add(tag);
                file.Properties.System.Keywords.Value = Tags.ToArray();
                return true;
            }
            catch
            {
                Tags.Remove(tag);
                return false;
            }
        }

        public bool RemoveTag(string tag)
        {
            Tags.Remove(tag);
            file.Properties.System.Keywords.Value = Tags.ToArray();
            try
            {
                Tags.Remove(tag);
                file.Properties.System.Keywords.Value = Tags.ToArray();
                return true;
            }
            catch
            {
                Tags.Add(tag);
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Components;

namespace ImageViewer
{
    internal class FilteredFileList
    {
        private readonly List<FileElement> _allFiles;
        private readonly LockableHashSet<TagElement> _tags = new LockableHashSet<TagElement>();
        private readonly ItemsControl _ctrl;
        private int _imageIndex;
        private Task _activeImageSetter;

        public LockableHashSet<TagElement>.LockedList GetTags()
        {
            return _tags.GetList();
        }

        public FilteredFileList(string directory, ItemsControl ctrl)
        {
            _ctrl = ctrl;
            _allFiles = Directory.EnumerateFiles(directory)
                .OrderBy(Path.GetFileName)
                .Where(BitmapImageCheck.IsExtensionSupported)
                .Select((a, i) => new FileElement(a, i))
                .ToList();


            _imageIndex = (InitialImage(Environment.GetCommandLineArgs().Length <= 1 ? null : Environment.GetCommandLineArgs()[1])?.Index).GetValueOrDefault(0);
            TaskList.StartTask(EnumerateTags);
        }

        public enum Delta
        {
            None,
            Prev,
            Next
        }

        public bool ChangeImage(Delta delta, Action<Action> onComplete)
        {
            if (!FilteredFiles.Any()) return false;

            _activeImageSetter = TaskList.StartTask(() =>
            {
                Func<int, bool> filter = i => 
                        delta == Delta.Next ? i >  _imageIndex :
                        delta == Delta.Prev ? i <  _imageIndex :
                                              i <= _imageIndex;
                var defaultValue = delta == Delta.Next
                        ? FilteredFiles.First()
                        : FilteredFiles.Last();
                
                if (delta == Delta.Next)
                    _imageIndex = FilteredFiles.FirstOrDefault(a => filter(a.Index), defaultValue).Index;
                else
                    _imageIndex = FilteredFiles.LastOrDefault(a => filter(a.Index), defaultValue).Index;

                onComplete(() => _activeImageSetter = null);
            });
            return true;
        }

        public bool IsActiveImageSetter => _activeImageSetter == null || Task.CurrentId == _activeImageSetter.Id;

        private void EnumerateTags()
        {
            var filesCopy = GetFileList();

            foreach (var f in filesCopy)
            {
                if (TaskList.Closing) break;
                if (f.Tags.Count == 0) continue;

                List<TagElement> v;

                using (var t = GetTags())
                {
                    foreach (var e in f.Tags
                        .Where(a => !t.Contains(a)))
                    {
                        t.Add(e);
                    }
                    v = t.OrderBy(a => a.TagName).ToList();
                }
                if (TaskList.Closing) break;
                _ctrl.Dispatcher.Invoke(() =>
                {
                    _ctrl.ItemsSource = null;
                    _ctrl.ItemsSource = v;
                });
            }
        }

        public List<FileElement> GetFileList()
        {
            return new List<FileElement>(_allFiles);
        }

        public void Remove(FileElement fe)
        {
            _allFiles.Remove(fe);
        }

        public bool Empty()
        {
            return !_allFiles.Any();
        }

        public IEnumerable<FileElement> FilteredFiles
        {
            get
            {
                foreach (var f in _allFiles)
                {
                    if (TaskList.Closing) yield break;
                    using (var t = GetTags())
                    {
                        if (t.Any())
                        {
                            if (t.Any(a => a.Exclude && f.HasTag(a))) continue;
                            if (t.Any(a => a.Include && !f.HasTag(a))) continue;
                            if (t.All(a => a.Union && !f.HasTag(a))) continue;
                        }
                    }

                    yield return f;
                }
            }
        }

        public FileElement InitialImage(string fileName)
        {
            return _allFiles.FirstOrDefault(a => a.FileName == fileName, _allFiles.FirstOrDefault());
        }


        public FileElement CurrentFile
        {
            get
            {
                var result = FilteredFiles.FirstOrDefault();

                foreach (var f in FilteredFiles)
                {
                    if (TaskList.Closing) return null;
                    if (f.Index != _imageIndex) continue;
                    result = f;
                    break;
                }

                return result;
            }
        }

        public void AddTag(string tag)
        {
            using (var tags = GetTags())
                if (!tags.Contains(tag))
                    tags.Add(tag);
        }
    }
}

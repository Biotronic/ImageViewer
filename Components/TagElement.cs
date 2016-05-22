using System;
using System.Windows.Media;

namespace Components
{
    public class TagElement : IComparable
    {
        private bool _include = false;
        private bool _exclude = false;
        private bool _union = false;
        private Color _color = Colors.Black;

        public TagElement(string name)
        {
            TagName = name;
            Color = new SolidColorBrush(Colors.Black);
        }

        /// <summary>
        /// The actual tag.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// Should this tag be required for matching?
        /// </summary>
        public bool Include { get { return _include; }
            set
            {
                if (value == _include) return;
                _include = value;
                _exclude = false;
                _union = false;
            }
        }

        /// <summary>
        /// Should this tag be excluded from all matches?
        /// </summary>
        public bool Exclude
        {
            get { return _exclude; }
            set
            {
                if (value == _exclude) return;
                _include = false;
                _exclude = value;
                _union = false;
            }
        }

        /// <summary>
        /// Should this tag be optional for all matches?
        /// </summary>
        public bool Union
        {
            get { return _union; }
            set
            {
                if (value == _union) return;
                _include = false;
                _exclude = false;
                _union = value;
            }
        }

        /// <summary>
        /// Ugly bleed-through of other concerns - Tag text should be highlighted in main window, and I can't find a way to have XAML call functions with per-element parameters.
        /// </summary>
        public SolidColorBrush Color
        {
            get { return new SolidColorBrush(_color); }
            set { _color = value.Color; }
        }

        // Boilerplate comparison stuff. I wish C# had sensible metaprogramming facilities.
        public override int GetHashCode()
        {
            return TagName.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            var o = obj as TagElement;
            return o == null ? 1 : string.Compare(TagName, o.TagName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            var te = obj as TagElement;
            return (object)te != null && te.TagName == TagName;
        }

        public static bool operator !=(TagElement a, TagElement b)
        {
            return (object)a != null && (object)b != null && a.TagName != b.TagName;
        }

        public static bool operator ==(TagElement a, TagElement b)
        {
            return (object)a != null && (object)b != null && a.TagName == b.TagName;
        }

        public static implicit operator TagElement(string name)
        {
            return new TagElement(name);
        }

        public static implicit operator string(TagElement tag)
        {
            return tag.TagName;
        }
    }
}

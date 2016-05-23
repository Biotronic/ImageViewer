using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageViewer
{
    public partial class MainWindow
    {
        private readonly FilteredFileList _files;
        private static MainWindow _instance;
        private WindowState _oldWindowState;
        private AnimatedPanel _tagListAnimation;

        public MainWindow()
        {
            _instance = this;
            InitializeComponent();
            _tagListAnimation = new AnimatedPanel(tagScrollList, panel, newTagBox, myGrid, MaxWidthProperty, ActualWidthProperty, () => tagScrollList.ActualWidth <= 10);

            var dir = Environment.CurrentDirectory;
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var s = Environment.GetCommandLineArgs()[1];
                if (Directory.Exists(s))
                    dir = s;
                else if (File.Exists(s))
                    dir = Path.GetDirectoryName(s);
            }

            _files = new FilteredFileList(dir, tagList);
            ChangeImage();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (newTagBox.IsFocused) return;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.Key)
            {
                case Key.Left:
                    ChangeImage(FilteredFileList.Delta.Prev);
                    break;
                case Key.Right:
                    ChangeImage(FilteredFileList.Delta.Next);
                    break;
                case Key.Delete:
                    CurrentFile.Delete(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
                    ChangeImage();
                    break;
                case Key.Space:
                    ToggleFullScreen();
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        private void ToggleFullScreen()
        {
            if (IsFullScreen)
            {
                Visibility = Visibility.Collapsed;
                WindowState = _oldWindowState;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
                _oldWindowState = WindowState;
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Visibility = Visibility.Visible;
            }
        }

        public bool IsFullScreen => WindowStyle == WindowStyle.None;

        private void UpdateImage(Action done)
        {
            var cf = CurrentFile;
            if (cf == null)
            {
                DisplayNoImage();
                return;
            }
            if (!cf.Exists)
            {
                _files.Remove(cf);
                ChangeImage();
                return;
            }
            if (!_files.IsActiveImageSetter) return;

            Dispatcher.Invoke(() => SetImageSource(cf));
            done();
        }

        private void SetImageSource(FileElement file)
        {
            Title = file.FileName;
            ((BlurEffect) image.Effect).Radius = 0;
            image.Visibility = Visibility.Visible;
            label.Visibility = Visibility.Hidden;

            var src = new BitmapImage();

            src.BeginInit();
            src.UriSource = new Uri(file.FileName);
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();
            src.Freeze();

            image.Source = src;

            using (var t = _files.GetTags())
            {
                foreach (var tag in t)
                {
                    tag.Color = TagMatch(tag);
                }
            }
            UpdateTagList();
        }

        private void ChangeImage(FilteredFileList.Delta delta = FilteredFileList.Delta.None)
        {
            ((BlurEffect) image.Effect).Radius = 25;
            label.Visibility = Visibility.Visible;
            label.Text = "Loading...";

            if (!_files.ChangeImage(delta, UpdateImage))
            {
                DisplayNoImage();
            }
        }

        private void DisplayNoImage()
        {
            Dispatcher.Invoke(() =>
            {
                image.Visibility = Visibility.Hidden;
                Title = label.Text = "No images match your filters.";
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (TaskList.Empty) return;
            e.Cancel = true;
            base.OnClosing(e);

            if (TaskList.Closing) return;
            
            TaskList.Close();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Start();
            timer.Tick += (s, a) =>
            {
                if (!TaskList.Empty) return;

                timer.Stop();
                Close();
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            TaskList.Close();
            base.OnClosed(e);
        }

        private void ChangeTag(object sender, RoutedEventArgs e)
        {
            DelayedExec(TimeSpan.FromMilliseconds(250), () => ChangeImage());
        }

        private static void DelayedExec(TimeSpan delay, Action action)
        {
            var timer = new DispatcherTimer { Interval = delay };
            timer.Tick += (s, a) =>
            {
                timer.Stop();
                action();
            };
            timer.Start();
        }

        public static SolidColorBrush TagMatch(string tag)
        {
            return CurrentFile != null && CurrentFile.HasTag(tag) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
        }

        private static FileElement CurrentFile => _instance._files.CurrentFile;

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var txt = (TextBlock) sender;
            var tag = txt.Text;
            if (CurrentFile.HasTag(tag))
                CurrentFile.RemoveTag(tag);
            else
                CurrentFile.AddTag(tag);
            txt.Foreground = TagMatch(tag);
        }

        private void UpdateTagList()
        {
            var tmp = tagList.ItemsSource;
            tagList.ItemsSource = null;
            tagList.ItemsSource = tmp;
        }

        private void newTagBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var txt = (TextBlock)sender;
            var tag = txt.Text;
            if (e.Key != Key.Enter) return;


            if (!CurrentFile.HasTag(tag)) return;

            CurrentFile.AddTag(tag);
            _files.AddTag(tag);
            txt.Text = "";
        }

        private double TagListTriggerArea => Math.Max(myGrid.ActualWidth * 0.05, 20);

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            if (pos.X < TagListTriggerArea)
            {
                _tagListAnimation.Show(() => tagScrollList.VerticalScrollBarVisibility = ScrollBarVisibility.Auto);
            }
        }

        private void HideTagList(object sender, MouseEventArgs e)
        {
            tagScrollList.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            _tagListAnimation.Hide(() => { });
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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

        public MainWindow()
        {
            _instance = this;
            InitializeComponent();

            var dir = Environment.CurrentDirectory;
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                dir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[1]);
            }

            _files = new FilteredFileList(dir, tagList);
            if (_files.Empty()) return;
            ChangeImage();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.Key)
            {
                case Key.Left:
                    ChangeImage(FilteredFileList.Delta.Prev);
                    break;
                case Key.Right:
                    ChangeImage(FilteredFileList.Delta.Prev);
                    break;
                case Key.Delete:
                    _files.CurrentFile.Delete(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
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
            var cf = _files.CurrentFile;
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
            ((BlurEffect)image.Effect).Radius = 25;
            label.Visibility = Visibility.Visible;
            label.Text = "Loading...";


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

        private void tagScrollList_MouseEnter(object sender, MouseEventArgs e)
        {
            if (tagScrollList.ActualWidth < tagScrollList.MaxWidth) return;
            tagScrollList.Width = double.NaN;
            Animate(0, 1, () => {
                tagScrollList.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            });
        }

        private void tagScrollList_MouseLeave(object sender, MouseEventArgs e)
        {
            tagScrollList.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            if (tagScrollList.ActualWidth <= MinTagViewWidth) return;
            Animate(1, 0, () =>
            {
                tagScrollList.Width = MinTagViewWidth;
            });
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

        private double MinTagViewWidth => Math.Max(myGrid.ActualWidth*0.05, 20);

        private void Animate(double from, double to, Action end)
        {
            var se = new CubicEase() {EasingMode = EasingMode.EaseInOut};

            var da1 = new DoubleAnimation(myGrid.ActualWidth * from + MinTagViewWidth, myGrid.ActualWidth * to + MinTagViewWidth, new Duration(TimeSpan.FromMilliseconds(125))) { EasingFunction = se };
            var da2 = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = se };
            da2.Completed += (o, args) =>end();
            tagScrollList.BeginAnimation(ScrollViewer.MaxWidthProperty, da1);
            tagList.BeginAnimation(ItemsControl.OpacityProperty, da2);
        }

        public static SolidColorBrush TagMatch(string tag)
        {
            return _instance._files.CurrentFile.HasTag(tag) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var tag = ((TextBlock) sender).Text;
            if (_files.CurrentFile.HasTag(tag))
                _files.CurrentFile.AddTag(tag);
            else
                _files.CurrentFile.RemoveTag(tag);
            ((TextBlock) sender).Foreground = TagMatch(tag);
        }

        private void UpdateTagList()
        {
            var tmp = tagList.ItemsSource;
            tagList.ItemsSource = null;
            tagList.ItemsSource = tmp;
        }
    }
}

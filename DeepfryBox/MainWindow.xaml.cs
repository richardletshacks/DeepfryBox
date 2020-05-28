using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Xabe.FFmpeg;

namespace DeepfryBox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const String ffmpegPath = "./ffmpeg-binaries";

        public MainWindow()
        {
            InitializeComponent();

            FFmpeg.SetExecutablesPath(ffmpegPath);
        }

        private void CloseLabel_Clicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BrowseInputButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Video Files|*.mp4;*.avi;*.mov";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                InputPathBox.Text = dlg.FileName;
            }
        }

        private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Output File|*.mp4";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                OutputPathBox.Text = dlg.FileName;
            }
        }

        private void DeepfryButton_Click(object sender, RoutedEventArgs e)
        {
            String inputPath = InputPathBox.Text;
            String outputPath = OutputPathBox.Text;

            if (!IsValidPath(inputPath) || !File.Exists(inputPath))
            {
                NotificationBar.MessageQueue.Enqueue("Input path is not valid!");
                return;
            }

            if (!IsValidPath(outputPath))
            {
                NotificationBar.MessageQueue.Enqueue("Output path is not valid!");
                return;
            }

            if(!IsFFmpegInstalled())
            {
                NotificationBar.MessageQueue.Enqueue("FFmpeg binaries not found!");
                return;
            }

            Deepfry(inputPath, outputPath);
        }

        private async void Deepfry(String inputPath, String outputPath)
        {
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
            IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault().SetCodec(VideoCodec.h264);
            IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault().SetCodec(AudioCodec.aac);

            IConversion c = FFmpeg.Conversions.New();
            c.AddStream(audioStream, videoStream);
            c.SetOutput(outputPath);
            c.SetOverwriteOutput(true);
            c.SetVideoBitrate((long)VideoBitrateSlider.Value);
            c.SetAudioBitrate((long)AudioBitrateSlider.Value);
            c.OnProgress += (sender, args) => { this.Dispatcher.Invoke(() => ConversionProgressBar.Value = args.Percent); };
            
            await c.Start();

            NotificationBar.MessageQueue.Enqueue("Complete!", "OPEN", new Action(() => Process.Start("explorer.exe", outputPath)));
        }

        private bool IsValidPath(String path)
        {
            if (path == "")
                return false;
            try
            {
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(Directory.GetParent(path).FullName);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return true;
        }

        private bool IsFFmpegInstalled()
        {
            if (!File.Exists(ffmpegPath + "/ffmpeg.exe")) return false;
            if (!File.Exists(ffmpegPath + "/ffprobe.exe")) return false;
            if (!File.Exists(ffmpegPath + "/ffplay.exe")) return false;
            return true;
        }
    }
}

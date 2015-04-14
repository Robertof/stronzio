using ImageMagick;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/**
 *  Copyright 2015 Roberto Frenna
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *  http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License. 
 */
namespace Stronzio
{
    public partial class MainWindow : Window
    {
        private long RawCalculatedSize;
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Event handlers
        /// <summary>
        /// <para>Opens a file selection window. Populates InputPath.</para>
        /// </summary>
        private void Browse(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "Image files|*.jpeg;*.jpg;*.png;*.gif";
            Nullable<bool> result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                InputPath.Text = dlg.FileName;
                CompressBtn.IsEnabled = true;
                OpenFolderBtn.IsEnabled = true;
                CalculateCompressedSize();
            }
        }

        /// <summary>
        /// <para>Fired when the value of the TargetSize updown changes.</para>
        /// <para>If the current Unit is set to %, it calls
        /// <see cref="CalculateCompressedSize"/>.</para>
        /// </summary>
        private void TargetSizeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Unit != null && GetComboBoxItemString(Unit) == "%")
                CalculateCompressedSize();
        }

        /// <summary>
        /// <para>Fired when the value of the Unit combobox changes.</para>
        /// <para>
        /// If the new Unit is %, then the current size is converted to a percentage and,
        /// if it is over 100%, it's set to 50%. <see cref="CalculateCompressedSize"/> is called.
        /// </para>
        /// <para>
        /// If the new Unit is not %, then the current percentage is converted to a size.
        /// </para>
        /// </summary>
        private void UnitChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CalculatedSize == null) return;
            string current  = (string)((ComboBoxItem)e.AddedItems[0]).Content,
                   previous = (string)((ComboBoxItem)e.RemovedItems[0]).Content;
            // retrieve the old and new indexes of the selected options
            ComboBox combo = (ComboBox)sender;
            int oldIndex = combo.Items
                                .Cast<ComboBoxItem>()
                                .ToList()
                                .FindIndex(elm => (string)elm.Content == previous),
                newIndex = combo.SelectedIndex,
                delta    = newIndex - oldIndex; // Δ = current index - previous index
            if (current != "%") // the same as newIndex != 0
            {
                CalculatedSize.Visibility = System.Windows.Visibility.Collapsed;
                TargetSize.Maximum = Int32.MaxValue;
                // perform an unit conversion
                if (oldIndex == 0 && RawCalculatedSize != 0) // old selected item == '%', Δ > 0
                    TargetSize.Value = RawCalculatedSize / Math.Pow(1024, delta);
                else if (oldIndex != 0) // old selected item == 'KB', 'MB', ..., Δ ϵ Z
                {
                    double mult = Math.Pow(1024, Math.Abs(delta));
                    // divide instead of multiplying when the delta is positive
                    if (delta > 0) mult = 1 / mult;
                    TargetSize.Value *= mult;
                }
                if (TargetSize.Value < 0.1) --Unit.SelectedIndex;
            }
            else
            {
                TargetSize.Maximum = 99;
                // convert the value back to a percentage
                long? size = null;
                try
                {
                    if (InputPath.Text != "")
                    {
                        size = new FileInfo(InputPath.Text).Length;
                        TargetSize.Value *= Math.Pow(1024, Math.Abs(delta)) / size * 100;
                    }
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
                finally
                {
                    // if targetsize has no value, or if the rounded value is over 99, set it to
                    // 50(%)
                    if (!TargetSize.Value.HasValue || (TargetSize.Value > 99 &&
                        (TargetSize.Value = Math.Round(TargetSize.Value.Value)) > 99))
                        TargetSize.Value = 50;
                    CalculateCompressedSize(size);
                }
            }
        }

        /// <summary>
        /// <para>Fired when the CompressBtn button is clicked.</para>
        /// <para>When the button is clicked:</para>
        /// <list type="number">
        /// <item>
        /// <description>
        /// The final filename is created: the original filename is taken, the extension is
        /// stripped and -compressed.jpg is added at the end of it. Then, if this new filename
        /// already exists, the user decides if it should be overwritten or renamed. The renaming
        /// process is simple: a (n) is added to the end of the filename, where n is an integer
        /// increased until an available file name is found.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// Once the final filename has been established, the GUI is locked to prevent user action
        /// and a <see cref="System.ComponentModel.BackgroundWorker"/> is created.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// The worker is started, and the compression is performed.
        /// </description>
        /// </item>
        /// </list>
        /// <seealso cref="worker_CompressImage"/>
        /// <seealso cref="worker_Completed"/>
        /// </summary>
        private void Compress(object sender, RoutedEventArgs e)
        {
            if (!TargetSize.Value.HasValue)
            {
                ShowError(Properties.Strings.TargetSizeRequired);
                return;
            }
            string name = Path.Combine(
                Path.GetDirectoryName(InputPath.Text),
                Path.GetFileNameWithoutExtension(InputPath.Text)
            ), finalPath = string.Format("{0}-compressed.jpg", name);
            if (File.Exists(finalPath))
            {
                var result = MessageBox.Show(
                    string.Format (Properties.Strings.OverwritePrompt, finalPath),
                    Properties.Strings.Question,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );
                switch (result)
                {
                    case (MessageBoxResult.No):
                        int index = 1;
                        do
                        {
                            finalPath = string.Format(
                                "{0}-compressed ({1}).jpg",
                                name,
                                index++
                            );
                        } while (File.Exists(finalPath));
                        break;
                    case (MessageBoxResult.Cancel):
                    case (MessageBoxResult.None):
                        return;
                }
            }
            this.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            var worker = new BackgroundWorker();
            worker.DoWork += worker_CompressImage;
            worker.RunWorkerCompleted += worker_Completed;
            worker.RunWorkerAsync(new string[] { InputPath.Text, GetFinalSize(), finalPath });
        }

        /// <summary>
        /// <para>Fired when the OpenFolderBtn button is clicked.</para>
        /// <para>An explorer process is spawned pointing to the input file.</para>
        /// </summary>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            string arg = "/select, \"" + InputPath.Text + "\"";
            System.Diagnostics.Process.Start("explorer.exe", arg);
        }

        /// <summary>
        /// Opens the homepage of the project (set in the XAML).
        /// </summary>
        private void OpenProjectHomePage(object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
        #endregion

        #region Worker methods
        /// <summary>
        /// <para>This method is called by the worker created in <see cref="Compress"/>.</para>
        /// <para>The compression is performed here using the methods of the Magick library.</para>
        /// <para>Exceptions are handled in <see cref="worker_Completed"/></para>
        /// <seealso cref="Compress"/>
        /// <seealso cref="worker_Completed"/>
        /// </summary>
        /// <param name="e">
        /// The event which contains the arguments, in the form of a string array. The first
        /// element contains the path of the input image, the second the desired output size
        /// and the last contains the path of the output image.
        /// </param>
        private void worker_CompressImage(object sender, DoWorkEventArgs e)
        {
            var arguments = (string[])e.Argument;
            using (MagickImage image = new MagickImage(arguments[0]))
            {
                image.Format = MagickFormat.Jpg;
                image.Quality = 100;
                image.SetDefine(MagickFormat.Jpg, "extent", arguments[1]);
                image.Write(arguments[2]);
            }
        }

        /// <summary>
        /// <para>Called when the worker completes.</para>
        /// <para>The GUI is re-enabled and if any error occurred, it is shown to the user.</para>
        /// <seealso cref="Compress"/>
        /// <seealso cref="worker_CompressImage"/>
        /// </summary>
        private void worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsEnabled = true;
            Mouse.OverrideCursor = null;
            if (e.Error != null)
                ShowError(e.Error.Message);
        }
        #endregion

        #region Generic methods
        /// <summary>
        /// <para>Calculates the final size after the compression if the Unit is set to %,
        /// using the percentage provided by the user.</para>
        /// <para>The calculated value is put into RawCalculatedSize (in bytes), and into
        /// CalculatedSize.</para>
        /// </summary>
        private void CalculateCompressedSize(long? fileSize = null)
        {
            if (GetComboBoxItemString(Unit) != "%" ||
                InputPath.Text == "" ||
                !TargetSize.Value.HasValue)
                return;
            double percentage = TargetSize.Value.Value;
            string final;
            try
            {
                RawCalculatedSize = Convert.ToInt64(
                    (fileSize.HasValue ? fileSize.Value : new FileInfo(InputPath.Text).Length)
                        * percentage / 100
                );
                final = BytesToString(RawCalculatedSize);
            }
            catch (Exception)
            {
                final = "???";
                CompressBtn.IsEnabled = false;
            }
            CalculatedSize.Visibility = System.Windows.Visibility.Visible;
            CalculatedSize.Text = string.Format("({0})", final);
        }

        /// <summary>
        /// <para>Converts byteCount to a string with a suitable unit.</para>
        /// <para>Thanks to http://stackoverflow.com/a/4975942.</para>
        /// </summary>
        /// <param name="byteCount">The number of bytes</param>
        /// <returns>The converted string.</returns>
        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        /// <summary>
        /// Given a ComboBox, it returns the stringified value of the selected item.
        /// </summary>
        /// <param name="combobox">The ComboBox</param>
        /// <returns>The stringified value of the selected item</returns>
        private string GetComboBoxItemString(ComboBox combobox)
        {
            return (string) ((ComboBoxItem)combobox.SelectedItem).Content;
        }

        /// <summary>
        /// Returns the final size picked by the user for use with
        /// <see cref="MagickImage.SetDefine"/>.
        /// </summary>
        /// <returns>An appropriate value usable with <see cref="MagickImage.SetDefine"/></returns>
        private string GetFinalSize()
        {
            string unit = GetComboBoxItemString(Unit);
            if (unit == "%")
                return string.Format("{0}b", RawCalculatedSize);
            return string.Format("{0}{1}", TargetSize.Value, unit);
        }

        /// <summary>
        /// Displays an error message in a dialog.
        /// </summary>
        /// <param name="arg0">The error message</param>
        private void ShowError(object arg0)
        {
            MessageBox.Show(
                string.Format(Properties.Strings.ErrorMsg, arg0),
                Properties.Strings.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        #endregion
    }
}

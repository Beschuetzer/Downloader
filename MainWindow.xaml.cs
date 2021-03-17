using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace Downloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Initialization Stuff
        public static readonly string startingDestination = "Enter a Destination";
        public static string startingUrl = "Enter a URL";
        public static int WindowHeight = 321;
        public static int WindowWidthNormal = 316;
        public static int WindowWidthExpanded = WindowWidthNormal * 3;
        private bool DataContextSet = false;
        public MainWindow()
        {
            InitializeComponent();
            MainMenu.Width = WindowWidthNormal;
            this.Width = WindowWidthNormal;
            this.Height= WindowHeight;
            this.MinHeight = WindowHeight;
            this.MaxHeight = WindowHeight * 10;
            this.MaxWidth = WindowWidthNormal * 3;
            this.MinWidth = WindowWidthNormal;
            InitializeDownloader();
        }
        #endregion
        #region InitializeDownloader
        private void InitializeDownloader()
        {
            //Initializing Box starting values
            urltextbox.Text = startingUrl;
            destinationtextbox.Text = startingDestination;
            urltextbox.Focus();

            //Initializing the object and making it the data context for binding in XAML.  Purpose is to allow for data validation to occur.
            Downloader downloader = new Downloader(startingUrl, startingDestination);
            this.DataContext = downloader;
            DataContextSet = true;
        }
        #endregion
        #region CommandBinding_Executed
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)      //These two methods are used by RoutedCommand and RoutedUICommand.  Put in the main Window class
        {
            String Name = ((RoutedCommand)e.Command).Name;
            #region Reset Button Command
            if (Name == "ResetBoxes")
            {
                Downloader dc = this.DataContext as Downloader;
                destinationtextbox.Text = "";
                urltextbox.Text = "";
            }
            #endregion
            #region Download Button Command
            else if (Name == "Download")
            {
                Downloader dc = this.DataContext as Downloader;

                #region Setting the Prefix Options
                if (MenuItem_Prefix_DoNotUse.IsChecked)
                {
                    dc.PrefixFileName = false;  
                }
                else if (MenuItem_Prefix_Specify.IsChecked)
                {
                    CustomDialog customDialog = new CustomDialog("Enter the Prefix", "Type the Prefix Below", "Example:  'MyPrefix' ( ' - ' will be added)");
                    if (customDialog.ShowDialog() == true)
                    {
                        dc.FileNamePrefix = customDialog.Answer.Trim();
                    }
                }
                #endregion

                #region Getting the Date if Select Date 
                if (MenuItem_Download_SelectDate.IsChecked == true)
                {
                   DatePicker datePicker = new DatePicker("Enter the Date After Which to Download Files", "Click the Calander Button to Select a Date", "");
                    if (datePicker.ShowDialog() == true)
                    {
                        DateTime result;
                        if (DateTime.TryParse(datePicker.Answer, out result))
                        {
                            dc.DownloadSinceDate = result;
                        }
                    }
                }
                #endregion

                try
                {
                    #region Setting the Regex Options
                    if (RegexComboBox.Text.ToUpper() == "DEFAULTREGEX")
                    {
                        dc.RegularExpressionForUrls = Downloader.REGEX_DEFAULT_FILES;
                    }
                    else if (RegexComboBox.Text.ToUpper() == "OCREMIXREGEX")
                    {
                        dc.RegularExpressionForUrls = @"href=""(/remix/OCR\d+)""";
                    }
                    else
                    {
                        dc.RegularExpressionForUrls = RegexComboBox.Text;
                    }
                    #endregion
                    dc.StartDownload();
                }
                catch (Exception e2)
                {
                    MessageBox.Show(string.Format("Error:\n{0}", e2.Message));
                }
            }
            #endregion
            #region Cancel Button Command
            else if (Name == "Cancel")
            {
                CheckBox_CancelStatus.IsChecked = true;
            }
            #endregion
            #region ShowDownloadLog
            else if (Name == "ShowDownloadLog")
            {
                if (MenuItem_ShowDownloadLog.IsChecked)
                {
                    MainMenu.Width = WindowWidthExpanded;
                    this.WindowState = WindowState.Normal;
                    this.MaxWidth = WindowWidthExpanded;
                    this.Width = WindowWidthExpanded;
                }
                else
                {
                    MainMenu.Width = WindowWidthNormal;
                    this.WindowState = WindowState.Normal;
                    this.MaxWidth = WindowWidthNormal;
                }
            }
            #endregion
            #region ShowRenamingWindow
            else if (Name == "ShowRenamingWindow")
            {
                MenuItem_ShowRenamingWindow.IsChecked = false;
                RenameFilesWindow renameFilesWindow = new RenameFilesWindow();
               // this.WindowState = WindowState.Minimized;
                renameFilesWindow.Show();
                //this.WindowState = WindowState.Normal;
            }
            #endregion
            #region ShowStringArrayMakerWindow
            else if (Name == "ShowStringArrayMakerWindow")
            {
                MenuItem_ShowStringArrayMakerWindow.IsChecked = false;
                StringArrayMaker stringArrayMakerWindow = new StringArrayMaker();
               // this.WindowState = WindowState.Minimized;
                stringArrayMakerWindow.Show();
                //this.WindowState = WindowState.Normal;
            }
            #endregion
            #region Exit
            else if (Name == "Exit")
            {
                Application.Current.Shutdown();
            }
            #endregion
        }
        #endregion
        #region CommandBinding_CanExecute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string Name = ((RoutedCommand)e.Command).Name;
            #region Reset Button
            if (Name == "ResetBoxes")
            {
                //determining if the UIElement should be turned on or off (CanExecute)
                if (destinationtextbox.Text.Length > 0 || urltextbox.Text.Length > 0)      //this was for a textbox
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
            #endregion
            #region Download Button
            else if (Name == "Download")
            {
                try
                {
                    if (Downloader.GetIsValidUrl(urltextbox.Text) && (string.IsNullOrEmpty(destinationtextbox.Text) || destinationtextbox.Text == startingDestination || Directory.Exists(destinationtextbox.Text)))
                    {
                        e.CanExecute = true;
                    }
                    else
                    {
                        e.CanExecute = false;

                    }
                }
                catch (Exception)
                {
                    e.CanExecute = false;
                }
            }
            #endregion
            #region Cancel Button
            else if (Name == "Cancel")
            {
                if (DataContextSet)
                {
                    Downloader downloader = this.DataContext as Downloader;
                    if (string.IsNullOrEmpty(downloader.TransferProgress))
                    {
                        e.CanExecute = false;
                    }
                    else
                    {
                        e.CanExecute = true;
                    }
                }
            }
            #endregion
            #region ShowDownloadLog
            else if (Name == "ShowDownloadLog")
            {
                e.CanExecute = true;
            }
            #endregion
            #region ShowRenamingWindow
            else if (Name == "ShowRenamingWindow")
            {
                e.CanExecute = true;
            }
            #endregion
            #region ShowStringArrayMakerWindow
            else if (Name == "ShowStringArrayMakerWindow")
            {
                e.CanExecute = true;
            }
            #endregion
            else
            {
                e.CanExecute = true;
            }
        }
        #endregion
        #region Needed For Recursion Depth Text boxes (Only allows numeric input)
        private void MaskNumericInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }
        private void MaskNumericPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private bool TextIsNumeric(string input)
        {
            return input.All(c => Char.IsDigit(c) || Char.IsControl(c));
        }
        #endregion
        #region Event Handlers
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OCRemixRegex != null)
            {
                if (OCRemixRegex.IsSelected == true)
                {
                    urltextbox.Text = "https://ocremix.org";
                    MenuItem_Download_By_Date.IsChecked = true;
                }
                else
                {
                    MenuItem_Download_By_Date.IsChecked = false;
                }
            }
        }
        private void urltextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = ((TextBox)sender);
            textBox.Text = "";
        }
        #endregion
    }
}


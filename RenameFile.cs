using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downloader
{
    #region RenameFile
    ///<summary>
    /// renames all the filePathes in a DirectoryPath based on the IDv3 tag
    /// </summary>
    /// <param name="sourceFolder"></param>
    /// <param name="regex"></param>
    /// <param name="replaceWith"></param>
    /// 
    class RenameFile :  IDataErrorInfo, INotifyPropertyChanged
    {
        #region Members
        #region  Fields
        /// <summary>
        /// All of the Formats that TaglibSupports
        /// </summary>
        public static string[] TagLibSupportedFormats = { "mkv", "ogv", "avi", "wmv", "asf", "mp4", "m4p", "m4v", "mpeg", "mpg", "mpe", "mpv", "mpg", "m2v", "aa", "aax", "aac", "aiff", "ape", "dsf", "flac", "m4a", "m4b", "m4p", "mp3", "mpc", "mpp", "ogg", "oga", "wav", "wma", "wv", "webm", "bmp", "gif", "jpeg", "pbm", "pgm", "ppm", "pnm", "pcx", "png", "tiff", "dng", "svg", };
        /// <summary>
        /// Possible Image Formats for IDv3 Picture
        /// </summary>
        public static string[] ImageFormats = { "JPEG", "JFIF", "Exif", "TIFF", "GIF", "BMP", "PNG", "PPM", "PGM", "PBM", "PNM", "JPG"};
        /// <summary>
        /// An array of strings representing the new filesnames for the files to be renamed.
        /// </summary>
        internal string[] NewFileNames = null;
        /// <summary>
        /// List of illegal character to use in renaming>
        /// </summary>
       public static List<char> IllegalCharacters;
        
        /// <summary>
        /// These are strings that will be replaced with a space in the NewFileName.  Default is {"_",  "/","  ", "- -"}
        /// </summary>
        private string[] ILLEGAL_STRINGS = {"_",  "/","  ","- -"};
        
        /// <summary>
        ///whether to change name to all mp3 filePathes in subdirectories of path.  Default is false.
        /// </summary>
        public  bool Recursion = false;
       
        /// <summary>
        /// Whether or not to include the artists and producers when renaming the filePathes. Default is false.
        /// </summary>
        public bool IncludePerformers = false;
        /// <summary>
        /// Capitalizes the first letter of each word in the NewFileName if true.  Default is true.
        /// </summary>
        public bool ShouldCapitalizeFirstLetter = true;
        /// <summary>
        /// Replaces more than one contiguous space with one space in NewFileName.  Default is true.
        /// </summary>
        public bool RemoveMultipleContiguousSpaces= true;
        /// <summary>
        /// A list of all the changes made to filePathes during each session
        /// </summary>
        internal List<Tuple<string, string, string>> ChangesList = new List<Tuple<string, string, string>>();
        internal ObservableCollection<FilenameChange> PreviewChangesCollection = new ObservableCollection<FilenameChange>();
        internal string NewFileName;
        internal string OriginalFileName;
        internal string DirectoryPath;
        internal string FileExtension;
        internal string[] Performers = null;
        internal string Title = null;
        internal string FilePath;
        internal bool PreviewChanges = false;
        internal bool CanMakeChanges = false;
        internal int IDv3ChangesCount = 0;
        #endregion
        #region Bound Properties
        #region FileExtensions
        private string _FileExtensions = ".mp3";
        /// <summary>
        /// File name types to include in the renaming
        /// </summary>
        public string FileExtensions
        {
            get { return _FileExtensions; }
            set
            {
                if (_FileExtensions != value)
                {
                    _FileExtensions = value;
                    NotifyPropertyChanged("FileExtensions");
                }
            }
        }
        #endregion
        #region ToReplaceRegexPatterns
        private string[] _ToReplaceRegexPatterns = { @" " };
        /// <summary>
        /// the regex pattern to use to when naming.  Default is null (no regex replacing occurs).
        /// </summary>
        public string[] ToReplaceRegexPatterns
        {
            get { return _ToReplaceRegexPatterns; }
            set
            {
                if (_ToReplaceRegexPatterns != value)
                {
                    _ToReplaceRegexPatterns = value;
                    NotifyPropertyChanged("ToReplaceRegexPatterns");
                }
            }
        }
        #endregion
        #region ToReplaceWith
        private string[] _ToReplaceWith = { @" " };
        /// <summary>
        /// what to replace the regex pattern with.  Default is " ".
        /// </summary>
        public string[] ToReplaceWith
        {
            get { return _ToReplaceWith; }
            set
            {
                if (_ToReplaceWith != value)
                {
                    _ToReplaceWith = value;
                    NotifyPropertyChanged("ToReplaceWith");
                }
            }
        }
        #endregion
        #region CheckBox_IncludeSubdirectories_Flag
        private bool _CheckBox_IncludeSubdirectories_Flag;
        public bool CheckBox_IncludeSubdirectories_Flag
        {
            get { return _CheckBox_IncludeSubdirectories_Flag; }
            set
            {
                if (_CheckBox_IncludeSubdirectories_Flag != value)
                {
                    _CheckBox_IncludeSubdirectories_Flag = value;
                    NotifyPropertyChanged("CheckBox_IncludeSubdirectories_Flag");
                }
            }
        }
        #endregion
        #region CheckBox_IncludeAllFileTypes_Flag
        private bool _CheckBox_IncludeAllFileTypes_Flag = false;
        /// <summary>
        /// Whether to recursively rename filePathes or just in the given path
        /// </summary>
        public bool CheckBox_IncludeAllFileTypes_Flag
        {
            get { return _CheckBox_IncludeAllFileTypes_Flag; }
            set
            {
                if (_CheckBox_IncludeAllFileTypes_Flag != value)
                {
                    _CheckBox_IncludeAllFileTypes_Flag = value;
                    NotifyPropertyChanged("CheckBox_IncludeAllFileTypes_Flag");
                }
            }
        }
        #endregion
        #region CheckBox_FilenameRegex_Flag
        private bool _CheckBox_FilenameRegex_Flag = false;
        public bool CheckBox_Files_FilenameRegex_Flag
        {
            get { return _CheckBox_FilenameRegex_Flag; }
            set
            {
                if (_CheckBox_FilenameRegex_Flag != value)
                {
                    _CheckBox_FilenameRegex_Flag = value;
                    NotifyPropertyChanged("CheckBox_Files_FilenameRegex_Flag");
                }
            }
        }
        #endregion
        #region DirectoryToRenamePath
        private string _DirectoryToRenamePath;
        public string DirectoryToRenamePath
        {
            get { return _DirectoryToRenamePath; }
            set
            {
                if (_DirectoryToRenamePath != value)
                {
                    _DirectoryToRenamePath = value;
                    NotifyPropertyChanged("DirectoryToRenamePath");
                }
            }
        }
        #endregion
        #region Separator
        private string[] _Separator = new string[] { "-" };
        /// <summary>
        /// The character in the filename of each filePath that corresponds to the prefix separator. Default is '-'.
        /// </summary>
        public string[] Separator
        {
            get { return _Separator; }
            set
            {
                if (_Separator.GetValue(0) != value)
                {
                    _Separator.SetValue(string.Format("{0}", value), 0) ;
                    NotifyPropertyChanged("Separator");
                }
            }
        }
        #endregion
        #region MaxPrefixCount
        private int _MaxPrefixCount = 1;
        /// <summary>
        /// The number of times to grab the string before the Separator characters and use it in the filename.  Default is 1.  Must be 1 or larger.
        /// </summary>
        public int MaxPrefixCount
        {
            get { return _MaxPrefixCount; }
            set
            {
                if (_MaxPrefixCount != value)
                {
                    _MaxPrefixCount = value;
                    NotifyPropertyChanged("MaxPrefixCount");
                }
            }
        }
        #endregion
        #region RadioButton_Prefix_UseFilePrefix_Flag
        private bool _RadioButton_Prefix_UseFilePrefix_Flag;
        public bool RadioButton_Prefix_UseFilePrefix_Flag
        {
            get { return _RadioButton_Prefix_UseFilePrefix_Flag; }
            set
            {
                if (_RadioButton_Prefix_UseFilePrefix_Flag != value)
                {
                    _RadioButton_Prefix_UseFilePrefix_Flag = value;
                    NotifyPropertyChanged("RadioButton_Prefix_UseFilePrefix_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_Prefix_UseRegex_Flag
        private bool _RadioButton_Prefix_UseRegex_Flag;
        public bool RadioButton_Prefix_UseRegex_Flag
        {
            get { return _RadioButton_Prefix_UseRegex_Flag; }
            set
            {
                if (_RadioButton_Prefix_UseRegex_Flag != value)
                {
                    _RadioButton_Prefix_UseRegex_Flag = value;
                    NotifyPropertyChanged("RadioButton_Prefix_UseRegex_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_Prefix_UseParentFolder_Flag
        private bool _RadioButton_Prefix_UseParentFolder_Flag = true;
        public bool RadioButton_Prefix_UseParentFolder_Flag
        {
            get { return _RadioButton_Prefix_UseParentFolder_Flag; }
            set
            {
                if (_RadioButton_Prefix_UseParentFolder_Flag != value)
                {
                    _RadioButton_Prefix_UseParentFolder_Flag = value;
                    NotifyPropertyChanged("RadioButton_Prefix_UseParentFolder_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_Prefix_UseCustom_Flag
        private bool _RadioButton_Prefix_UseCustom_Flag;
        public bool RadioButton_Prefix_UseCustom_Flag
        {
            get { return _RadioButton_Prefix_UseCustom_Flag; }
            set
            {
                if (_RadioButton_Prefix_UseCustom_Flag != value)
                {
                    _RadioButton_Prefix_UseCustom_Flag = value;
                    NotifyPropertyChanged("RadioButton_Prefix_UseCustom_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_PrefixSource_Filename_Flag
        private bool _RadioButton_PrefixSource_Filename_Flag = true;
        public  bool RadioButton_PrefixSource_Filename_Flag
        {
            get { return _RadioButton_PrefixSource_Filename_Flag; }
            set
            {
                if (_RadioButton_PrefixSource_Filename_Flag != value)
                {
                    _RadioButton_PrefixSource_Filename_Flag = value;
                    NotifyPropertyChanged("RadioButton_PrefixSource_Filename_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_PrefixSource_Textbox_Flag
        private bool _RadioButton_PrefixSource_Textbox_Flag;
        public bool RadioButton_PrefixSource_Textbox_Flag
        {
            get { return _RadioButton_PrefixSource_Textbox_Flag; }
            set
            {
                if (_RadioButton_PrefixSource_Textbox_Flag != value)
                {
                    _RadioButton_PrefixSource_Textbox_Flag = value;
                    NotifyPropertyChanged("RadioButton_PrefixSource_Textbox_Flag");
                }
            }
        }
        #endregion
        #region TextBox_Prefix_Value
        private string _TextBox_Prefix_Value;
        public string TextBox_Prefix_Value
        {
            get { return _TextBox_Prefix_Value; }
            set
            {
                if (_TextBox_Prefix_Value != value)
                {
                    _TextBox_Prefix_Value = value;
                    NotifyPropertyChanged("TextBox_Prefix_Value");
                }
            }
        }
        #endregion
        #region TextBox_PrefixRegex_Value
        private string _TextBox_PrefixRegex_Value;
        public string TextBox_PrefixRegex_Value
        {
            get { return _TextBox_PrefixRegex_Value; }
            set
            {
                if (_TextBox_PrefixRegex_Value != value)
                {
                    _TextBox_PrefixRegex_Value = value;
                    NotifyPropertyChanged("TextBox_PrefixRegex_Value");
                }
            }
        }
        #endregion
        #region RadioButton_Suffix_UseCustomInput_Flag
        private bool _RadioButton_Suffix_UseCustomInput_Flag;
        public bool RadioButton_Suffix_UseCustomInput_Flag
        {
            get { return _RadioButton_Suffix_UseCustomInput_Flag; }
            set
            {
                if (_RadioButton_Suffix_UseCustomInput_Flag != value)
                {
                    _RadioButton_Suffix_UseCustomInput_Flag = value;
                    NotifyPropertyChanged("RadioButton_Suffix_UseCustomInput_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_Suffix_UseIDv3Title_Flag
        private bool _RadioButton_Suffix_UseIDv3Title_Flag = true;
        public bool RadioButton_Suffix_UseIDv3Title_Flag
        {
            get { return _RadioButton_Suffix_UseIDv3Title_Flag; }
            set
            {
                if (_RadioButton_Suffix_UseIDv3Title_Flag != value)
                {
                    _RadioButton_Suffix_UseIDv3Title_Flag = value;
                    NotifyPropertyChanged("RadioButton_Suffix_UseIDv3Title_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_Suffix_UseFilename_Flag
        private bool _RadioButton_Suffix_UseFilename_Flag;
        public bool RadioButton_Suffix_UseFilename_Flag
        {
            get { return _RadioButton_Suffix_UseFilename_Flag; }
            set
            {
                if (_RadioButton_Suffix_UseFilename_Flag != value)
                {
                    _RadioButton_Suffix_UseFilename_Flag = value;
                    NotifyPropertyChanged("RadioButton_Suffix_UseFilename_Flag");
                }
            }
        }
        #endregion
        #region CheckBox_WriteIDv3Tags_Flag
        private bool _CheckBox_WriteIDv3Tags_Flag;
        public bool CheckBox_WriteIDv3Tags_Flag
        {
            get { return _CheckBox_WriteIDv3Tags_Flag; }
            set
            {
                if (_CheckBox_WriteIDv3Tags_Flag != value)
                {
                    _CheckBox_WriteIDv3Tags_Flag = value;
                    NotifyPropertyChanged("CheckBox_WriteIDv3Tags_Flag");
                }
            }
        }
        #endregion
        #region CheckBox_ChangeFilename_Flag
        private bool _CheckBox_ChangeFilename_Flag = false;
        public bool CheckBox_ChangeFilename_Flag
        {
            get { return _CheckBox_ChangeFilename_Flag; }
            set
            {
                if (_CheckBox_ChangeFilename_Flag != value)
                {
                    _CheckBox_ChangeFilename_Flag = value;
                    NotifyPropertyChanged("CheckBox_ChangeFilename_Flag");
                }
            }
        }
        #endregion
        #region CheckBox_Files_IDv3ArtistRegex_Flag
        private bool _CheckBox_Files_IDv3ArtistRegex_Flag = false;
        public bool CheckBox_Files_IDv3ArtistRegex_Flag
        {
            get { return _CheckBox_Files_IDv3ArtistRegex_Flag; }
            set
            {
                if (_CheckBox_Files_IDv3ArtistRegex_Flag != value)
                {
                    _CheckBox_Files_IDv3ArtistRegex_Flag = value;
                    NotifyPropertyChanged("CheckBox_Files_IDv3ArtistRegex_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_TitleSource_Filename_Path_Flag
        private bool _RadioButton_TitleSource_Filename_Path_Flag = true;
        public bool RadioButton_TitleSource_Filename_Path_Flag
        {
            get { return _RadioButton_TitleSource_Filename_Path_Flag; }
            set
            {
                if (_RadioButton_TitleSource_Filename_Path_Flag != value)
                {
                    _RadioButton_TitleSource_Filename_Path_Flag = value;
                    NotifyPropertyChanged("RadioButton_TitleSource_Filename_Path_Flag");
                }
            }
        }
        #endregion
        #region RadioButton_TitleSource_Textbox_Path_Flag
        private bool _RadioButton_TitleSource_Textbox_Path_Flag;
        public bool RadioButton_TitleSource_Textbox_Path_Flag
        {
            get { return _RadioButton_TitleSource_Textbox_Path_Flag; }
            set
            {
                if (_RadioButton_TitleSource_Textbox_Path_Flag != value)
                {
                    _RadioButton_TitleSource_Textbox_Path_Flag = value;
                    NotifyPropertyChanged("RadioButton_TitleSource_Textbox_Path_Flag");
                }
            }
        }
        #endregion
        #region TextBox_FileNames_Text
        private string _TextBox_FileNames_Text;
        public string TextBox_FileNames_Text
        {
            get { return _TextBox_FileNames_Text; }
            set
            {
                if (_TextBox_FileNames_Text != value)
                {
                    _TextBox_FileNames_Text = value;
                    NotifyPropertyChanged("TextBox_FileNames_Text");
                }
            }
        }
        #endregion
        #region TextBox_IDv3TitleRegex_Text
        private string _TextBox_IDv3TitleRegex_Text;
        public string TextBox_IDv3TitleRegex_Text
        {
            get { return _TextBox_IDv3TitleRegex_Text; }
            set
            {
                if (_TextBox_IDv3TitleRegex_Text != value)
                {
                    _TextBox_IDv3TitleRegex_Text = value;
                    NotifyPropertyChanged("TextBox_IDv3TitleRegex_Text");
                }
            }
        }
        #endregion
        #region TextBox_IDv3Album_Text
        private string _TextBox_IDv3Album_Text;
        public string TextBox_IDv3Album_Text
        {
            get { return _TextBox_IDv3Album_Text; }
            set
            {
                if (_TextBox_IDv3Album_Text != value)
                {
                    _TextBox_IDv3Album_Text = value;
                    NotifyPropertyChanged("TextBox_IDv3Album_Text");
                }
            }
        }
        #endregion
        #region  TextBox_IDv3Artist_Text
        private string _TextBox_IDv3Artist_Text;
        public string  TextBox_IDv3Artist_Text
        {
            get { return _TextBox_IDv3Artist_Text; }
            set
            {
                if (_TextBox_IDv3Artist_Text != value)
                {
                    _TextBox_IDv3Artist_Text = value;
                    NotifyPropertyChanged(" TextBox_IDv3Artist_Text");
                }
            }
        }
        #endregion
        #region TextBox_Suffix_RegexToUse_Text
        private string _TextBox_Suffix_RegexToUse_Text;
        public string TextBox_Suffix_RegexToUse_Text
        {
            get { return _TextBox_Suffix_RegexToUse_Text; }
            set
            {
                if (_TextBox_Suffix_RegexToUse_Text != value)
                {
                    _TextBox_Suffix_RegexToUse_Text = value;
                    NotifyPropertyChanged("TextBox_Suffix_RegexToUse_Text");
                }
            }
        }
        #endregion
        #region TextBox_PathRegexText
        private string _TextBox_PathRegexText;
        public string TextBox_FilenameRegex_Text
        {
            get { return _TextBox_PathRegexText; }
            set
            {
                if (_TextBox_PathRegexText != value)
                {
                    _TextBox_PathRegexText = value;
                    NotifyPropertyChanged("TextBox_FilenameRegex_Text");
                }
            }
        }
        #endregion
        #region TextBox_Files_IDv3ArtistRegex_Text
        private string _TextBox_Files_IDv3ArtistRegex_Text;
        public string TextBox_Files_IDv3ArtistRegex_Text
        {
            get { return _TextBox_Files_IDv3ArtistRegex_Text; }
            set
            {
                if (_TextBox_Files_IDv3ArtistRegex_Text != value)
                {
                    _TextBox_Files_IDv3ArtistRegex_Text = value;
                    NotifyPropertyChanged("TextBox_Files_IDv3ArtistRegex_Text");
                }
            }
        }
        #endregion
        #region TextBox_Path_IDv3Image_Text
        private string _TextBox_Path_IDv3Image_Text;
        public string TextBox_Path_IDv3Image_Text
        {
            get { return _TextBox_Path_IDv3Image_Text; }
            set
            {
                if (_TextBox_Path_IDv3Image_Text != value)
                {
                    _TextBox_Path_IDv3Image_Text = value;
                    NotifyPropertyChanged("TextBox_Path_IDv3Image_Text");
                }
            }
        }
        #endregion
        #endregion
        #region WPF UI Stuff
        #region Needed to implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {

                //MessageBox.Show(string.Format("propertyName: {0}", propertyName));
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region Needed to implement IDataErrorInfo. 
        public string Error => null;    //returns null
        public string this[string propertyName]       //the name of the property for the current object
        {
            get
            {
                char[] illegalChars = Path.GetInvalidFileNameChars();
                bool ContainsIllegalChars = illegalChars.Any(c => Separator[0].Contains(c));

                //MessageBox.Show(string.Format("ContainsIllegalChars: {0}", ContainsIllegalChars));
                //MessageBox.Show(string.Format("propertyName: {0}", propertyName));
                string retvalue = null;
                if (propertyName == "Separator")
                {
                    if (string.IsNullOrEmpty(Separator[0]))  //define your own logic for data validation
                    {
                        retvalue = "Enter exactly one character to use as a separator between the prefix and the suffix";       //define an appropriate message
                    }
                    if (ContainsIllegalChars)
                    {
                        retvalue = $"{Separator[0]} is an illegal filePath character.";
                    }
                }
                else if (propertyName == "MaxPrefixCount")
                {
                    if (!Regex.Match(this.MaxPrefixCount.ToString(), @"\d+", RegexOptions.IgnoreCase).Success || this.MaxPrefixCount < 1)  //define your own logic for data validation
                    {
                        retvalue = "MaxPrefixCount must be an integer greater than 0";       //define an appropriate message
                    }
                }
                else if (propertyName == "FileExtensions")
                {
                    if (FileExtensions.Length > 0)  //define your own logic for data validation
                    {
                        retvalue = "";       //define an appropriate message
                    }
                }
                else if (propertyName == "ToReplaceWith")
                {
                    if (true)  //define your own logic for data validation
                    {
                        retvalue = "ToReplaceWith";       //define an appropriate message
                    }
                }
                else if (propertyName == "ToReplaceRegexPatterns")
                {
                    if (true)  //define your own logic for data validation
                    {
                        retvalue = "ToReplaceRegexPatterns";       //define an appropriate message
                    }
                }
                return retvalue;
            }
        }
        #endregion
        #endregion
        #region Constructor
        public RenameFile()
        {
             IllegalCharacters = Path.GetInvalidFileNameChars().ToList();
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
           Console.WriteLine("RenameFileFromDownloader crashed", "Unhandled Exception Occurred");
            TaskScheduler.UnobservedTaskException += (s, e) =>
            Console.WriteLine("RenameFileFromDownloader crashed", "Unhandled Exception Occurred");
        }
        #endregion
        #endregion
        #region Methods
        #region Renaming Methods
        #region RenameDirectory
        ///<summary>
        /// Renames the filePathes in the DirectoryToRenamePath based on IDv3 tags.  
        /// </summary>
        /// <param name="dirPath">Path to the DirectoryPath with the filePathes.  If calling from RenameFilesWindow, leave this at the default value.</param>
        internal void RenameDirectory(string dirPath = "", RenameFileModes mode = RenameFileModes.FromDownloader, bool displayResults = true)
        {
            #region Initialization Stuff
            CanMakeChanges = false;
            List<string> filePathes = new List<string>();
            if (dirPath == "")
            {
                dirPath = DirectoryToRenamePath;
            }
            #endregion
            GetFileNames(dirPath, ref filePathes);
            #region Getting Prefix and Calling Rename File Method on Each File
            string[] desiredTitlesFromTextBox = null;
            if (RadioButton_TitleSource_Textbox_Path_Flag)
            {
                desiredTitlesFromTextBox = TextBox_FileNames_Text.Split('\n');
            }
            foreach (var filePath in filePathes)
            {
                string prefix = null;
                #region Case: Renaming Automatically from Downloader Window
                if (mode == RenameFileModes.FromDownloader)
                {
                    try
                    {
                        prefix = GetPrefixFromFileName(filePath);
                        RenameFileFromDownloader(filePath, prefix);
                        ReplaceURLsWithNewFilenames(dirPath);
                    }
                    #region Handling Any Renaming Issues via Abort/Retry/Ignore
                    catch (Exception e)
                    {
                        DialogResult answer = MessageBox.Show(string.Format("Exception Encountered during file renaming: {0}.\nTrying again.", e.Message), string.Format("Error Renaming Files"), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
                        if (answer == DialogResult.Abort)
                        {
                            //Application.Exit();
                            System.Windows.Forms.Application.ExitThread();
                        }
                        else if (answer == DialogResult.Ignore)
                        {
                            continue;
                        }
                        else
                        {

                        }
                        #endregion
                    }
                }
                #endregion
                #region Case: Using the Separate Window to Rename
                else if (mode == RenameFileModes.FromOutsideFile || mode == RenameFileModes.FromIDv3Tags)
                {
                    RenameFileFromOutsideDownloader(filePath, desiredTitlesFromTextBox);
                }
                #endregion
            }
            #endregion
            #region Displaying Results if Desired
            if (displayResults)
            {
                if (!PreviewChanges)
                {
                    System.Windows.Forms.MessageBox.Show(string.Format($"Renaming of {dirPath} Complete!\n{ChangesList.Count} filePathes renamed."), string.Format("Finished Renaming"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ChangesList.Clear();
                }
                if (CheckBox_WriteIDv3Tags_Flag)
                {
                    System.Windows.Forms.MessageBox.Show(string.Format($"Writing of IDv3 tags Complete!\n{IDv3ChangesCount} IDv3 tags changed."), string.Format("Finished IDv3 Writing"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    IDv3ChangesCount = 0;
                }
            }
            #endregion
        }
        #endregion
        #region RenameFileFromOutsideDownloader
        internal void RenameFileFromOutsideDownloader(string filePath, string[] desiredTitlesFromTextBox)
        {
            string prefix = null;
            #region Changing the filename if specified
            if (CheckBox_ChangeFilename_Flag)
            {
                prefix = GetPrefixBasedOnUISettings(filePath, IDv3ChangesCount);
                filePath = RenameFileFromDownloader(filePath, prefix);
            }
            #endregion
            #region Writing to IDv3 Tags If Specified
            if (CheckBox_WriteIDv3Tags_Flag)
            {
                string titleSource = null;
                if (RadioButton_TitleSource_Filename_Path_Flag)
                {
                    titleSource = Path.GetFileNameWithoutExtension(filePath).Trim();
                }
                else if (RadioButton_TitleSource_Textbox_Path_Flag)
                {
                    titleSource = desiredTitlesFromTextBox[IDv3ChangesCount].Trim();
                }
                //todo: need to thoroughly test WriteIDv3Tag() by not inputting anything into the fields

                #region Creating an Artist Array Based on Presence of Comma
                string[] artists = null;
                if (!string.IsNullOrEmpty(TextBox_IDv3Artist_Text))
                {
                    if (TextBox_IDv3Artist_Text.Contains(','))
                    {
                        artists = TextBox_IDv3Artist_Text.Trim().Split(',');
                    }
                    else
                    {
                        artists = new string[1];
                        artists.SetValue(TextBox_IDv3Artist_Text, 0);
                    }
                }
                #endregion
                WriteIDv3Tag(filePath, titleSource, artists, TextBox_IDv3Album_Text, TextBox_IDv3TitleRegex_Text, TextBox_Path_IDv3Image_Text);
                IDv3ChangesCount++;
            }
            #endregion
        }
        #endregion
        #region RenameFileFromDownloader
        /// <summary>
        /// Renames the filePath based on IDv3 tags in the format of: 'prefix - Title'
        /// </summary>
        /// <param name="filePath">Path to the filePath being changed</param>
        /// <param name="prefix"> the prefix to add</param>
        /// <param name="usageMode"> Determines the behavior.  If set to RenameFile, it renames the filePath and returns "true" or "false" as a string if successful or 
        /// not, otherwise it returns the new NewFileName., which is the default.</param>
        /// <returns>the new filePath of the file</returns>
        internal string RenameFileFromDownloader(string filePath, string prefix = "")
        {
            #region Initialization            
            FilePath = filePath;
            NewFileName = Path.GetFileName(FilePath);
            OriginalFileName = NewFileName;
            this.DirectoryPath = System.IO.Directory.GetParent(FilePath).ToString();
            FileExtension = Path.GetExtension(FilePath);
            Performers = null;
            Title = null;
            string suffix;
            #endregion

            if (RadioButton_Suffix_UseIDv3Title_Flag)
            {
                #region Getting IDv3 Title and Artists if it is a supported format
                if (TagLibSupportedFormats.Any(f => FileExtension.Contains(f)))
                {
                    try
                    {
                        TagLib.File tagLibFile = TagLib.File.Create(FilePath);
                        Performers = tagLibFile.Tag.Performers;
                        Title = tagLibFile.Tag.Title;
                    }
                    catch { }
                }
                #endregion
                #region Getting fileName Based on IncludePerformers and prefix
                if (!string.IsNullOrEmpty(Title))
                {
                    if (Performers != null && IncludePerformers)
                    {
                        NewFileName = string.Format("{0} - {1} - {2}", prefix.Trim(), Performers[0].Trim(), Title.Trim());
                    }
                    else
                    {
                        //todo: only do this if the filename has multiple separator characters?
                        if (!string.IsNullOrEmpty(TextBox_Suffix_RegexToUse_Text))
                        {
                            Title = Regex.Match(Title, TextBox_Suffix_RegexToUse_Text, RegexOptions.IgnoreCase).Groups[1].Value;
                        }
                        NewFileName = GetNewFileName(prefix, Title);
                    }
                }
                else
                {
                    if (!NewFileName.Trim().ToUpper().StartsWith(prefix.ToUpper()))
                    {
                        NewFileName = string.Format("{0} - {1}", prefix.Trim(), NewFileName.Trim());
                    }
                }
                #endregion
            }
            else if (RadioButton_Suffix_UseFilename_Flag)
            {
                suffix = Path.GetFileName(filePath);
                if (!string.IsNullOrEmpty(TextBox_Suffix_RegexToUse_Text))
                {
                    suffix = Regex.Match(suffix, TextBox_Suffix_RegexToUse_Text, RegexOptions.IgnoreCase).Groups[1].Value;
                }
                NewFileName = string.Format("{0} {1} {2}", prefix, Separator[0], suffix); ;
            }
            return CleanUpAndRename();
        }
        #endregion
        #endregion
        #region Helper Methods
        #region ReplaceURLsWithNewFilenames
        private void ReplaceURLsWithNewFilenames(string dirPath)
        {
            #region Replacing Urls.txt FileNames with Renamed Ones
            string urlsFilePath = Path.Combine(dirPath, Downloader.UrlsTextFileName);
            if (System.IO.File.Exists(urlsFilePath) && ChangesList.Count != 0)
            {
                string[] oldUrlsFileContents = System.IO.File.ReadAllLines(urlsFilePath);
                StringBuilder changesMade = new StringBuilder();
                int lineCount = default(int);
                bool ShouldReWriteUrlsTextFile = false;
                foreach (string oldLine in oldUrlsFileContents)
                {
                    if (!string.IsNullOrEmpty(oldLine))
                    {
                        bool ShouldAddOldLine = true;
                        foreach (Tuple<string, string, string> line in ChangesList)
                        {
                            string originalFileName = line.Item2;
                            string newFileName = line.Item3;
                            if (oldLine.Contains(Downloader.WEBSITE_DEMARCATION_MARKER) && lineCount == default(int))
                            {
                                changesMade.AppendLine(oldLine);
                                lineCount++;
                                break;
                            }
                            else
                            {
                                string toCheckAgainst = Downloader.GetCompareAgainst(oldLine);
                                //Have to handle the case that filename has a comma in it
                                if (Regex.Match(originalFileName.Trim(), Regex.Escape(toCheckAgainst.Trim()), RegexOptions.IgnoreCase).Success)
                                {
                                    string[] lineSplit = oldLine.Split(',');
                                    changesMade.AppendLine(string.Format("{0}, {1}", newFileName.Trim(), lineSplit[1].Trim()));
                                    ShouldReWriteUrlsTextFile = true;
                                    ShouldAddOldLine = false;
                                    lineCount++;
                                    break;
                                }
                            }
                            lineCount++;
                        }
                        if (!oldLine.Contains(Downloader.WEBSITE_DEMARCATION_MARKER) && ShouldAddOldLine && lineCount != default(int) + 1)
                        {
                            changesMade.AppendLine(oldLine);
                        }
                    }
                }
                if (ShouldReWriteUrlsTextFile)
                {
                    //MessageBox.Show(string.Format("changesMade.ToString(): {0}\n\noriginal: {1}", changesMade.ToString(), oldUrlsFileContents));
                    System.IO.File.WriteAllText(urlsFilePath, changesMade.ToString().Trim());
                }
            }
            #endregion
        }
        #endregion
        #region CleanUpAndRename
        private string CleanUpAndRename()
        {
            #region Cleaning Up fileName
            #region Replacing Illegal Strings
            foreach (string str in ILLEGAL_STRINGS)
            {
                if (str != Separator[0] && NewFileName.Contains(str))
                {
                    NewFileName = NewFileName.Replace(str, " ");
                }
            }
            #endregion
            NewFileName = RemoveInvalidFileNameCharacters(NewFileName);
            #region Various Things
            if (ToReplaceRegexPatterns.Length > 0)
            {
                if (ToReplaceWith.Length == ToReplaceRegexPatterns.Length)
                {
                    for (int i = 0; i < ToReplaceRegexPatterns.Length; i++)
                    {
                        while (Regex.Match(NewFileName, ToReplaceRegexPatterns[i]).Success) { 
                            NewFileName = Regex.Replace(NewFileName, ToReplaceRegexPatterns[i], ToReplaceWith[i]);
                        }
                    }
                }
                else
                {
                    throw new Exception("ToReplaceWith and ToReplaceRegexPatterns must be the same length.");
                }
            }
            if (RemoveMultipleContiguousSpaces)
            {
                NewFileName = Regex.Replace(NewFileName, @"[\s]{2,}", " ");
            }
            if (ShouldCapitalizeFirstLetter)
            {
                NewFileName = CapitalizeFirstLetter(NewFileName, ' ');
            }
            NewFileName = Regex.Replace(NewFileName, @"- -", @"-");
            #endregion
            #endregion
            #region Getting fullFilePath
            string fullFilePath = NewFileName.Contains(FileExtension) ? Path.Combine(DirectoryPath, NewFileName) : Path.Combine(DirectoryPath, NewFileName + FileExtension);
            if (!fullFilePath.Contains(FileExtension))
            {
                fullFilePath += FileExtension;
            }
            //fullFilePath = NewFileName.Contains(FileExtension) ? Path.Combine(DirectoryPath, NewFileName) : Path.Combine(DirectoryPath, NewFileName + FileExtension);
            //MessageBox.Show(string.Format("OriginalFileName: {0}\n length: {1}\nnewFileName: {2}\n length: {3}", OriginalFileName, OriginalFileName.Length, NewFileName, NewFileName.Length));
            #endregion
            #region Renaming File
            if (!System.IO.File.Exists(fullFilePath) && (!string.IsNullOrEmpty(Title) || Performers != null || ToReplaceRegexPatterns.Length > 0))
            {
                CanMakeChanges = true;
                //MessageBox.Show(string.Format("Renaming {0} to NewFileName: {1}",oldFileName, fullFilePath));
                bool newFileNameHasExtension = FileExtensions.Any(f => NewFileName.Contains(f));
                NewFileName = newFileNameHasExtension ? NewFileName : NewFileName + FileExtension;
                Tuple<string, string, string> changesTuple = new Tuple<string, string, string>
                (
                    string.Format("Renaming '{0}' to '{1}'", OriginalFileName, NewFileName),
                    OriginalFileName,
                    NewFileName
                );
                if (OriginalFileName != NewFileName && PreviewChanges == false)
                {
                    ChangesList.Add(changesTuple);
                    System.IO.File.Move(FilePath, fullFilePath);
                    return fullFilePath;
                }
            }
            PreviewChangesCollection.Add(new FilenameChange() { CurrentFileName = OriginalFileName, DesiredFileName = NewFileName, FilePath = FilePath });
            return FilePath;
            #endregion
        }
        public static string RemoveInvalidFileNameCharacters(string fileName)
        {
            #region Replacing Invalid FileName Chars
            string invalid = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalid)
            {
                fileName = fileName.Replace(c, ' ');
            }
            return fileName;
            #endregion
        }
        #endregion
        #region GetNewFileName
        /// <summary>
        /// Determines whether the Title contains any new information (not already in the prefix) and returns a resultant string representing the combination
        /// </summary>
        /// <param name="prefix">The prefix of the filename.</param>
        /// <param name="title">the proposed Title of the filename</param>
        private string GetNewFileName(string prefix, string title)
        {
            string newFileName = prefix;
            string[] splitTitles = title.Split(Separator, StringSplitOptions.None);
            foreach (string splitTitle in splitTitles)
            {
                // MessageBox.Show(string.Format("splitTitle: {0}\n prefix: {1}", splitTitle, prefix));
                //Adding the splitTitle part if it isn't contained in prefix
                if (!prefix.Contains(splitTitle))
                {
                    //MessageBox.Show(string.Format("splitTitle: {0} is new info", splitTitle));
                    newFileName = string.Format("{0} - {1}", newFileName.Trim(), splitTitle.Trim());
                }
            }

            //MessageBox.Show(string.Format("newPrefix: {0}", NewFileName));
            return newFileName;
        }
        #endregion
        #region CapitalizeFirstLetter
        public static String CapitalizeFirstLetter(String words, Char splitter)
        {
            String[] split;
            split = words.Split(splitter);
            words = String.Empty;
            foreach (String part in split)
            {
                Char[] chars;
                chars = part.ToCharArray();
                if (chars.Length > 0)
                {
                    chars[0] = ((new String(chars[0], 1)).ToUpper().ToCharArray())[0];
                }
                words += new String(chars) + splitter;
            }
            words = words.Substring(0, words.Length - 1);
            return (words);
        }
        #endregion
        #region GetFilesRecursively
        /// <summary>
        /// Gets all the filePathes in a folder and all subfolders based on searchPattern. 
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="searchPattern">This is routed to DirectoryPath.GetFiles, so it must be in the following format: '*.EXTENSION'.  If none is specified, it defaults to '*.mp3'</param>
        /// <returns></returns>
        public List<string> GetFilesRecursively(string sourceDir, string searchPattern = "")
        {
            List<string> res = new List<string>();

            #region Setting a Default of *.mp3 for file type
            if (searchPattern == "")
            {
                searchPattern = "*" + FileExtensions[0];
            }
            #endregion

            try
            {
                #region Gets the files in all SubFolders of sourceDir
                foreach (string dir in System.IO.Directory.GetDirectories(sourceDir))
                {
                    foreach (string file in System.IO.Directory.GetFiles(dir, searchPattern))
                    {
                        res.Add(file);
                    }
                    this.GetFilesRecursively(dir);
                }
                #endregion

                #region Gets the files in sourceDir
                foreach (string file in System.IO.Directory.GetFiles(sourceDir, searchPattern))
                {
                    res.Add(file);
                }
                #endregion
            }
            catch (System.Exception excpt)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("Error Recursively Getting Files:\n{0}", excpt.Message));
            }
            return res;
        }
        #endregion
        #region GetFileNames
        /// <summary>
        /// Gets the files to rename based on options
        /// </summary>
        /// <param name="dirPath">the main directory</param>
        /// <param name="filePathes">the list of file pathes that will be assigned to</param>
        public void GetFileNames(string dirPath, ref List<string> filePathes)
        {
            #region Getting All File Types
            if (CheckBox_IncludeAllFileTypes_Flag)
            {
                if (CheckBox_IncludeSubdirectories_Flag)
                {
                    filePathes = GetFilesRecursively(dirPath, "*");
                }
                else
                {
                    filePathes = Directory.GetFiles(dirPath, "*").ToList();
                }
            }
            #endregion
            #region Getting Only Specified File Types
            else
            {
                string[] FileExtensionSeparator = { "," };
                string[] splitExtensions = FileExtensions.Split(FileExtensionSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string fileExtension in splitExtensions)
                {
                    string searchPattern = fileExtension.Contains('.') ? "*" + fileExtension.Trim() : "*." + fileExtension.Trim();
                    if (CheckBox_IncludeSubdirectories_Flag)
                    {
                        filePathes.AddRange(GetFilesRecursively(dirPath, searchPattern));
                    }
                    else
                    {
                        filePathes.AddRange(Directory.GetFiles(dirPath, searchPattern).ToList());
                    }
                }
            }
            #endregion
            #region If Filtering File by Filename Regex
            if (CheckBox_Files_FilenameRegex_Flag)
            {
                List<string> matchedFilePathes = new List<string>();
                foreach (string  path in filePathes)
                {
                    string filename = Path.GetFileNameWithoutExtension(path);
                    if (Regex.Match(filename, TextBox_FilenameRegex_Text, RegexOptions.IgnoreCase).Success)
                    {
                        matchedFilePathes.Add(path);
                    }
                }
                filePathes = matchedFilePathes;
            }
            #endregion  
            #region If Filtering Files by IDv3 Artist Regex
            if (CheckBox_Files_IDv3ArtistRegex_Flag)
            {
                List<string> matchedFilePathes = new List<string>();
                foreach (string path in filePathes)
                {
                    TagLib.File tagLibFile = TagLib.File.Create(path);

                    if (Regex.Match(tagLibFile.Tag.Title, TextBox_Files_IDv3ArtistRegex_Text, RegexOptions.IgnoreCase).Success)
                    {
                        matchedFilePathes.Add(path);
                    }
                }
                filePathes = matchedFilePathes;
            }

            #endregion
        }
        #endregion
        #region GetPrefixBasedOnUISettings
        /// <summary>
        /// Gets the prefix based on UI options
        /// </summary>
        /// <param name="filePath">path to file</param>
        /// <returns></returns>
        public string GetPrefixBasedOnUISettings(string filePath, int counter)
        {
            string prefix = null;
            try
            {
                if (RadioButton_Prefix_UseCustom_Flag)
                {
                    prefix = TextBox_Prefix_Value;
                }
                else if (RadioButton_Prefix_UseFilePrefix_Flag)
                {
                    prefix = GetPrefixFromFileName(filePath);
                }
                 else if (RadioButton_Prefix_UseParentFolder_Flag)
                {
                    prefix = Directory.GetParent(filePath).Name;
                }
                else if (RadioButton_Prefix_UseRegex_Flag)
                {
                    string source = null;
                    if (RadioButton_PrefixSource_Filename_Flag)
                    {
                        source = Path.GetFileName(filePath);
                    }
                    else if (RadioButton_PrefixSource_Textbox_Flag)
                    {
                        source= TextBox_FileNames_Text.Split('\r')[counter];
                    }
                    prefix = Regex.Match(source, TextBox_PrefixRegex_Value, RegexOptions.IgnoreCase).Groups[1].Value;
                }
                return prefix;
            }
            catch (Exception e)
            {
                throw new Exception($"Error: {e.Message}");
            }
        }
        #endregion
        #region GetPrefixFromFileName
        /// <summary>
        /// Gets the prefix of NewFileName based on the Separator and MenuItem_UseParentNameAsPrefix_Flag values.
        /// </summary>
        /// <param name="fileNamePath">PathFile name to get the prefix for</param>
        /// <returns></returns>
        internal string GetPrefixFromFileName(string fileNamePath)
        {
            #region Initialization
            string prefix = null;
            int prefixCount = 1;
            if (MaxPrefixCount < 1)
            {
                throw new Exception("MaxPrefixCount is below 1.  It must be 1 or higher.");
            }
            #endregion

            #region Getting the current prefix if it exists
            if (Separator.Any(sep => fileNamePath.Contains(sep.ToString())))
            {
                string fileName = Path.GetFileNameWithoutExtension(fileNamePath);
                List<string> prefixes = fileName.Split(Separator, StringSplitOptions.RemoveEmptyEntries).ToList();
                for (int i = 0; i < prefixes.Count - 1; i++)
                {
                    if (prefixCount <= MaxPrefixCount)
                    {
                        string nextPrefix = prefixes[i];
                        if (i == 0)
                        {
                            prefix = Path.GetFileName(nextPrefix).Trim();
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(nextPrefix))
                            {
                                prefix = string.Format("{0} {1} {2}", prefix, Separator[0], nextPrefix).Trim();
                            }
                        }
                        prefixCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            #endregion        

            return prefix;
        }
        #endregion
        #region WriteIDv3Tag
        /// <summary>
        /// Writes an IDv3 tag with the supplied info the the newDestination file
        /// </summary>
        /// <param name="newDestination">the path to the file</param>
        /// <param name="titleBeforeRegexMatch">the title before gett</param>
        /// <param name="artist">the artist to write to idv3</param>
        /// <param name="album">the album</param>
        /// <param name="titleRegex">the regex used to get the title from titleBeforeRegexMatch.  If left to default, titleBeforeRegexMatch is used as title.  Otherwise the first group found is used as title.</param>
        public static void WriteIDv3Tag(string newDestination, string titleBeforeRegexMatch, string[] artists, string album = null, string titleRegex = null, string imagePath = null)
        {
            string extension = Path.GetExtension(newDestination);
            if (TagLibSupportedFormats.Any(f => extension.Contains(f)))
            {
                try
                {
                    TagLib.File file = TagLib.File.Create(newDestination);
                    if (!string.IsNullOrEmpty(album))
                    {
                        file.Tag.Album = album.Trim();
                    }
                    if (artists != null)
                    {
                        if (artists.Length > 0)
                        {
                            file.Tag.Performers = artists;
                        }
                    }
                    if (!string.IsNullOrEmpty(titleRegex))
                    {
                        file.Tag.Title = Regex.Match(titleBeforeRegexMatch, titleRegex, RegexOptions.IgnoreCase).Groups[1].Value;
                    }
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        TagLib.IPicture newArt = new TagLib.Picture(imagePath);
                        file.Tag.Pictures = new TagLib.IPicture[1] { newArt };
                    }
                    file.Save();
                    file.Dispose();
                }
                catch (Exception e4)
                {
                    throw new Exception($"{e4 }\n went wrong writing IDv3 tags.  Try again.");
                }
            }
            else
            {
                string errorFilename = Path.GetFileName(newDestination);
                System.Windows.Forms.MessageBox.Show($"'{extension}' is not a supported format.  Skipping '{errorFilename}'.", $"{errorFilename} does not Support IDv3", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
        #endregion
        #endregion
        #endregion
        #region Enums
        public enum RenameFileModes
        {
            FromDownloader,
            FromIDv3Tags,
            FromOutsideFile,
        }
        public enum UsageModes
        {
            ReturnString,
            RenameFile
        }
        #endregion
    }
    #endregion
    #region FilenameChange
    public class FilenameChange
    {
        public string CurrentFileName { get; set; }
        public string DesiredFileName { get; set; }
        public string FilePath { get; set; }
        //public string IDv3Artist { get; set; }
        //public string IDv3Title { get; set; }
        //public string IDv3Album { get; set; }
    }
    #endregion
}

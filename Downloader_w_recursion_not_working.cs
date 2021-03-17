using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using System.Collections.Concurrent;
using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;
using System.Text;
using System.Security.Policy;

namespace Downloader 
{
    #region Downloader
    public class Downloader : IDataErrorInfo, INotifyPropertyChanged
    {
        #region Constants, Fields, and Properties
        #region Private Constants
        private const int DOWNLOAD_SOURCE_CODE_INTERVAL = 400;
        private const int DOWNLOAD_WAIT_INTERVAL = 500;
        public const string REGEX_DEFAULT_FILES = @"href=""(.+\{0})""";
        private static string REGEX_DEFAULT_URLS = "";
        private const string EXTENSION_DEFAULT = ".mp3";
        private const string FILENAME_PREFIX_DEFAULT = "";
        private const string DESTINATION_DEFAULT = "";
        private const string FILENAME_PREFIX_SEPARATOR_DEFAULT = "";
        private readonly double BYTES_TO_MB_FACTOR = Math.Pow(2, 20);
        internal static readonly string WEBSITE_DEMARCATION_MARKER = "website,";
        #endregion
        #region Private Fields
        //todo: keep track of files with empty title tags and auto set title to filename suffix?
        private string OriginalDestination { get; set; } = "";
        private string OriginalUrl { get; set; } = "";
        /// <summary>
        /// Caching requests made when downloading
        /// </summary>
        private HashSet<string> CachedUrls = new HashSet<string>();
        /// <summary>
        /// hashset of urls with possible files to download based on extension and recursion properties
        /// </summary>
        private readonly HashSet<UrlObject> UrlsToDownload = new HashSet<UrlObject>();
        /// <summary>
        /// hashset of tuples representing a url and whether it is a file or not
        /// </summary>
        private readonly HashSet<UrlObject> UrlsToRecurse = new HashSet<UrlObject>();
        private readonly HashSet<string> UniqueUrls = new HashSet<string>();
        private ConcurrentQueue<string> RecurseUrlsQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> DownloadQueue = new ConcurrentQueue<string>();
        /// <summary>
        /// The regex used to find pages that have files to download (different sites use different naming schemas).  Use a literal string e.g. @" "
        /// </summary>
        public static readonly Stopwatch Stopwatch = new Stopwatch();
        private bool IsFirstTimeDownloading = true;
        private string FileExtension;
        private string DirectoryPath;
        private string DatePath;
        internal bool PrefixFileName = true;
        internal DateTime DownloadSinceDate = DateTime.Parse("2001/01/01");
        /// <summary>
        /// Keeps track of the expected size of the downloaded files
        /// </summary>
        private readonly HashSet<long> FileSizes = new HashSet<long>();
        /// <summary>
        /// counting how many files have completed downloading
        /// </summary>
        private int FilesDownloaded = 0;
        /// <summary>
        /// counting how many files have started downloading
        /// </summary>
        private int FilesStarted=0;
        private readonly RenameFile RenameFile = new RenameFile()
        {
            ToReplaceRegexPatterns = new string[] { @"\s20([a-zA-Z]+)\s*", @"s\s+27\s+", @"\s27s\s+" },
            //ToReplaceRegexPatterns = new string[] { @"\w[^\-]*\s+20\s*[^\-]*", @"\w[^\-]*\s+27\s*[^\-]*" },
            ToReplaceWith = new string[] { @" $1 ",@"s' ", @"'s " },
            Recursion = true,
        };
        private bool FinishedGettingSourceCode = false;
        private int TransferProgressLineCount = 1;
        /// <summary>
        /// Marks a completion of the dowloading batch
        /// </summary>
        internal bool TransferComplete = false;
        /// <summary>
        /// A list of string representing a line in the urls.txt file
        /// </summary>
        internal List<string> UrlsLogText = new List<string>();
        /// <summary>
        /// urls.txt file path
        /// </summary>
        private string UrlsTextFilePath;
        /// <summary>
        /// whether urls.txt exists in DirectoryPath
        /// </summary>
        private bool UrlsTextExists = false;
        //todo: add option to copy files to and from mtp device and/or to computer?
        //todo: delete if can't get max recursion figured out
        
        //[ThreadStatic]private static int CurrentRecursionDepth = -1;
        private ThreadLocal<int> CurrentRecursionDepth = new ThreadLocal<int>(() => { return -1; });
        #endregion
        #region Public Fields
        /// <summary>
        ///sets the regex pattern used to find files on each of the webpage URLs.  This must be a c# regular expression where the first group matches what you are looking for example @"href=""(/OCR\d{4})""".    It is advised to leave this alone as it must contain {0} for string formatting internally
        /// </summary>
        private readonly string RegularExpressionForFiles = REGEX_DEFAULT_FILES;
        /// <summary>
        ///The amount of time between downloads.  Default is 500ms.  Used to prevent the server from sending a 404 code back.  try larger values if you are getting server response codes and lower values if you aren't
        /// </summary>
        public int DurationBetweenDownloads = DOWNLOAD_WAIT_INTERVAL;
        /// <summary>
        ///The amount of time between downloads of webpage source code.  Default is 400 ms.  Used to prevent the server from sending a 404 code back.  Try larger values if you are getting server response codes and lower values if you aren't
        /// </summary>
        public int DurationBetweenSourceCodeDownloads = DOWNLOAD_SOURCE_CODE_INTERVAL;
        /// <summary>
        /// The prefix that will be applied to all new downloaded files in the current batch
        /// </summary>
        public string FileNamePrefix = FILENAME_PREFIX_DEFAULT;
        public HashSet<string> CurrentExtensions = new HashSet<string>();
        public static readonly string UrlRegex= @"^\s*https?://.+\{0}/*(.+)*$";
        /// <summary>
        /// Whether or not to go to each link that matches the RegularExpression and download files from there.  Default is true.
        /// </summary>
        public bool Recursion = true;
        #region Domain and File Extensions
        public static string[] FileExtensions = { ".mp3", ".flac", ".mp4", ".wav", ".cda", ".mid", ".midi", ".mpa", ".wpl", ".ogg", ".aif" };
        public static string[] TopLevelDomains = { ".com", ".org", ".net", ".edu", ".gov", ".mil", ".arpa", ".ac", ".ae", ".af", ".ag", ".ai", ".al", ".am", ".ao", ".aq", ".ar", ".at", ".au", ".aw", ".ax", ".az", ".ba", ".bb", ".bd", ".be", ".bf", ".bg", ".bh", ".bi", ".bj", ".bm", ".bn", ".bo", ".br", ".bs", ".bt", ".bw", ".by", ".bz", ".ca", ".cc", ".cd", ".cf", ".cg", ".ch", ".ci", ".ck", ".cl", ".cm", ".cn", ".co", ".cr", ".cu", ".cv", ".cw", ".cx", ".cy", ".cz", ".de", ".dj", ".dk", ".dm", ".dz", ".ec", ".ee", ".eg", ".er", ".es", ".et", ".eu", ".fi", ".fj", ".fk", ".fm", ".fo", ".fr", ".ga", ".gd", ".ge", ".gf", ".gg", ".gh", ".gi", ".gl", ".gm", ".gn", ".gp", ".gq", ".gr", ".gs", ".gt", ".gu", ".gw", ".gy", ".hk", ".hm", ".hn", ".hr", ".ht", ".hu", ".id", ".ie", ".il", ".im", ".io", ".iq", ".ir", ".it", ".je", ".jm", ".jo", ".jp", ".ke", ".kg", ".kh", ".ki", ".km", ".kn", ".kp", ".kr", ".kw", ".ky", ".kz", ".la", ".lb", ".lc", ".li", ".lk", ".lr", ".ls", ".lt", ".lu", ".lv", ".ly", ".ma", ".mc", ".md", ".me", ".mg", ".mh", ".mk", ".ml", ".mm", ".mn", ".mo", ".mp", ".mq", ".mr", ".ms", ".mt", ".mu", ".mv", ".mw", ".mx", ".my", ".mz", ".na", ".nc", ".ne", ".nf", ".ng", ".ni", ".nl", ".no", ".nr", ".nu", ".nz", ".om", ".pa", ".pe", ".pf", ".pg", ".ph", ".pk", ".pl", ".pm", ".pn", ".pr", ".ps", ".pt", ".pw", ".py", ".qa", ".re", ".ro", ".rs", ".ru", ".rw", ".sa", ".sb", ".sc", ".sd", ".se", ".sg", ".sh", ".si", ".sk", ".sl", ".sm", ".sn", ".so", ".sr", ".ss", ".st", ".su", ".sv", ".sx", ".sy", ".sz", ".tc", ".td", ".tf", ".tg", ".th", ".tj", ".tk", ".tl", ".tm", ".tn", ".to", ".tr", ".tt", ".tv", ".tw", ".tz", ".ua", ".ug", ".uk", ".us", ".uy", ".uz", ".va", ".vc", ".ve", ".vg", ".vi", ".vn", ".vu", ".wf", ".ws", ".ye", ".yt", ".za", ".zm", ".zw" };
        #endregion
        /// <summary>
        /// The file name of the urls text file
        /// </summary>
        public static readonly string UrlsTextFileName = "urls.txt";
        #endregion
        #region Public(Bound) Properties

        #region DisplayResults

        #region DisplayResults
        private bool _DisplayResults;
        public bool DisplayResults
        {
            get { return _DisplayResults; }
            set
            {
                if (_DisplayResults != value)
                {
                    _DisplayResults = value;
                    NotifyPropertyChanged("DisplayResults");
                }
            }
        }
        #endregion
        #endregion
        #region IsFile
        private bool _IsFile = false;
        /// <summary>
        /// Specifies whether a url is a file.  Default is false.
        /// </summary>
        public bool IsFile
        {
            get { return _IsFile; }
            set
            {
                if (_IsFile != value)
                {
                    _IsFile = value;
                    NotifyPropertyChanged("IsFile");
                }
            }
        }
        #endregion
        #region RegularExpressionForUrls
        private string _RegularExpressionForUrls = REGEX_DEFAULT_URLS;
        /// <summary>
        ///
        ///Sets the regex pattern used to find more Urls to pages that may contain files.  This must be a c# regular expression where the first group matches what you are looking for example @"href=""(/OCR\d{4})""".    Study the html of the website to formulate the regex that will match the pages you are looking for.  The default is "", which means that the algorithm will only look for direct file URls for the URL given;
        /// </summary>
        public string RegularExpressionForUrls
        {
            get { return _RegularExpressionForUrls; }
            set
            {
                if (_RegularExpressionForUrls != value)
                {
                    _RegularExpressionForUrls = value;
                    NotifyPropertyChanged("RegularExpressionForUrls");
                }
            }
        }
        #endregion
        #region FileName
        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    NotifyPropertyChanged("FileName");
                }
            }
        }
        #endregion
        #region Url
        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                if (_url != value)
                {
                    _url = value;
                    NotifyPropertyChanged("Url");
                }
            }
        }
        #endregion
        #region DestinationOfFile
        private string _destinationOfFile;
        public string DestinationOfFile
        {
            get { return _destinationOfFile; }
            set
            {
                if (_destinationOfFile != value)
                {
                    _destinationOfFile = value;
                    NotifyPropertyChanged("DestinationOfFile");
                }
            }
        }
        #endregion
        #region ProgressValue
        private int _progressValue;
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    NotifyPropertyChanged("ProgressValue");
                }
            }
        }
        #endregion
        #region TotalDownloadReceived
        private string _totalDownloadReceived;
        public string TotalDownloadReceived
        {
            get { return _totalDownloadReceived; }
            set
            {
                if (_totalDownloadReceived != value)
                {
                    _totalDownloadReceived = value;
                    NotifyPropertyChanged("TotalDownloadReceived");
                }
            }
        }
        #endregion
        #region TotalDownloadSize
        private string _totalDownloadSize;
        public string TotalDownloadSize
        {
            get { return _totalDownloadSize; }
            set
            {
                if (_totalDownloadSize != value)
                {
                    _totalDownloadSize = value;
                    NotifyPropertyChanged("TotalDownloadSize");
                }
            }
        }
        #endregion
        #region DownloadSpeed
        //private string _downloadSpeed;
        //public string DownloadSpeed
        //{
        //    get { return _downloadSpeed; }
        //    set
        //    {
        //        if (_downloadSpeed != value)
        //        {
        //            _downloadSpeed = value;
        //            NotifyPropertyChanged("DownloadSpeed");
        //        }
        //    }
        //}
        #endregion
        #region DownloadAsync
        private bool _DownloadAsync = true;
        /// <summary>
        /// This determines whether multiple files are downloaded at one.  Default is true, but set to false if getting a lot of download errors as this will slow down download rate and probably prevent the server from sending errors.
        /// </summary>
        public bool DownloadAsync
        {
            get { return _DownloadAsync; }
            set
            {
                if (_DownloadAsync != value)
                {
                    _DownloadAsync = value;
                    NotifyPropertyChanged("DownloadAsync");
                }
            }
        }
        #endregion
        #region MenuItem_KeepTrackOfFilesDownloaded_Flag
        private bool _MenuItem_KeepTrackOfFilesDownloaded_Flag = true;

        /// <summary>
        /// Flag used for setting to write urls and filenames to urls.txt and only download new files.  Default is true.
        /// </summary>
        public bool MenuItem_KeepTrackOfFilesDownloaded_Flag
        {
            get { return _MenuItem_KeepTrackOfFilesDownloaded_Flag; }
            set
            {
                if (_MenuItem_KeepTrackOfFilesDownloaded_Flag != value)
                {
                    _MenuItem_KeepTrackOfFilesDownloaded_Flag = value;
                    NotifyPropertyChanged("MenuItem_KeepTrackOfFilesDownloaded_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_Download_SelectDate_Flag
        private bool _MenuItem_Download_SelectDate_Flag;
        public bool MenuItem_Download_SelectDate_Flag
        {
            get { return _MenuItem_Download_SelectDate_Flag; }
            set
            {
                if (_MenuItem_Download_SelectDate_Flag != value)
                {
                    _MenuItem_Download_SelectDate_Flag = value;
                    NotifyPropertyChanged("MenuItem_Download_SelectDate_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_IsYouTubeUrl_Flag
        private bool _MenuItem_IsYouTubeUrl_Flag = false;
        /// <summary>
        /// Whether the URL is to a youtube video
        /// </summary>
        public bool MenuItem_IsYouTubeUrl_Flag
        {
            get { return _MenuItem_IsYouTubeUrl_Flag; }
            set
            {
                if (_MenuItem_IsYouTubeUrl_Flag != value)
                {
                    _MenuItem_IsYouTubeUrl_Flag = value;
                    NotifyPropertyChanged("MenuItem_IsYouTubeUrl_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_Regex_Custom_Flag
        private bool _MenuItem_Regex_Custom_Flag;
        public bool MenuItem_Regex_Custom_Flag
        {
            get { return _MenuItem_Regex_Custom_Flag; }
            set
            {
                if (_MenuItem_Regex_Custom_Flag != value)
                {
                    _MenuItem_Regex_Custom_Flag = value;
                    NotifyPropertyChanged("MenuItem_Regex_Custom_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_Download_By_Date_Flag
        private bool _MenuItem_Download_By_Date_Flag = false;
        public bool MenuItem_Download_By_Date_Flag
        {
            get { return _MenuItem_Download_By_Date_Flag; }
            set
            {
                if (_MenuItem_Download_By_Date_Flag != value)
                {
                    _MenuItem_Download_By_Date_Flag = value;
                    NotifyPropertyChanged("MenuItem_Download_By_Date_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_RenameFiles_Flag
        private bool _MenuItem_RenameFiles_Flag = true;
        public bool MenuItem_RenameFiles_Flag
        {
            get { return _MenuItem_RenameFiles_Flag; }
            set
            {
                if (_MenuItem_RenameFiles_Flag != value)
                {
                    _MenuItem_RenameFiles_Flag = value;
                    NotifyPropertyChanged("MenuItem_RenameFiles_Flag");
                }
            }
        }
        #endregion
        #region MenuItem_CreateFolder_Flag
        private bool _MenuItem_CreateFolder_Flag = true;
        public bool MenuItem_CreateFolder_Flag
        {
            get { return _MenuItem_CreateFolder_Flag; }
            set
            {
                if (_MenuItem_CreateFolder_Flag != value)
                {
                    _MenuItem_CreateFolder_Flag = value;
                    NotifyPropertyChanged("MenuItem_CreateFolder_Flag");
                }
            }
        }
        #endregion
        #region OnlyDownloadSinceLastDownloadDate
        private bool _OnlyDownloadSinceLastDownloadDate = false;
        /// <summary>
        /// If true, will only download files based on the last time files were downloaded from the site.  This info is stored in a file in the download folder/>
        /// </summary>
        public bool OnlyDownloadSinceLastDownloadDate
        {
            get { return _OnlyDownloadSinceLastDownloadDate; }
            set
            {
                if (_OnlyDownloadSinceLastDownloadDate != value)
                {
                    _OnlyDownloadSinceLastDownloadDate = value;
                    NotifyPropertyChanged("OnlyDownloadSinceLastDownloadDate");
                }
            }
        }
        #endregion
        #region DateRegex
        private string _DateRegex = @"href=""/remix/OCR\d+"".+(\d{4}-\d{2}-\d{2})";
        public string DateRegex
        {
            get { return _DateRegex; }
            set
            {
                if (_DateRegex != value)
                {
                    _DateRegex = value;
                    NotifyPropertyChanged("DateRegex");
                }
            }
        }
        #endregion
        #region MaxRecursionDepth
        private int _MaxRecursionDepth = 10000000;
        /// <summary>
        /// The maximum number of pages that can be traversed to find content to download.  Default is unlimited.
        /// </summary>
        public int MaxRecursionDepth
        {
            get { return _MaxRecursionDepth; }
            set
            {
                if (_MaxRecursionDepth != value)
                {
                    _MaxRecursionDepth = value;
                    NotifyPropertyChanged("MaxRecursionDepth");
                }
            }
        }
        #endregion
        #region MinRecursionDepth
        private int _MinRecursionDepth = 0;
        /// <summary>
        ///The minimum number of pages that can be traversed in order to begin downloading files.  Default is 0 (being the page of the URL provided)
        /// </summary>
        public int MinRecursionDepth
        {
            get { return _MinRecursionDepth; }
            set
            {
                if (_MinRecursionDepth != value)
                {
                    _MinRecursionDepth = value;
                    NotifyPropertyChanged("MinRecursionDepth");
                }
            }
        }
        #endregion
        #region TransferProgress
        private string _TransferProgress = "";
        /// <summary>
        /// Used for updated UI with transfer progress
        /// </summary>
        public string TransferProgress
        {
            get { return _TransferProgress; }
            set
            {
                if (_TransferProgress != value)
                {
                    _TransferProgress = value;
                    NotifyPropertyChanged("TransferProgress");
                }
            }
        }
        #endregion
        #region ShouldCancel
        private bool _ShouldCancel = false;
        private bool TransferStarted = false;
        private bool InCachedUrlRecursion = false;

        /// <summary>
        /// Whether or not to continue downloading
        /// </summary>
        public bool ShouldCancel
        {
            get { return _ShouldCancel; }
            set
            {
                if (_ShouldCancel != value)
                {
                    _ShouldCancel = value;
                    NotifyPropertyChanged("ShouldCancel");
                }
            }
        }
        #endregion
        #endregion
        #region WPF Implementions
        #region Needed to implement IDataErrorInfo
        public string Error => null;    //returns null
        public string this[string propertyName]       //the name of the property for the current object
        {
            get
            {
                string retvalue = null;

                //MessageBox.Show(string.Format("propertyName: {0}", propertyName));
                if (propertyName == "Url")
                {
                    // Getting Matches
                    bool isValidUrl = GetIsValidUrl(Url);

                    if (String.IsNullOrEmpty(this.Url))  //define your own logic for data validation
                    {
                        retvalue = string.Format("Please enter a valid, complete URL.");
                    }
                    else if (!isValidUrl)
                    {
                        retvalue = string.Format("Please enter a valid, complete URL.\n'{0}' is either not valid or doesn't contain \n'http:' and/or a top level domain (.com, .edu, etc.)", this.Url);    //define an appropriate messag
                    }
                }
                if (propertyName == "DestinationOfFile")
                {
                    bool pathExists = Directory.Exists(this.DestinationOfFile);

                    if (String.IsNullOrEmpty(this.DestinationOfFile))        //define your own logic for data validation
                    {
                        retvalue = string.Format("Please enter a valid DirectoryPath to which to save the file(s).");    //define an appropriate messag
                    }
                    else if (pathExists == false)
                    {
                        retvalue = string.Format("Please enter a valid DirectoryPath.\n'{0}' doesn't exist", this.DestinationOfFile);    //define an appropriate messag
                    }
                }
                if (propertyName == "MinRecursionDepth")
                {
                    if (!Regex.Match(this.MinRecursionDepth.ToString(), @"\d+").Success || this.MinRecursionDepth < 0)
                    {
                        retvalue = string.Format("The miminum recursion value must be an integer greater than 0");
                    }
                }
                if (propertyName == "MaxRecursionDepth")
                {
                    bool success = !Regex.Match(this.MaxRecursionDepth.ToString(), @"\d+").Success;
                    int depth = this.MaxRecursionDepth;

                   // MessageBox.Show(string.Format("{0} {1}", success, depth));
                    if (!Regex.Match(this.MaxRecursionDepth.ToString(), @"\d+").Success || this.MaxRecursionDepth < 0)
                    {
                        retvalue = string.Format("Please enter a valid DirectoryPath.\n'{0}' doesn't exist", this.DestinationOfFile);
                    }
                }
                return retvalue;
            }
        }
        #endregion
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
        #endregion
        #endregion
        #region Constructors
        #region Constructor() 
        /// <summary>
        /// Don't use this constructor.  It is for private use.
        /// </summary>
        private Downloader(string url, string destination)
        {
            //Intializing Members
            DestinationOfFile = destination.Trim();
            Url = url.Trim();
        }
        #endregion
        #region Constructor 2
        /// <summary>
        /// Asks for destination with GUI, sets extension to default .mp3, and set recursion to true
        /// </summary>
        /// <param name="url">url containing links to files or other urls with files</param>
        /// <param name="destination">where to save files locally.  If left blank, a SaveFileDialog box will ask for location</param>
        public Downloader(string url, string destination = DESTINATION_DEFAULT, string extension = EXTENSION_DEFAULT) : this(url, destination)
        {
            CurrentExtensions.Add(extension);
        }
        #endregion
        #region Constructer 3
        /// <summary>
        /// Asks for destination with GUI, sets extension to default .mp3, and set recursion to true
        /// </summary>
        /// <param name="url">url containing links to files or other urls with files</param>
        /// <param name="destination">where to save files locally.  If left blank, a SaveFileDialog box will ask for location</param>      
        public Downloader(string url, string[] extensions, string destination = DESTINATION_DEFAULT) : this(url, destination)
        {
            foreach (var extension in extensions)
            {
                CurrentExtensions.Add(extension);
            }
        }
        #endregion
        #endregion
        #region Methods
        #region MainLogic Methods
        #region StartDownload
        public void StartDownload(string url = null, string destination=null)
        {
            
            #region Handling Click on Download Button when Download in progress
            if (!String.IsNullOrEmpty(url))
            {
                Url = url;
            }
            if (!String.IsNullOrEmpty(destination))
            {
                DestinationOfFile= destination;
            }

            if (CachedUrls.Count > 0 && CachedUrls.All(u => u.Trim().ToLower() != Url.Trim().ToLower()))
            {
                MessageBox.Show(string.Format("Caching '{0}\nDestination: {1}'", Url, OriginalDestination));
            }
            CachedUrls.Add(Url);
            if (!TransferComplete && TransferStarted)
            {
            }
            #endregion
            else
            {
                #region Intitializing Stuff
                OriginalUrl = Url;
                TransferStarted = true;
                ShouldCancel = false;
                TransferProgress = "";
                TransferProgressLineCount = 1;
                TransferComplete = false;
                UpdateTransferProgress("Setting Up...", true);
                //The following code will handle all unhandled exceptions in the program, and accounts for
                //good practices in exception management.The program should not crash randomly, and, if
                //it needs to crash at all, then it should log information and clean up all resources.
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show($"Program Crashed", $"Unhandled Exception Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TaskScheduler.UnobservedTaskException += (s, e) =>
               MessageBox.Show($"Program Crashed", $"Unhandled Exception Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);

                #endregion
                //Program can find different media files;  these extensions determine which extensions to look for in ProcessURL
                SetCurrentExtension();
                try
                {
                    SetupEnvironment();
                }
                catch (Exception e)
                {
                    ResetMembers();
                    throw new Exception(e.Message);
                }
                #region Case: File (Downloads file and catches errors)
                if (OriginalUrl.StartsWith("http") && IsFile == true && !MenuItem_IsYouTubeUrl_Flag)
                {
                    string message = null;
                    try
                    {
                        Task downloadFile = Task.Factory.StartNew(() =>
                        {
                            UpdateTransferProgress($"Starting Download of {OriginalUrl}");
                            DownLoadFile(OriginalUrl);
                        });
                    }
                    #region Exception Handling
                    catch (ArgumentNullException)
                    {
                        message = string.Format("Error Downloading '{0}'.\nEither the OriginalUrl or the file destination are not correct.  Please check both.", OriginalUrl);
                        throw new Exception(message);
                    }
                    catch (WebException)
                    {
                        message = string.Format("Error Downloading '{0}'.\nThe website didn't respond.  Please check the OriginalUrl or try again later.", OriginalUrl);
                        throw new Exception(message);
                    }
                    catch (InvalidOperationException)
                    {
                        message = string.Format("Error Downloading '{0}'.\nInvalid Operation", OriginalUrl);
                        throw new Exception(message);
                    }
                    catch (UriFormatException)
                    {
                        message = string.Format("'{0}' is not a valid OriginalUrl.Please try again.\n", OriginalUrl);
                        throw new Exception(message);
                    }
                    #endregion
                }
                #endregion
                else if (OriginalUrl.StartsWith("http") && MenuItem_IsYouTubeUrl_Flag)
                {
                    Task downloadYoutubeFile = Task.Factory.StartNew(() =>
                    {
                        DownloadAudioFromYouTube();
                    });
                }
                #region Case: Website
                else
                {
                    StartBatchDownload();
                }
                #endregion
            }
        }
        #endregion
        #region SetupEnvironment
            private void SetupEnvironment()
        {
            #region Setting PrefixFileName Based On MenuItems
            if (MenuItem_Regex_Custom_Flag == true)
            {
                PrefixFileName = false;
            }
            #endregion
            #region Getting Path, Filename, and DirectoryName, and setting DatePath
            #region Gets the destination for the files if the user didn't specify one

            if (DestinationOfFile == MainWindow.startingDestination || string.IsNullOrEmpty(DestinationOfFile))
            {
                GetDestinationWithGUI();
            }
            #endregion
            #region Gets the destination, et. al when using Destinationtextbox
            else
            {
                DirectoryPath = DestinationOfFile;
            }
            #endregion
            #region Renaming Files if Selected
            if (MenuItem_CreateFolder_Flag && !IsFile)
            {
                string newFolderName;
                newFolderName = FileNamePrefix != "" ? FileNamePrefix : Path.GetFileNameWithoutExtension(OriginalUrl);
                newFolderName = ReplaceCharactersInFileName(newFolderName);
                newFolderName = Regex.Replace(newFolderName, @"\s+", @" ");
                newFolderName = RenameFile.CapitalizeFirstLetter(newFolderName, ' ');
                
                //creating new DirectoryPath and changing DirectoryPath path to the new one
                DirectoryPath = Path.Combine(DirectoryPath, newFolderName);
                //MessageBox.Show(string.Format("DirectoryPath: {0}\n newFolderName: {1}", DirectoryPath, newFolderName));
                Directory.CreateDirectory(DirectoryPath);
            }
            #endregion
            DatePath = Path.Combine(DirectoryPath, "Date.txt");
            #endregion
            #region Gets the Prefix for recursive Downloads.  If not recursive the file is just named whatever is given in the SaveDialogbox
            if (!IsFile)
            {
                if (PrefixFileName == true && FileNamePrefix == FILENAME_PREFIX_DEFAULT)
                {
                    FileNamePrefix = Path.GetFileNameWithoutExtension(DirectoryPath);
                }
            }
            #endregion
        }
        #endregion
        #region StartBatchDownload
        private void StartBatchDownload()
        {
            Console.WriteLine();
            UpdateTransferProgress($"Looking for Urls in {OriginalUrl}");
            //Start the process of finding urls and continue to find them
            Task GetUrlsTask = Task.Factory.StartNew(() =>
            {
                UrlsTextFilePath = Path.Combine(DirectoryPath, UrlsTextFileName);
                UrlsTextExists = File.Exists(UrlsTextFilePath);
                GetUrlsInSourceCode(OriginalUrl);
                FinishedGettingSourceCode = true;
            });

            //start downloading once the first file is put into DownloadQueue
            Task DownloadFilesLoopTask = Task.Factory.StartNew(() =>
            {
                //todo: add the option to ask before downloading?  E.g. get a list of urls and display then download selected.
                //todo add option to display renaming file results
                //todo add another window to get and select urls to batch download from
                DownloadFilesLoop();
                WriteToUrlsLog(UrlsTextFilePath);
                WriteDate();
                #region Waiting Until all files Completely Downloaded
                while (FilesDownloaded != FilesStarted)
                {
                    Thread.Sleep(200);
                }
                #endregion
                #region Checking Should Cancel and Renaming and Verifying if false
                if (ShouldCancel == true)
                {
                    UpdateTransferProgress("Cancelled by User");
                }
                else
                {
                    #region Renaming Files if Specified
                    if (MenuItem_RenameFiles_Flag)
                    {
                        #region Renaming Files
                        while (ShouldCancel == false) 
                        {
                            RenameFile.RenameDirectory(DirectoryPath, displayResults: DisplayResults);
                            if (RenameFile.ChangesList.Count > 0)
                            {
                                foreach (Tuple<string,string,string> line in RenameFile.ChangesList)
                                {
                                    UpdateTransferProgress(line.Item1);
                                }
                            }
                            UpdateTransferProgress("Renaming Files Complete", true);
                            break;
                        }
                        #endregion
                    }
                    #endregion
                    VerifyDownloads();
                }
                #endregion
                #region Starting Cached Urls if they Exist
                //todo: figure out how to start cached urls with the same destination as first batch
                bool removed =  CachedUrls.Remove(OriginalUrl);
                if (CachedUrls.Count > 0 && InCachedUrlRecursion == false)
                {
                    ConcurrentQueue<string> cachedUrlsQueue = new ConcurrentQueue<string>();
                    HashToQueue(CachedUrls, ref cachedUrlsQueue);
                    string nextUrl = "";
                    while (cachedUrlsQueue.TryDequeue(out nextUrl))
                    {
                        InCachedUrlRecursion = true;
                       // MessageBox.Show(string.Format("nextUrl: {0}, OriginalDestination: {1}\nCachedUrls.Count : {2}  ", nextUrl, OriginalDestination, CachedUrls.Count));
                        ResetMembers();
                        StartDownload(nextUrl, OriginalDestination);
                        InCachedUrlRecursion = false;
                    }
                }
                else if (InCachedUrlRecursion ==false) 
                {
                    CachedUrls.Clear();
                    ResetMembers();
                }
                #endregion
            });
        }
        #endregion
        #region GetUrlsInSourceCode
        private void GetUrlsInSourceCode(string url)
        {
            CurrentRecursionDepth.Value += 1;
            //MessageBox.Show(string.Format("CurrentRecursionDepth out of loop: {0}\nMaxRecursionDepth  out of loop: {1}\nUrl: {2}", CurrentRecursionDepth, MaxRecursionDepth, url));
            if (CurrentRecursionDepth.Value <= MaxRecursionDepth && ShouldCancel == false)
            {
                //Start a separate thread to get website urls in SourceCode only if the user specified a regex for the intermediate pages
                string sourceCode = GetURLSourceCode(url);
                if (sourceCode != null)
                {
                    //Task GetNextUrlMatchesTask = null;
                    if (REGEX_DEFAULT_URLS != RegularExpressionForUrls)
                    {
                        //GetNextUrlMatchesTask = Task.Factory.StartNew(() =>
                        //{
                            GetNextUrlMatches(sourceCode);
                        //});
                    }
                    //UpdateTransferProgress("In source code finding still...");
                    //GetNextUrlMatchesTask.Wait();
                    GetNextFileMatches(sourceCode);
                }
            }
        }
        #endregion
        #region GetNextUrlMatches
        private void GetNextUrlMatches(string sourceCode)
        {
            #region Initialization and Getting nextUrlMatches
            //nextURLMat
            MatchCollection nextUrlMatches = Regex.Matches(sourceCode, RegularExpressionForUrls);
            List<string> nextUrlMatchesList = new List<string>();
            for (int i = 0; i < nextUrlMatches.Count; i++)
            {
                string nextToAdd = nextUrlMatches[i].Groups[1].ToString();
                if (!nextUrlMatchesList.Contains(nextToAdd))
                {
                    nextUrlMatchesList.Add(nextToAdd);
                }
            }
            MatchCollection urlDatesNotFeatured = null;
            MatchCollection urlDatesFeatured = null;
            bool Continue = true;
            List<string> urlDatesCombined = new List<string>();
            #endregion

            #region Gettings Dates if Downloading Based on Date
            if (MenuItem_Download_By_Date_Flag || MenuItem_Download_SelectDate_Flag)
            {
                urlDatesFeatured = Regex.Matches(sourceCode, @"class=""col-border-left"">\s*(.+)\s*<br>");
                urlDatesNotFeatured = Regex.Matches(sourceCode, DateRegex);
                for(int i = 0; i < urlDatesNotFeatured.Count; i++)
                {
                    urlDatesCombined.Add((urlDatesNotFeatured[i].Groups[1].ToString()));
                }
                for (int i = 0; i < urlDatesFeatured.Count; i++)
                {
                    urlDatesCombined.Add((urlDatesFeatured[i].Groups[1].ToString()));
                }
            }
            #endregion

            #region Iterating through all matches to determine if they are likely a website or file
            for (int i = 0; i < nextUrlMatchesList.Count; i++)
            {
                if (MenuItem_Download_By_Date_Flag || MenuItem_Download_SelectDate_Flag)
                {
                    if (i < urlDatesCombined.Count)
                    {
                        Continue = GetShouldDownloadByDate(urlDatesCombined[i]);
                    }
                }
                if (Continue)
                {

                    string urlSubSection = nextUrlMatchesList[i];
                    urlSubSection = urlSubSection.Replace("&amp;", "&");
                    urlSubSection = urlSubSection.Replace("&apos;;", "'");
                    urlSubSection = urlSubSection.Trim();

                    AddUrlToQueue(urlSubSection);                   
                    //MessageBox.Show(string.Format("CurrentRecursionDepth in loop: {0}\nURL subsection:", CurrentRecursionDepth, urlSubSection));
                    //MessageBox.Show(string.Format("urlSubSection {0}\nnextUrlMatches.Count: {1}\nurlDatesCombined.Count: {2}", urlSubSection, nextUrlMatches.Count, urlDatesCombined.Count));
                }
            }
            #endregion
        }
        #endregion
        #region GetNextFileMatches
        private void GetNextFileMatches(string sourceCode)
        {
            #region Iterates through each of the file extension types given to get UrlsToDownload and UrlsToRecurse
            foreach (var extension in CurrentExtensions)
            {
                //Gets the matches for each extension type wanting to download
                MatchCollection nextFileUrlMatches = Regex.Matches(sourceCode, string.Format(RegularExpressionForFiles, extension));

                //Iterates through all matches to determine if they are likely a website or file
                foreach (Match match in nextFileUrlMatches)
                {
                    string urlSubSection = match.Groups[1].ToString();
                    urlSubSection = urlSubSection.Replace("&amp;", "&");
                    urlSubSection = urlSubSection.Replace("&apos;;", "'");
                    urlSubSection = urlSubSection.Trim();
                    AddUrlToQueue(urlSubSection);
                }
            }
            #endregion
            #region Populating DownloadQueue
            if (UrlsToDownload.Count > 0)
            {
                //add UrlsToDownload to a queue
                foreach (var item in UrlsToDownload)
                {
                    int recursionDepth = item.RecursionDepth;
                    if (recursionDepth >= MinRecursionDepth && recursionDepth <= MaxRecursionDepth)
                    {
                        DownloadQueue.Enqueue(item.Url);
                    }
                }

                //clearing for recursive calls
                lock (UrlsToDownload)
                {
                    UrlsToDownload.Clear();                                
                }
            }
            #endregion
            #region Populating RecurseUrlsQueue
            if (UrlsToRecurse.Count > 0)
            {
                #region Adding UrlstoRecurse to a queue
                HashToQueue(UrlsToRecurse, ref RecurseUrlsQueue);
                lock (UrlsToRecurse)
                {
                    UrlsToRecurse.Clear();
                }
                #endregion

                #region while there is still something in the queue check the next one and remove it
                while (RecurseUrlsQueue.Count > 0)
                {
                    string nextURL;
                    RecurseUrlsQueue.TryDequeue(out nextURL);
                    GetUrlsInSourceCode(nextURL);
                }
                #endregion
            }
            #endregion
        }
        #endregion
        
        #region AddUrlToQueue
        private void AddUrlToQueue(string urlSubSection)
        {
            if (!UniqueUrls.Contains(urlSubSection))
            {
                //Keeping track of Unique Urls
                UniqueUrls.Add(urlSubSection);
                //todo: CurrentRecursionDepth either need reworking or transforming into an item picker (download 3rd-15th file) 
                //MessageBox.Show(string.Format("CurrentRecursionDepth: {0}\n urlSubSection: {1}", CurrentRecursionDepth, urlSubSection));
                #region In the Case the url contains http, add to UrlsToDownload
                if (urlSubSection.Trim().Contains("http"))
                {
                    //probably a file to download
                    lock (UrlsToDownload)
                    {
                        UrlsToDownload.Add(new UrlObject(urlSubSection, CurrentRecursionDepth.Value));
                    }
                    //MessageBox.Show(string.Format("Download: \nurlSubSection: {0}", urlSubSection));
                }
                #endregion

                #region Otherwise add to UrlsToRecurse
                else
                {
                    //probably a another page
                    lock (UrlsToRecurse)
                    {
                        UrlsToRecurse.Add(new UrlObject(GetBaseUrl(OriginalUrl) + urlSubSection, CurrentRecursionDepth.Value));
                    }
                    //MessageBox.Show(string.Format("Website: \n Url: {0}", GetBaseUrl(Url) + urlSubSection));
                }
                #endregion
            }
        }
        #endregion
        #region DownloadFilesLoop
        private void DownloadFilesLoop()
        {
            //MessageBox.Show(string.Format("DownloadFilesLoop Started"));
            while (UrlsToRecurse.Count > 0 || UrlsToDownload.Count > 0 || DownloadQueue.Count > 0 | RecurseUrlsQueue.Count > 0 || FinishedGettingSourceCode == false)
            {
                if (ShouldCancel == false)
                {
                    //download files in queue
                    while (DownloadQueue.Count > 0)
                    {
                        string nextURL;
                        //MessageBox.Show(string.Format("nextDownload file: {0}", nextDownload));
                        DownloadQueue.TryDequeue(out nextURL);
                        DownLoadFile(nextURL, true);
                    }
                }
                else
                {
                    break;
                }
            }
            //MessageBox.Show(string.Format("DownloadFilesLoop Ended"));
        }
        #endregion
        #endregion
        #region DownLoadFile
        private void  DownLoadFile(string url, bool recursive = false)
        {
            #region Sets DestinationOfFiles for recursive calls only
            if (recursive)
            {
                GetDestinationOfBatchFiles(url);
            }
            #endregion
            //taskCompletionSource is needed for exception handling
            #region Getting Name of Renamed File to Check Against
            string fileNamePathToCheck = null;
            if (UrlsTextExists && MenuItem_KeepTrackOfFilesDownloaded_Flag)
                {
                    bool matchSuccess = false;
                    string urlsTextContents = File.ReadAllText(UrlsTextFilePath);
                    matchSuccess = Regex.Match(urlsTextContents, url, RegexOptions.IgnoreCase).Success;
                    if (matchSuccess)
                    {
                        string[] urlsTextContentsLines = File.ReadAllLines(UrlsTextFilePath);
                        foreach (string line in urlsTextContentsLines)
                        {
                            if (line.Contains(url))
                            {
                                string compareAgainst = GetCompareAgainst(line);
                                fileNamePathToCheck = Path.Combine(DirectoryPath, compareAgainst);
                                fileNamePathToCheck = !Path.GetExtension(fileNamePathToCheck).Contains("mp3") ? fileNamePathToCheck + ".mp3" : fileNamePathToCheck;
                                break;
                            }
                        }
                    }
                }
            #endregion
            #region Downloading File if it doesn't already Exist
            //if (!File.Exists(fileNamePathToCheck) && !File.Exists(DestinationOfFile))
            //{
            #region Sleeping after 1st iteration
            if (IsFirstTimeDownloading)
                {
                    IsFirstTimeDownloading = false;
                }
                else
                {
                    Thread.Sleep(DurationBetweenDownloads);
                }
                #endregion
                #region Using WebClient to Download
                using (WebClient client = new WebClient())
                {
                    bool originalExists = File.Exists(DestinationOfFile);
                    bool renamedExists = File.Exists(fileNamePathToCheck);
                    if (originalExists || renamedExists)
                    {
                        FileInfo fileInfo = new FileInfo(originalExists ? DestinationOfFile : fileNamePathToCheck);
                        long expectedLength = fileInfo.Length;
                        using (var sr = client.OpenRead(url))
                        {
                            Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                            //MessageBox.Show(string.Format("File: {0}, fileInfo.Length: {1}\n Expected: {2}", FileName ,fileInfo.Length, bytes_total.ToString()));
                            if (bytes_total != expectedLength)
                            {
                                ActuallyDownloadFile(client, url);
                            }
                            else
                            {
                                UpdateTransferProgress($"Skipping: '{FileName}'");
                            }
                        }
                }
                else
                    {
                        ActuallyDownloadFile(client, url);
                    }                   
                }
            #endregion
            #endregion
        }
        #endregion             
        #region ResetMembers
        private void ResetMembers()
        {            
            //MessageBox.Show(string.Format("ResetMembers Reached"));
            TransferStarted = false;
            TransferComplete = true;
            ShouldCancel = false;
            IsFirstTimeDownloading = true;
            FileExtension = "";
            DirectoryPath = "";
            FileName = "";
            FileNamePrefix = "";
            DestinationOfFile = "";
            FinishedGettingSourceCode = false;
            ProgressValue = 0;
            TotalDownloadReceived = "";
            TotalDownloadSize = "";
            //DownloadSpeed = "";
            DatePath = "";
            CurrentRecursionDepth.Value = -1;
            Recursion = true;
            UrlsToDownload.Clear();
            UrlsToRecurse.Clear();
            RecurseUrlsQueue = new ConcurrentQueue<string>();
            DownloadQueue = new ConcurrentQueue<string>();
            UniqueUrls.Clear();
            FilesDownloaded = 0;
            FilesStarted = 0;
            RenameFile.ChangesList.Clear();
    }
    #endregion
        #region VerifyDownloads
        private void VerifyDownloads()
        {
            if (FilesDownloaded > 0)
            {
                List<string> filesNeedingRedownload = new List<string>();
                #region Getting the Files in the Directory with the Specified Extensions
                List<string> filesInDirectoryList = new List<string>();
                string[] files;
                //todo: figure out how to get a list off all the media files in the DirectoryPath (excluding *.txt
                foreach (var extension in CurrentExtensions)
                {
                    files = Directory.GetFiles(DirectoryPath, string.Format("*{0}", extension));
                    foreach (var file in files)
                    {
                        filesInDirectoryList.Add(file);
                    }
                }
                #endregion

                #region Testing (needed for the Mbox below)
                //StringBuilder fileSizes = new StringBuilder();
                //foreach (var fileSize in FileSizes)
                //{
                //    fileSizes.Append(fileSize + ", ");
                //}
                #endregion

                #region Checking each File in DirectoryPath to see if the Sizes match the expected Sizes
                foreach (string file in filesInDirectoryList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    //MessageBox.Show(string.Format("file: {0}\n fileInfo.Length: {1}\n FileSizes:{2}", file, fileInfo.Length, fileSizes));
                    if (!FileSizes.Contains(fileInfo.Length))
                    {
                        //todo: issue with it saying that you need to redownload files that were already in DirectoryPath when the download started.
                        //todo: need to only do something with the files that were actually attempted to download.  Use the files in files
                        //MessageBox.Show(string.Format("file: {0} needs to be redownloaded", file));
                        filesNeedingRedownload.Add(file);
                        UpdateTransferProgress($"'{file}' needs to be redownloaded.");
                    }
                }
                #endregion
                UpdateTransferProgress("Verifying Files Complete");

            }
        }
        #endregion
        #region DownloadAudioFromYouTube
        public void DownloadAudioFromYouTube()
        {
            string filePath = null;
            using (var cli = Client.For(YouTube.Default))
            {
                string url = OriginalUrl;
                var videoInfos = cli.GetAllVideos(url);
                //var possibleBitrates = videoInfos.Where(i => i.AdaptiveKind == AdaptiveKind.Audio).Select(i => i.AudioBitrate);
                //var possibleResolutions = videoInfos.Where(i => i.AdaptiveKind == AdaptiveKind.Video).Select(i => i.Resolution);
                foreach (var video in videoInfos)
                {
                    if (video.AdaptiveKind == AdaptiveKind.Audio)
                    //if(video.AudioFormat == AudioFormat.Aac)
                    //if(video.AudioBitrate == 128)
                    //if(video.AdaptiveKind == AdaptiveKind.Video)
                    //if (video.Format == VideoFormat.Mp4)
                    //if (video.Resolution == 360)
                    {
                        string downloadUri = video.Uri;
                        filePath = Path.Combine(DirectoryPath, video.FullName);
                        Uri uri = new Uri(downloadUri);
                        using (WebClient webClient = new WebClient())
                        {
                            webClient.DownloadFile(uri, filePath);
                        }
                        //var videoToDownload = cli.GetVideo(downloadUri);
                        //byte[] VideoBytes = videoToDownload.GetBytes();
                        //File.WriteAllBytes(filePath, VideoBytes);
                    }
                }
                //OR
                //var downloadInfo = videoInfos.Where(i => i.AudioFormat == AudioFormat.Aac && i.AudioBitrate == 128).FirstOrDefault();
                var downloadInfo = videoInfos.Where(i => i.Format == VideoFormat.Mp4 && i.Resolution == 720).FirstOrDefault(); // if 720p is possible
               

                //MessageBox.Show(string.Format("BitRate of video grabbing: {0}\nUrl: {1}\nDestinationOfFile: {2}", video.AudioBitrate, Url, DestinationOfFile));
                
            }
            //string downloadUri = downloadInfo.Uri;
            //var youtube = YouTube.Default;
            //var video = youtube.GetVideo(downloadUri);

            var inputFile = new MediaFile { Filename = filePath };
            var outputFile = new MediaFile { Filename = $"{filePath}.mp3" };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);

                engine.Convert(inputFile, outputFile);
            }
        }
        #endregion
        #endregion
        #region Helper Methods
        #region HashToQueue
        private void HashToQueue(HashSet<UrlObject> urlsToRecurse, ref ConcurrentQueue<string> recurseUrlsQueue)
        {
            foreach (var item in urlsToRecurse)
            {
                recurseUrlsQueue.Enqueue(item.Url);
            }
        }
        private void HashToQueue(HashSet<string> urlsToRecurse, ref ConcurrentQueue<string> recurseUrlsQueue)
        {
            foreach (var item in urlsToRecurse)
            {
                recurseUrlsQueue.Enqueue(item);
            }
        }
        #endregion
        #region ActuallyDownloadFile
        private void ActuallyDownloadFile(WebClient client, string url)
        {
            #region Handling the DownloadFileCompleted Event
            client.DownloadFileCompleted += (sender, e) =>
            {
                UpdateTransferProgress($"Finished Downloading: '{FileName}'");
                FilesDownloaded++;
                //MessageBox.Show(string.Format("FileSize for {1}: {0}", FileSize, FileName));
                if (e.Error != null)
                {
                    MessageBox.Show("Error Occured Downloading.  Caching Request");
                }
                else if (e.Cancelled)
                {
                    MessageBox.Show("Download has been canceled.");
                }
                else
                {
                    Stopwatch.Stop();
                    //MessageBox.Show(string.Format("Download of '{0}{1}' is complete.", FileName, FileExtension));
                }
            };
            #endregion
            #region Handling the DownloadProgressChanged Event

            client.DownloadProgressChanged += (s, e) =>
            {
                FileSizes.Add(e.TotalBytesToReceive);

                //Setting speed
                //DownloadSpeed = string.Format("{0:##.#}", (10 * e.BytesReceived / BYTES_TO_MB_FACTOR / Downloader.Stopwatch.Elapsed.TotalSeconds).ToString("0.0"));

                //Setting amount to download
                TotalDownloadSize = string.Format("{0:##.##}", e.TotalBytesToReceive / BYTES_TO_MB_FACTOR);

                //Setting total received
                TotalDownloadReceived = string.Format("{0:##.##}", e.BytesReceived / BYTES_TO_MB_FACTOR);

                //MessageBox.Show(string.Format("e.ProgressPercentage: {0}", e.ProgressPercentage));

                // MessageBox.Show(string.Format("Downloadfile Task.CurrentId: {0}", Task.CurrentId));
                //Setting progress bar value
                ProgressValue = e.ProgressPercentage;
            };
            #endregion
            #region Starting the stopwatch for Speed Calculation
            Stopwatch.Start();
            #endregion
            #region  Actually Downloading the File
            Uri uri = new Uri(url);
            UpdateTransferProgress($"Started Downloading: '{FileName + FileExtension}'");
            UrlsLogText.Add(string.Format("{0}, {1}\n", FileName + FileExtension, url));
            FilesStarted++;
            lock (client)
            {
                if (DownloadAsync)
                {
                    client.DownloadFileAsync(uri, DestinationOfFile);
                }
                else
                {
                    client.DownloadFile(uri, DestinationOfFile);
                }
            }
            #endregion
        }
        #endregion
        #region GetDestinationOfFiles
        private void GetDestinationOfBatchFiles(string url)
        {
            FileName = ReplaceCharactersInFileName(Path.GetFileNameWithoutExtension(url));
            FileExtension = Path.GetExtension(url);

            if (PrefixFileName)
            {
                FileName = string.Format("{0} - {1}", FileNamePrefix, FileName);
                DestinationOfFile = string.Format("{0}\\{1}{2}", DirectoryPath, FileName, FileExtension);
            }
            else
            {
                DestinationOfFile = string.Format("{0}\\{1}", DirectoryPath, FileName + FileExtension);
            }
        }
        #endregion
        #region GetBaseUrl
        /// <summary>
        /// Finds the main page of a URL (e.g. 'http://site.com/page' => 'http://site.com'
        /// </summary>
        /// <param name="url"></param>
        /// <returns>the main page of a UR.  returns null if not foundL</returns>
        public static string GetBaseUrl(string url)
        {
            foreach (var domain in TopLevelDomains)
            {
                //case when TLD is in middle of url
                if (url.Contains(string.Format("{0}/", domain)))
                {
                    MatchCollection domainMatch = Regex.Matches(url, string.Format(@"(http.+{0})/.*", domain));
                    return domainMatch[0].Groups[1].ToString();
                }
                //case when TLD is at end
                else if (url.EndsWith(string.Format("{0}", domain)))
                {
                    MatchCollection domainMatch = Regex.Matches(url, string.Format(@"(http.+{0})", domain));
                    return domainMatch[0].Groups[1].ToString();
                }
            }
            return null;
        }
        #endregion
        #region GetURLSourceCode
        /// <summary>
        /// Returns source code of a URL
        /// </summary>
        /// <param name="url">url of site</param>
        /// <returns>source code of url</returns>
        /// /// <exception cref="ArgumentException"></exception>
        /// /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="SecurityException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public string GetURLSourceCode(string url)
        {
            Thread.Sleep(DurationBetweenSourceCodeDownloads);
            string sourceCode;
            Stream data = null;
            try
            {
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                data = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(data))
                {
                    sourceCode = sr.ReadToEnd();
                }
                return sourceCode;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Exception: {0}", e.Message));
                return null;
            }
        }
        #endregion
        #region ReplaceCharactersInFileName
        private string ReplaceCharactersInFileName(string filename)
        {
            char[] invalidDirectoryChars= Path.GetInvalidPathChars();
            char[] invalidFileNameChars= Path.GetInvalidFileNameChars();
            string res = filename;
            string[] toRemove = { @"_", @"%", @"-", @"\." };
            string[] toReplaceWith = { " ", " ", " ", " - " };

            for (int i = 0; i < toRemove.Length; i++)
            {
                res = Regex.Replace(res, toRemove[i], toReplaceWith[i]);
            }
            foreach (char c in invalidDirectoryChars)
            {
                res = res.Replace(c, ' ');
            }
            foreach (char c in invalidFileNameChars)
            {
                res = res.Replace(c, ' ');
            }
            res.Replace("  ", " ");
            return res;
        }
        #endregion
        #region GetDestinationWithGUI
        /// <summary>
        /// propmps user for a DirectoryPath to save file
        /// </summary>
        /// <returns>string of path to save files.  returns null if user cancels</returns>
        /// <exception cref="TaskCanceledException"></exception>
        public void GetDestinationWithGUI()
        {
            #region Initializing 
            string fileNameFromURI = Path.GetFileNameWithoutExtension(OriginalUrl).Trim();
            fileNameFromURI = ReplaceCharactersInFileName(fileNameFromURI);
            fileNameFromURI = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileNameFromURI);
            string extension = Path.GetExtension(OriginalUrl.Trim());
            #endregion
            #region Getting IsFile
            ////MessageBox.Show(string.Format("extension: {0}\n is empty: {1}", extension, string.IsNullOrEmpty(extension)));
            //bool containsTLD = TopLevelDomains.Any(domain => extension.ToUpper().Trim() == domain.ToUpper().Trim());
            //if (!(string.IsNullOrEmpty(extension) || containsTLD))
            //{
            //    //MessageBox.Show(string.Format("This is a file"));
            //    IsFile = true;
            //}
            #endregion
            #region Opening SaveDialogBox if IsFile
            if (IsFile)
            {
                #region SaveFileDialogBox Setup and Get Answer
                SaveFileDialog saveFileDialog = new SaveFileDialog()
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = "Save Download to...",
                    FileName = fileNameFromURI
                };

                if (!string.IsNullOrEmpty(extension))
                {
                    saveFileDialog.Filter = string.Format("{1} files (*{0})|*{0}", extension, extension.Substring(1));
                }
                //Getting the result of the box
                DialogResult dialogResult = saveFileDialog.ShowDialog();
                #endregion
                #region Handling User's Response to SaveBox and Setting Properties
                if (dialogResult == DialogResult.OK)
                {
                    DestinationOfFile = saveFileDialog.FileName;
                    DirectoryPath = Path.GetDirectoryName(DestinationOfFile);
                    FileName = Path.GetFileNameWithoutExtension(DestinationOfFile).Trim();
                    FileExtension = Path.GetExtension(DestinationOfFile).Trim();
                    //MessageBox.Show(string.Format("DirectoryPath.GetParent(DestinationOfFile).ToString(): {0}", DirectoryPath.GetParent(DestinationOfFile).ToString()));
                }
                else if (dialogResult == DialogResult.Cancel)
                {
                    throw new TaskCanceledException("User Cancelled the Download.");
                }
                else
                {
                    GetDestinationWithGUI();
                }
                #endregion
            }
            else
            {
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.ShowNewFolderButton = true;
                    folderBrowserDialog.Description = "Select A Folder to Save the File in";
                    folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        DirectoryPath = folderBrowserDialog.SelectedPath;
                    }
                }
            }
            OriginalDestination = DirectoryPath;
            #endregion
        }
        #endregion
        #region WriteDate
        private void WriteDate()
        {
            DateTime date = DateTime.Now;
            FileInfo dateFile = new FileInfo(DatePath);

            //Removing Hidden Attribute so it is writeable
            if (File.Exists(DatePath))
            {
                dateFile.Attributes &= ~FileAttributes.Hidden;
            }
            File.WriteAllText(DatePath, date.ToString());

            //Applying Hidden property
            dateFile.Attributes |= FileAttributes.Hidden;
            //MessageBox.Show(string.Format("date: {0}\ndate2: {1}\ndate > date2 {2}", date, date2, date>date2));
        }

        #endregion
        #region GetShouldDownloadByDate
        /// <summary>
        /// Determines if a file has already been downloaded in a previous download session for the specified DirectoryPath.  Used for sites like OCRemix.org where the date of publishing is available.
        /// </summary>
        /// <param name="urlDate">the string representation of the date the file was published</param>
        /// <returns>true if the urlDate is larger than date in the File otherwise false.  If no urlDate is available,  true is returned</returns>
        private bool GetShouldDownloadByDate(string urlDate)
        {
            bool Continue = true;
            DateTime urlDateTime = new DateTime();
            if (DateTime.TryParse(urlDate, out urlDateTime))
            {
                if (File.Exists(DatePath) && !MenuItem_Download_SelectDate_Flag)
                {
                    string temp = File.ReadAllText(DatePath);
                    DateTime result;
                    if (DateTime.TryParse(temp, out result))
                    {
                        DownloadSinceDate = result;
                    }
                }
                if (DownloadSinceDate.Date >= urlDateTime)
                {
                    Continue = false;
                }
            }
            else
            {
                throw new Exception($"Issue Parsing the Date: {urlDate}.");
            }
            //MessageBox.Show(string.Format("DownloadSinceDate.Date: {0}", DownloadSinceDate.Date));
            return Continue;
        }
        #endregion
        #region SetCurrentExtension
        private void SetCurrentExtension()
        {
            string extension = Path.GetExtension(OriginalUrl);
            if (IsFile)
            {
                if (!CurrentExtensions.Contains(extension))
                {
                    CurrentExtensions.Add(extension);
                }
            }
        }
        #endregion
        #region GetIsValidUrl
        /// <summary>
        /// Determines whether a string is considered a valid URL based on all of the possible registered toplevel domain names.
        /// </summary>
        /// <param name="url">String of the url</param>
        /// <returns>bool of whether url is considered a valid URL</returns>
        public static bool GetIsValidUrl(string url)
        {
            //Creating Instances
            bool res = false;
            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions po = new ParallelOptions
            {
                // Use ParallelOptions instance to store the CancellationToken
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = System.Environment.ProcessorCount
            };

            try
            {
                Parallel.ForEach(TopLevelDomains, po, (domain, state) =>
                {
                    string nextRegex = string.Format(UrlRegex, domain);
                    //MessageBox.Show(string.Format("Checking: {0}", nextRegex));
                    MatchCollection matches = Regex.Matches(url, nextRegex);
                    if (matches.Count > 0)
                    {
                        res = true;
                        po.CancellationToken.ThrowIfCancellationRequested();
                        state.Stop();
                        //MessageBox.Show(string.Format("Match found for: {0}", nextRegex));
                    }
                });
                return res;
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e.Message);
                return res;
            }
            finally
            {
                cts.Dispose();
            }
        }
        #endregion
        #region UpdateTransferProgress
        private void UpdateTransferProgress(string msg, bool addSeparator = false)
        {
            int i = 0;
            lock (TransferProgress)
            {
                if (addSeparator)
                {
                    if (i != 0)
                    {
                        TransferProgress += "\n";
                    }
                    TransferProgress += "-----------------------------------------------------";
                    i++;
                }
                TransferProgress += "\n";
                TransferProgress += $"{TransferProgressLineCount} - {msg}";
            }
            Interlocked.Increment(ref TransferProgressLineCount);
        }
        #endregion
        #region WriteToUrlsLog
        private void WriteToUrlsLog(string urlsFilePath)
        {
            //Create urls.txt if it doesn't exist
            File.AppendAllText(UrlsTextFilePath, null);
            string urlFileContents = File.ReadAllText(UrlsTextFilePath);
            if (!urlFileContents.Contains(WEBSITE_DEMARCATION_MARKER))
            {
                File.AppendAllText(urlsFilePath, string.Format("{0} {1}", WEBSITE_DEMARCATION_MARKER, OriginalUrl));
                File.AppendAllText(urlsFilePath, string.Format("\n"));
            }
            foreach (string line in UrlsLogText)
            {
                File.AppendAllText(Path.Combine(DirectoryPath, UrlsTextFileName), line);
            }
        }
        #endregion
        #region GetCompareAgainst
        /// <summary>
        /// Takes a line and separates it at a character, but only returns all but the last separation
        /// </summary>
        /// <param name="line">the line to split and return.  In this case it's the filename, url lines in urls.txt</param>
        /// <param name="separationCharacter">the separation character</param>
        /// <returns></returns>
        internal static string GetCompareAgainst(string line, char separationCharacter = ',')
        {
            string compareAgainst = "";
            string[] lineSplit = line.Split(separationCharacter);
            for (int i = 0; i < lineSplit.Length - 1; i++)
            {
                if (i > 0)
                {
                    compareAgainst += separationCharacter;
                }
                compareAgainst += lineSplit[i];
            }
            return compareAgainst;
        }
        #endregion
        #region StringArrayMaker
        /// <summary>
        /// Used for making a string array in C# from a long string of comma separated items.  Outputs the resultant string array e.g. {"item1", "item2", ...} to outFilePath
        /// </summary>
        /// <param name="items">a string of items that you want to make into a c# array, separated by a comma</param>
        /// <param name="toSurroundItemsWith">The character to surround each item with</param>
        /// <param name="separator">the character that separates the items in 'items'</param>
        /// <param name="outFilePath">Path to the resultant file</param>
        public static void StringArrayMaker(string items, string toSurroundItemsWith, char separator, string outFilePath, string filename = "StringArray.txt")
        {
            string filePath = Path.Combine(outFilePath, filename);
            if (File.Exists(filePath))
            {
                File.AppendAllText(filePath, "\n");
            }
            if (toSurroundItemsWith.Length > 1)
            {
                System.Windows.Forms.MessageBox.Show("The surrounder must be one character long.", "Error using StringArrayMaker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            toSurroundItemsWith = toSurroundItemsWith.Trim();
            string[] itemsSplit = items.Split(separator);
            StringBuilder stringBuilderResult = new StringBuilder();
            stringBuilderResult.Append("public static string[] myStringArray = { ");
            foreach (string item in itemsSplit)
            {
                stringBuilderResult.Append(toSurroundItemsWith + item.Trim() + toSurroundItemsWith + ", ");
            }
            stringBuilderResult.Append(" };");
            File.AppendAllText(Path.Combine(outFilePath, filename), stringBuilderResult.ToString().Trim());
        }
        #endregion
        #endregion
        #endregion
    #region Enums
        public enum AudioFileExtensionsEnum
    {
        mp3,
        flac,
        mp4,
        wav,
        cda,
        mid,
        midi,
        mpa,
        wpl,
        ogg,
        aif
    }
        public enum TopLevelDomainsEnum
    {
        com, org, net, edu, gov, mil, arpa, ac, ae, af, ag, ai, al, am, ao, aq, ar, at, au, aw, ax, az, ba, bb, bd, be, bf, bg, bh, bi, bj, bm, bn, bo, br, bs, bt, bw, by, bz, ca, cc, cd, cf, cg, ch, ci, ck, cl, cm, cn, co, cr, cu, cv, cw, cx, cy, cz, de, dj, dk, dm, dz, ec, ee, eg, er, es, et, eu, fi, fj, fk, fm, fo, fr, ga, gd, ge, gf, gg, gh, gi, gl, gm, gn, gp, gq, gr, gs, gt, gu, gw, gy, hk, hm, hn, hr, ht, hu, id, ie, il, im, io, iq, ir, it, je, jm, jo, jp, ke, kg, kh, ki, km, kn, kp, kr, kw, ky, kz, la, lb, lc, li, lk, lr, ls, lt, lu, lv, ly, ma, mc, md, me, mg, mh, mk, ml, mm, mn, mo, mp, mq, mr, ms, mt, mu, mv, mw, mx, my, mz, na, nc, ne, nf, ng, ni, nl, no, nr, nu, nz, om, pa, pe, pf, pg, ph, pk, pl, pm, pn, pr, ps, pt, pw, py, qa, re, ro, rs, ru, rw, sa, sb, sc, sd, se, sg, sh, si, sk, sl, sm, sn, so, sr, ss, st, su, sv, sx, sy, sz, tc, td, tf, tg, th, tj, tk, tl, tm, tn, to, tr, tt, tv, tw, tz, ua, ug, uk, us, uy, uz, va, vc, ve, vg, vi, vn, vu, wf, ws, ye, yt, za, zm, zw
    }
    #endregion
}
    //todo: can delete if can figure out maxrecursion and min recursion problem
    #region UrlObject Class
    public class UrlObject
    {
        public string Url;
        public int RecursionDepth;
        public UrlObject(string url, int recursionDepth)
        {
            Url = url;
            RecursionDepth = recursionDepth;
        }
    }
    #endregion
}


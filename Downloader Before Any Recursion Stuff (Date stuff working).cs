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
using System.Windows;

namespace Downloader 
{
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
        #endregion

        #region Private Fields
        /// <summary>
        /// hashset of urls with possible files to download based on extension and recursion properties
        /// </summary>
        private readonly HashSet<string> UrlsToDownload = new HashSet<string>();
        /// <summary>
        /// hashset of tuples representing a url and whether it is a file or not
        /// </summary>
        private readonly HashSet<string> UrlsToRecurse = new HashSet<string>();
        private readonly Queue<string> RecurseUrlsQueue = new Queue<string>();
        private readonly Queue<string> DownloadQueue = new Queue<string>();
        /// <summary>
        /// The regex used to find pages that have files to download (different sites use different naming schemas).  Use a literal string e.g. @" "
        /// </summary>
        public static readonly Stopwatch Stopwatch = new Stopwatch();
        private bool IsFirstTimeDownloading = true;
        private bool IsFile = false;
        private string FileExtension;
        private string DirectoryPath;
        private bool StartedRecursion = false;
        private string DatePath;
        internal bool PrefixFileName = true;
        private int CurrentRecursionDepth = -1;
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
        /// <summary>
        /// Whether to attach a prefix to batch downloaded files
        /// </summary>
 
        #region Domain and File Extensions
        public static string[] FileExtensions = { ".mp3", ".flac", ".mp4", ".wav", ".cda", ".mid", ".midi", ".mpa", ".wpl", ".ogg", ".aif" };

        public static string[] TopLevelDomains = { ".com", ".org", ".net", ".edu", ".gov", ".mil", ".arpa", ".ac", ".ae", ".af", ".ag", ".ai", ".al", ".am", ".ao", ".aq", ".ar", ".at", ".au", ".aw", ".ax", ".az", ".ba", ".bb", ".bd", ".be", ".bf", ".bg", ".bh", ".bi", ".bj", ".bm", ".bn", ".bo", ".br", ".bs", ".bt", ".bw", ".by", ".bz", ".ca", ".cc", ".cd", ".cf", ".cg", ".ch", ".ci", ".ck", ".cl", ".cm", ".cn", ".co", ".cr", ".cu", ".cv", ".cw", ".cx", ".cy", ".cz", ".de", ".dj", ".dk", ".dm", ".dz", ".ec", ".ee", ".eg", ".er", ".es", ".et", ".eu", ".fi", ".fj", ".fk", ".fm", ".fo", ".fr", ".ga", ".gd", ".ge", ".gf", ".gg", ".gh", ".gi", ".gl", ".gm", ".gn", ".gp", ".gq", ".gr", ".gs", ".gt", ".gu", ".gw", ".gy", ".hk", ".hm", ".hn", ".hr", ".ht", ".hu", ".id", ".ie", ".il", ".im", ".io", ".iq", ".ir", ".it", ".je", ".jm", ".jo", ".jp", ".ke", ".kg", ".kh", ".ki", ".km", ".kn", ".kp", ".kr", ".kw", ".ky", ".kz", ".la", ".lb", ".lc", ".li", ".lk", ".lr", ".ls", ".lt", ".lu", ".lv", ".ly", ".ma", ".mc", ".md", ".me", ".mg", ".mh", ".mk", ".ml", ".mm", ".mn", ".mo", ".mp", ".mq", ".mr", ".ms", ".mt", ".mu", ".mv", ".mw", ".mx", ".my", ".mz", ".na", ".nc", ".ne", ".nf", ".ng", ".ni", ".nl", ".no", ".nr", ".nu", ".nz", ".om", ".pa", ".pe", ".pf", ".pg", ".ph", ".pk", ".pl", ".pm", ".pn", ".pr", ".ps", ".pt", ".pw", ".py", ".qa", ".re", ".ro", ".rs", ".ru", ".rw", ".sa", ".sb", ".sc", ".sd", ".se", ".sg", ".sh", ".si", ".sk", ".sl", ".sm", ".sn", ".so", ".sr", ".ss", ".st", ".su", ".sv", ".sx", ".sy", ".sz", ".tc", ".td", ".tf", ".tg", ".th", ".tj", ".tk", ".tl", ".tm", ".tn", ".to", ".tr", ".tt", ".tv", ".tw", ".tz", ".ua", ".ug", ".uk", ".us", ".uy", ".uz", ".va", ".vc", ".ve", ".vg", ".vi", ".vn", ".vu", ".wf", ".ws", ".ye", ".yt", ".za", ".zm", ".zw" };
        #endregion

        #endregion

        #region Public(Bound) Properties
        /// <summary>
        ///
        ///Sets the regex pattern used to find more Urls to pages that may contain files.  This must be a c# regular expression where the first group matches what you are looking for example @"href=""(/OCR\d{4})""".    Study the html of the website to formulate the regex that will match the pages you are looking for.  The default is "", which means that the algorithm will only look for direct file URls for the URL given;
        /// </summary>
        private string _RegularExpressionForUrls = REGEX_DEFAULT_URLS;
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
        private string _downloadSpeed;
        public string DownloadSpeed
        {
            get { return _downloadSpeed; }
            set
            {
                if (_downloadSpeed != value)
                {
                    _downloadSpeed = value;
                    NotifyPropertyChanged("DownloadSpeed");
                }
            }
        }
        /// <summary>
        /// This determines whether multiple files are downloaded at one.  Default is true, but set to false if getting a lot of download errors as this will slow down download rate and probably prevent the server from sending errors.
        /// </summary>
        private bool _DownloadAsync = true;
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
        /// <summary>
        /// If true, will only download files based on the last time files were downloaded from the site.  This info is stored in a file in the download folder/>
        /// </summary>
        private bool _OnlyDownloadSinceLastDownloadDate = false;

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

        #region WPF Implementions

        #region Needed to implement IDataErrorInfo
        public string Error => null;    //returns null
        public string this[string propertyName]       //the name of the property for the current object
        {
            get
            {
                string retvalue = null;
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
                        retvalue = string.Format("Please enter a valid directory to which to save the file(s).");    //define an appropriate messag
                    }
                    else if (pathExists == false)
                    {
                        retvalue = string.Format("Please enter a valid directory.\n'{0}' doesn't exist", this.DestinationOfFile);    //define an appropriate messag
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
        public void StartDownload()
        {
            //todo: bug when downloading two urls in a row without restarting the program (need to reset values when completely done with a batch?)
            //todo: add the option to only download files after a certain date (set cutoff default to last download date for that folder?)
            //todo: add field to input own regex on Window
            //todo: add menu with options
            //Program can find different media files;  these extensions determine which extensions to look for in ProcessURL
            SetCurrentExtension();

            SetupEnvironment();  

            #region Case: File (Downloads file and catches errors)
            if (Url.StartsWith("http") && IsFile == true)
            {
                //Initializing
                string message = null;

                try
                {
                    Task downloadFile = Task.Factory.StartNew(() => {
                        DownLoadFileAsync(Url);
                    });
                }
                #region Exception Handling
                catch (ArgumentNullException)
                {
                    message = string.Format("Error Downloading '{0}'.\nEither the URL or the file destination are not correct.  Please check both.", Url);
                    throw new Exception(message);
                }
                catch (WebException)
                {
                    message = string.Format("Error Downloading '{0}'.\nThe website didn't respond.  Please check the URL or try again later.", Url);
                    throw new Exception(message);
                }
                catch (InvalidOperationException)
                {
                    message = string.Format("Error Downloading '{0}'.\nInvalid Operation", Url);
                    throw new Exception(message);
                }
                catch (UriFormatException)
                {
                    message = string.Format("'{0}' is not a valid URL.Please try again.\n", Url);
                    throw new Exception(message);
                }
                #endregion
            }
            #endregion

            #region Case: Website
            else
            {
                StartBatchDownload();
            }
            #endregion
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
                try
                {
                    GetDestinationWithGUI();
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            #endregion

            #region Gets the destination, et. al when using Destinationtextbox
            else
            {
                //todo: need to get these variables sorted right when using the destinationbox
                DirectoryPath = DestinationOfFile;
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

            #region Writing the Main URL to url.txt
            if (!IsFile)
            {
                using (StreamWriter sw = File.CreateText(Path.Combine(DirectoryPath, "urls.txt")))
                {
                    sw.WriteLine(string.Format("website, {0}", Url));
                }
            }

            #endregion
        }
        #endregion

        #region StartBatchDownload
        private void StartBatchDownload()
        {
            //MessageBox.Show(string.Format("StartBatchDownload Task.CurrentId: {0}", Task.CurrentId));

            //Start the process of finding urls and continue to find them
            Task GetUrlsTask = Task.Factory.StartNew(() =>
            {
                GetUrlsInSourceCode(Url);
            });

            //start downloading once the first file is put into DownloadQueue
            Task DownloadFilesLoopTask = Task.Factory.StartNew(() =>
            {
                DownloadFilesLoop();
                //GetUrlsTask.Wait();
                string.Format("UrlsToRecurse: {0}\nUrlsToDownload: {1}", UrlsToRecurse, UrlsToDownload);
                ResetMembers();
            });
        }
        #endregion

        #region GetUrlsInSourceCode
        private void GetUrlsInSourceCode(string url)
        {
            CurrentRecursionDepth++;
            MessageBox.Show(string.Format("CurrentRecursionDepth out of loop: {0}\nMaxRecursionDepth  out of loop: {1}\nUrl: {2}", CurrentRecursionDepth, MaxRecursionDepth, url));
            if (CurrentRecursionDepth <= MaxRecursionDepth)
            {
                //Start a separate thread to get website urls in SourceCode only if the user specified a regex for the intermediate pages
                string sourceCode = GetURLSourceCode(url);
                if (sourceCode != null)
                {
                    if (REGEX_DEFAULT_URLS != RegularExpressionForUrls)
                    {
                        //Task GetNextUrlMatchesTask = Task.Factory.StartNew(() =>
                        //{
                            GetNextUrlMatches(sourceCode);
                        //});
                    }
                    //Get file urls
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
            if (MenuItem_Download_By_Date_Flag)
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

            //Iterates through all matches to determine if they are likely a website or file
            for (int i = 0; i < nextUrlMatchesList.Count; i++)
            {
                if (MenuItem_Download_By_Date_Flag)
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
                    MessageBox.Show(string.Format("CurrentRecursionDepth in loop: {0}\nURL subsection:", CurrentRecursionDepth, urlSubSection));

                    //MessageBox.Show(string.Format("urlSubSection {0}\nnextUrlMatches.Count: {1}\nurlDatesCombined.Count: {2}", urlSubSection, nextUrlMatches.Count, urlDatesCombined.Count));
                    //Gambling that links to downloads will specify a full url with http...
                    if (urlSubSection.Trim().Contains("http"))
                    {
                        //probably a file to download
                        lock (UrlsToDownload)
                        {
                            UrlsToDownload.Add(urlSubSection);
                        }
                        //MessageBox.Show(string.Format("Download: \nurlSubSection: {0}", urlSubSection));
                    }
                    else
                    {
                        //probably a another page
                        lock (UrlsToRecurse)
                        {
                            UrlsToRecurse.Add(GetBaseUrl(Url) + urlSubSection);
                        }
                        //MessageBox.Show(string.Format("Website: \n Url: {0}", GetBaseUrl(Url) + urlSubSection));
                    }
                }
            }
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
                    //Gambling that links to downloads will specify a full url with http...
                    if (urlSubSection.Trim().Contains("http"))
                    {
                        //probably a file to download
                        //MessageBox.Show(string.Format("urlSubSection: {0}", urlSubSection));
                        lock (UrlsToDownload)
                        {
                            UrlsToDownload.Add(urlSubSection);
                        }
                    }
                    else
                    {
                        //probably a another page
                        lock (UrlsToRecurse)
                        {
                        UrlsToRecurse.Add(GetBaseUrl(Url) + urlSubSection);
                        }
                    }
                }
            }
            #endregion

            #region Populating DownloadQueue
            if (UrlsToDownload.Count > 0)
            {
                //add UrlsToDownload to a queue
                foreach (var item in UrlsToDownload)
                {
                    lock (DownloadQueue)
                    {
                        DownloadQueue.Enqueue(item);
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
                
                    foreach (var item in UrlsToRecurse)
                {
                    lock (RecurseUrlsQueue)
                    {
                        RecurseUrlsQueue.Enqueue(item);
                    }
                }
                lock (UrlsToRecurse)
                {
                    UrlsToRecurse.Clear();
                }
                #endregion

                #region while there is still something in the queue check the next one and remove it
                while (RecurseUrlsQueue.Count > 0)
                {
                    string nextURL;
                    lock (RecurseUrlsQueue)
                    {
                        nextURL = RecurseUrlsQueue.Dequeue();
                    }
                    GetUrlsInSourceCode(nextURL);
                    StartedRecursion = true;
                }
                #endregion
            }
            #endregion
        }
        #endregion

        #region DownloadFilesLoop
        private void DownloadFilesLoop()
        {
            //MessageBox.Show(string.Format("DownloadFilesLoop Started"));
            while (UrlsToRecurse.Count > 0 || UrlsToDownload.Count > 0 || DownloadQueue.Count > 0 | RecurseUrlsQueue.Count > 0 || StartedRecursion == false)
            {
                //download files in queue
                while (DownloadQueue.Count > 0)
                {
                    string nextURL;
                    //MessageBox.Show(string.Format("nextDownload file: {0}", nextDownload));
                    lock (DownloadQueue)
                    {
                        nextURL = DownloadQueue.Dequeue();
                    }
                    DownLoadFileAsync(nextURL, true);
                }
            }
            //MessageBox.Show(string.Format("DownloadFilesLoop Ended"));
        }
        #endregion

        #endregion

        #region DownLoadFileAsync
        private Task<string>  DownLoadFileAsync(string url, bool recursive = false)
        {
            #region Sets DestinationOfFiles for recursive calls only
            if (recursive)
            {
                GetDestinationOfBatchFiles(url);
            }
            #endregion

            //taskCompletionSource is needed for exception handling
            var taskCompletionSource = new TaskCompletionSource<string>();
            using (WebClient client = new WebClient())
            {
                if (!File.Exists(DestinationOfFile))
                {
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

                    #region Handling the DownloadFileCompleted Event
                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Error != null)
                        {
                            taskCompletionSource.TrySetException(e.Error);
                        }
                        else if (e.Cancelled)
                        {
                            taskCompletionSource.TrySetCanceled();
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
                    var bytesToMBFactor = Math.Pow(2, 20);
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        //Setting speed
                        DownloadSpeed = string.Format("{0:##.##}", (e.BytesReceived / bytesToMBFactor / Downloader.Stopwatch.Elapsed.TotalSeconds).ToString("0.00"));

                        //Setting amount to download
                        TotalDownloadSize = string.Format("{0:##.##}", e.TotalBytesToReceive / bytesToMBFactor);

                        //Setting total received
                        TotalDownloadReceived = string.Format("{0:##.##}", e.BytesReceived / bytesToMBFactor);

                        //MessageBox.Show(string.Format("e.ProgressPercentage: {0}", e.ProgressPercentage));

                       // MessageBox.Show(string.Format("Downloadfile Task.CurrentId: {0}", Task.CurrentId));
                        //Setting progress bar value
                        ProgressValue= e.ProgressPercentage;
                    };
                    #endregion

                    #region Starting the stopwatch for Speed Calculation
                    Uri uri = new Uri(url);
                    Stopwatch.Start();
                    #endregion

                    #region  Actually Downloading the File
                    //todo: DownloadFileAsync generates events but DownloadFile doesn't
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
                #region Write the download file urls to file
                if (!IsFile) 
                { 
                    File.AppendAllText(Path.Combine(DirectoryPath, "urls.txt"), string.Format("{0}, {1}\n", FileName, url)); 
                }
                #endregion

                return taskCompletionSource.Task;
            }
        }
        #endregion

        #region ResetMembers
        private void ResetMembers()
        {
            WriteDate();
            MessageBox.Show(string.Format("ResetMembers Reached"));
            IsFirstTimeDownloading = true;
            IsFile = false;
            FileExtension = "";
            DirectoryPath = "";
            StartedRecursion = false;
            FileName = "";
            Url = "";
            DestinationOfFile = "";
            ProgressValue = 0;
            TotalDownloadReceived = "";
            TotalDownloadSize = "";
            DownloadSpeed = "";
            DatePath = "";
            CurrentRecursionDepth = 0;
            Recursion = true;
            //PrefixFileName = true;
            //RegularExpressionForUrls = REGEX_DEFAULT_URLS;
            //DownloadAsync = true;
            //OnlyDownloadSinceLastDownloadDate = false;
            //DateRegex
    }
    #endregion

    #endregion

    #region Helper Methods

    //todo: event that occurs every pre-defined amount of time?

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

            //writing the source to file in same directory as .exe
            //using (StreamWriter sw = File.AppendText("htmlSource.txt"))
            //{
            //    sw.Write(html);
            //}
        }
        #endregion

        #region ReplaceCharactersInFileName
        private string ReplaceCharactersInFileName(string filename)
        {
            string res = filename;
            string[] toRemove = { @"_", @"%", @"-", @"\." };
            string[] toReplaceWith = { " ", " ", " ", " - " };

            for (int i = 0; i < toRemove.Length; i++)
            {
                res = Regex.Replace(res, toRemove[i], toReplaceWith[i]);
            }
            return res;
        }
        #endregion

        #region GetDestinationWithGUI
        /// <summary>
        /// propmps user for a directory to save file
        /// </summary>
        /// <returns>string of path to save files.  returns null if user cancels</returns>
        /// <exception cref="TaskCanceledException"></exception>
        public void GetDestinationWithGUI()
        {
            #region Initializing 
            string fileNameFromURI = Path.GetFileNameWithoutExtension(Url).Trim();
            fileNameFromURI = ReplaceCharactersInFileName(fileNameFromURI);
            fileNameFromURI = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileNameFromURI);
            string extension = Path.GetExtension(Url.Trim());
            #endregion

            #region Getting IsFile
            //MessageBox.Show(string.Format("extension: {0}\n is empty: {1}", extension, string.IsNullOrEmpty(extension)));
            bool containsTLD = TopLevelDomains.Any(domain => extension.ToUpper().Trim() == domain.ToUpper().Trim());
            if (!(string.IsNullOrEmpty(extension) || containsTLD))
            {
                MessageBox.Show(string.Format("This is a file"));
                IsFile = true;
            }
            #endregion

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
                    //MessageBox.Show(string.Format("Directory.GetParent(DestinationOfFile).ToString(): {0}", Directory.GetParent(DestinationOfFile).ToString()));
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
                    folderBrowserDialog.Description = "Select_A_Folder to Save the File in";
                    folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        DirectoryPath = folderBrowserDialog.SelectedPath;
                    }
                }
            }
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
            if (File.Exists(DatePath))
            {
                DateTime lastDownloadDate = DateTime.Parse(File.ReadAllText(DatePath));
                DateTime urlDateTime = new DateTime();
                if (DateTime.TryParse(urlDate, out urlDateTime))
                {
                    if (lastDownloadDate.Date >= urlDateTime)
                    {
                        Continue = false;
                    }
                }
               //MessageBox.Show(string.Format("lastDownloadDate.Date: {0}\nurlDateTime: {1}", lastDownloadDate.Date, urlDateTime));
            }

            //MessageBox.Show(string.Format("Continue: {0}", Continue));
            return Continue;
        }
        #endregion

        #region SetCurrentExtension
        private void SetCurrentExtension()
        {
            string extension = Path.GetExtension(Url);
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

    public class UrlObject
    {
        public string Url;
        public int RecursionDepth;
        public UrlObject()
        {
            Url = url;
            RecursionDepth = recursionDepth;
        }
        public void Add(string url, int recursionDepth)
        {
            
        }
    }
}


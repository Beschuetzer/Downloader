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
using Microsoft.Win32;
using System.Windows;
using System.Linq;

namespace Downloader3
{
    public class Downloader : IDataErrorInfo, INotifyPropertyChanged
    {
        #region Private Constants
        private const float DOWNLOAD_WAIT_INTERVAL = 2.0f;
        private const string REGEX_DEFAULT = @"href=""(.+\{0})""";
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

        #endregion

        #region Private Properties
        private bool IsFile { get; set; }
        private string UrlExtension { get; set; }
        private string DirectoryName { get; set; }
        private string FileExtension { get; set; }
        private bool IsFirstTimeDownloading { get; set; }
        private bool NewFolderCreated { get; set; }
        private string DirectoryPath { get; set; }
        private string ParentDirectory { get; set; }

        #endregion

        #region Bound Properties
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
        #endregion

        #region Public Fields
        /// <summary>
        ///sets the regex pattern used to find links to files or files themselves.  study the html of the website to formulate the regex that will match what you are looking to download
        /// </summary>
        public string RegularExpression = REGEX_DEFAULT;
        /// <summary>
        ///The amount of time between downloads.  Default is 2.  Used to prevent the server from sending a 404 code back.  try larger values if you are getting server response codes and lower values if you aren't
        /// </summary>
        public float SecondsBetweenDownloads = DOWNLOAD_WAIT_INTERVAL;
        /// <summary>
        /// Whether or not to go to each link that matches the RegularExpression and download files from there.  Default is true.
        /// </summary>
        public bool Recursion = true;
        /// <summary>
        /// The prefix of the filename(s) if PrefixFileName is true.
        /// </summary>
        public bool PrefixFileName = true;
        public string FileNamePrefix = FILENAME_PREFIX_DEFAULT;
        public string FileNamePrefixSeparator = FILENAME_PREFIX_SEPARATOR_DEFAULT;
        public HashSet<string> CurrentExtensions = new HashSet<string>();
        public static readonly string UrlRegex = @"^\s*https?://.+\{0}/*(.+)*$";

        #region Domain and File Extensions
        public static string[] FileExtensions = { ".mp3", ".flac", ".mp4", ".wav", ".cda", ".mid", ".midi", ".mpa", ".wpl", ".ogg", ".aif" };

        public static string[] TopLevelDomains = { ".com", ".org", ".net", ".edu", ".gov", ".mil", ".arpa", ".ac", ".ae", ".af", ".ag", ".ai", ".al", ".am", ".ao", ".aq", ".ar", ".at", ".au", ".aw", ".ax", ".az", ".ba", ".bb", ".bd", ".be", ".bf", ".bg", ".bh", ".bi", ".bj", ".bm", ".bn", ".bo", ".br", ".bs", ".bt", ".bw", ".by", ".bz", ".ca", ".cc", ".cd", ".cf", ".cg", ".ch", ".ci", ".ck", ".cl", ".cm", ".cn", ".co", ".cr", ".cu", ".cv", ".cw", ".cx", ".cy", ".cz", ".de", ".dj", ".dk", ".dm", ".dz", ".ec", ".ee", ".eg", ".er", ".es", ".et", ".eu", ".fi", ".fj", ".fk", ".fm", ".fo", ".fr", ".ga", ".gd", ".ge", ".gf", ".gg", ".gh", ".gi", ".gl", ".gm", ".gn", ".gp", ".gq", ".gr", ".gs", ".gt", ".gu", ".gw", ".gy", ".hk", ".hm", ".hn", ".hr", ".ht", ".hu", ".id", ".ie", ".il", ".im", ".io", ".iq", ".ir", ".it", ".je", ".jm", ".jo", ".jp", ".ke", ".kg", ".kh", ".ki", ".km", ".kn", ".kp", ".kr", ".kw", ".ky", ".kz", ".la", ".lb", ".lc", ".li", ".lk", ".lr", ".ls", ".lt", ".lu", ".lv", ".ly", ".ma", ".mc", ".md", ".me", ".mg", ".mh", ".mk", ".ml", ".mm", ".mn", ".mo", ".mp", ".mq", ".mr", ".ms", ".mt", ".mu", ".mv", ".mw", ".mx", ".my", ".mz", ".na", ".nc", ".ne", ".nf", ".ng", ".ni", ".nl", ".no", ".nr", ".nu", ".nz", ".om", ".pa", ".pe", ".pf", ".pg", ".ph", ".pk", ".pl", ".pm", ".pn", ".pr", ".ps", ".pt", ".pw", ".py", ".qa", ".re", ".ro", ".rs", ".ru", ".rw", ".sa", ".sb", ".sc", ".sd", ".se", ".sg", ".sh", ".si", ".sk", ".sl", ".sm", ".sn", ".so", ".sr", ".ss", ".st", ".su", ".sv", ".sx", ".sy", ".sz", ".tc", ".td", ".tf", ".tg", ".th", ".tj", ".tk", ".tl", ".tm", ".tn", ".to", ".tr", ".tt", ".tv", ".tw", ".tz", ".ua", ".ug", ".uk", ".us", ".uy", ".uz", ".va", ".vc", ".ve", ".vg", ".vi", ".vn", ".vu", ".wf", ".ws", ".ye", ".yt", ".za", ".zm", ".zw" };
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Don't use this constructor.  It is for private use.
        /// </summary>
        private Downloader(string url, string destination)
        {
            //Intializing Members
            DestinationOfFile = destination.Trim();
            Url = url.Trim();
            UrlExtension = Path.GetExtension(Url);
            IsFile = GetIsFile();
            NewFolderCreated = false;
            IsFirstTimeDownloading = true;
        }
        /// <summary>
        /// Asks for destination with GUI, sets extension to default .mp3, and set recursion to true
        /// </summary>
        /// <param name="url">url containing links to files or other urls with files</param>
        /// <param name="destination">where to save files locally.  If left blank, a SaveFileDialog box will ask for location</param>

        public Downloader(string url, string destination = DESTINATION_DEFAULT, string extension = EXTENSION_DEFAULT) : this(url, destination)
        {
            //MessageBox.Show("2");
            CurrentExtensions.Add(extension);
            //MessageBox.Show(string.Format("Extension added: {0}", extension));
        }

        /// <summary>
        /// Asks for destination with GUI, sets extension to default .mp3, and set recursion to true
        /// </summary>
        /// <param name="url">url containing links to files or other urls with files</param>
        /// <param name="destination">where to save files locally.  If left blank, a SaveFileDialog box will ask for location</param>      
        public Downloader(string url, string[] extensions, string destination = DESTINATION_DEFAULT) : this(url, destination)
        {
            //MessageBox.Show("3  ");
            foreach (var extension in extensions)
            {
                CurrentExtensions.Add(extension);
            }
        }

        #endregion

        #region Methods

        public void StartDownloadRework()
        {
            //1st 
        }

        #region MainLogic Methods

        #region StartDownload
        public void StartDownload()
        {
            //Program can find different media files;  these extensions determine which extensions to look for in ProcessURL
            SetCurrentExtension();

            #region Gets the destination for the files if the user didn't specify one
            if (!Directory.Exists(DestinationOfFile) || string.IsNullOrEmpty(DestinationOfFile))
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
                FileName = Path.GetFileNameWithoutExtension(DestinationOfFile).Trim();
                FileExtension = Path.GetExtension(DestinationOfFile).Trim();
                DirectoryName = Path.GetFileNameWithoutExtension(DirectoryPath);
            }
            #endregion

            //todo: maybe add a field to start download at a certain file?

            #region Gets the Prefix for recursive Downloads.  If not recursive the file is just named whatever is given in the SaveDialogbox
            if (!IsFile)
            {
                if (PrefixFileName == true && FileNamePrefix == FILENAME_PREFIX_DEFAULT)
                {
                    FileNamePrefix = Path.GetFileNameWithoutExtension(DestinationOfFile);
                }
            }
            #endregion

            #region Create a folder with the value of FileName if the FileName and DirectoryName are the same
            if (DirectoryName.ToUpper() != FileName.ToUpper() && !IsFile)
            {
                Directory.CreateDirectory(DestinationOfFile);
                NewFolderCreated = true;
            }
            #endregion

            #region Writing the Main URL to urls.txt
            string PathToDirectory;
            if (Path.GetFileName(DirectoryPath).ToUpper() == FileName.ToUpper()) { PathToDirectory = DirectoryPath; }
            else { PathToDirectory = Path.Combine(DirectoryPath, FileName); }
            File.WriteAllText(Path.Combine(PathToDirectory, "urls.txt"), string.Format("website, {0}\n", Url));
            #endregion

            #region Case: File (Downloads file and catches errors)
            if (Url.StartsWith("http") && IsFile == true)
            {
                //Initializing
                string message = null;

                try
                {
                    //todo: create a separate thread here
                    Task.Factory.StartNew(() =>
                    {
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
                Task download = Task.Factory.StartNew(() => DownloadFromWebsite(Url));
            }
            #endregion     
        }
        #endregion

        #region DownLoadFileAsync
        private Task<string> DownLoadFileAsync(string url, bool recursive = false)
        {

            #region Sets DestinationOfFiles for recursive calls only
            if (recursive)
            {
                GetDestinationOfFiles(url);
            }
            #endregion

            //Actual downloading
            var taskCompletionSource = new TaskCompletionSource<string>();
            using (WebClient client = new WebClient())
            {
                if (!File.Exists(DestinationOfFile))
                {
                    #region flag to only pause if the file exists
                    if (IsFirstTimeDownloading)
                    {
                        IsFirstTimeDownloading = false;
                    }
                    else
                    {
                        //todo: need to figure out why some files are being downloaded
                        //MessageBox.Show(string.Format("Sleeping: {0} ms", (int)(SecondsBetweenDownloads * 1000f)));
                        Thread.Sleep((int)(SecondsBetweenDownloads * 1000f));
                    }
                    #endregion

                    #region Handling the DownloadFileCompleted Event
                    client.DownloadFileCompleted += (s, e) =>
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
                            //MessageBox.Show(string.Format("Download of '{0}{1}' is complete.", FileName, FileExtension));
                        }
                    };
                    #endregion

                    #region Handling the DownloadProgressChanged Event
                    var bytesToMBFactor = Math.Pow(2, 20);
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        //Setting speed
                        DownloadSpeed = string.Format("{0:##.##}", (e.BytesReceived / bytesToMBFactor / Downloader.Stopwatch.Elapsed.TotalMilliseconds / 1000).ToString("0.00"));

                        //Setting amount to download
                        TotalDownloadSize = string.Format("{0:##.##}", e.TotalBytesToReceive / bytesToMBFactor);

                        //Setting total received
                        TotalDownloadReceived = string.Format("{0:##.##}", e.BytesReceived / bytesToMBFactor);

                        //Setting progress bar value
                        ProgressValue = e.ProgressPercentage;
                    };
                    #endregion

                    #region Starting the stopwatch for Speed Calculation
                    Uri uri = new Uri(url);
                    Stopwatch.Start();
                    #endregion

                    //Actually Downloading the File
                    client.DownloadFileAsync(uri, DestinationOfFile);
                }
                //Write the download file urls to file
                File.AppendAllText(Path.Combine(Directory.GetParent(DestinationOfFile).ToString(), "urls.txt"), string.Format("{0}, {1}\n", FileName, url));
                return taskCompletionSource.Task;
            }
            #region Books Way to implement Download
            //// Create the stream and request objects.
            //FileStream localFileStream = new FileStream(DestinationOfFile, FileMode.OpenOrCreate);
            //HttpWebRequest httpWebRequest = WebRequest.CreateHttp(url);

            //// Configure the HTTP request.
            //httpWebRequest.Method = WebRequestMethods.File.DownloadFile;        //not sure about this method

            //// Configure the response to the request.
            //WebResponse httpWebResponse = httpWebRequest.GetResponse();     //get a response from the request
            //Stream httpResponseStream = httpWebResponse.GetResponseStream();    //get a stream from the response
            //byte[] buffer = new byte[1024];     //how many bytes transfered at a time?

            //// Process the response by downloading data.
            //int bytesRead = httpResponseStream.Read(buffer, 0, 1024);    //keeping track of bytes read
            //while (bytesRead > 0)
            //{
            //    localFileStream.Write(buffer, 0, bytesRead);
            //    bytesRead = httpResponseStream.Read(buffer, 0, 1024);
            //}

            //// Close the streams.
            //localFileStream.Close();
            //httpResponseStream.Close();
            #endregion
        }
        #endregion

        #region GetDestinationOfFiles
        private void GetDestinationOfFiles(string url)
        {
            //todo: bug when downloading two urls in a row without restarting the program (need to reset values when completely done with a batch?)
            FileName = ReplaceCharactersInFileName(Path.GetFileNameWithoutExtension(url));
            FileExtension = Path.GetExtension(url);

            //Making Sure files get to the new folder
            if (NewFolderCreated)
            {
                if (IsFirstTimeDownloading)
                {
                    DestinationOfFile = Path.Combine(DestinationOfFile, FileName + FileExtension);
                }
                else
                {
                    DestinationOfFile = Path.Combine(Directory.GetParent(DestinationOfFile).ToString(), FileName + FileExtension);
                }
            }
            else
            {
                DestinationOfFile = Path.Combine(DirectoryPath, FileName + FileExtension);
            }

            //Adding Prefix
            if (PrefixFileName)
            {
                if (NewFolderCreated)
                {
                    DirectoryPath = Directory.GetParent(DestinationOfFile).ToString();
                }
                DestinationOfFile = string.Format("{0}\\{1} - {2}", DirectoryPath, FileNamePrefix, FileName + FileExtension);
            }
            //NewFolderCreated = false;
        }
        #endregion

        #region DownloadFromWebsite
        /// <summary>
        /// Downloads the file specified by the requestUriString
        /// </summary>
        /// <param name="requestUriString">url of the site or file</param>
        /// <param name="destination">location on local disk</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="UriFormatException"></exception>
        private void DownloadFromWebsite(string url)
        {
            //Get URLSourceCode and set this.SourceCode
            string sourceCode = GetURLSourceCode(url);

            //Process source code to find UrlsToDownload and UrlstoRecurse
            ProcessUrls(sourceCode);

            //Each new URL found
            if (UrlsToDownload.Count > 0)
            {
                PopulateDownloadQueue();
            }
            if (UrlsToRecurse.Count > 0)
            {
                PopulateRecurseUrlsQueue();
            }
        }
        #endregion

        #endregion

        #region Helper Methods

        #region ProcessUrls
        private void ProcessUrls(string sourceCode)
        {
            //iterates through each of the file extension types given to find links that match
            foreach (var extension in CurrentExtensions)
            {
                //Gets any matches based on extension
                //todo: need to do if clause for when regex is specified.  can't have string format with extension
                if (!RegularExpression.Contains("{0}"))
                {
                    //todo: add the default regex or make RegularExpression an array and iterate through it to include
                    RegularExpression = string.Format("{0}{1}{2}", RegularExpression, "|", REGEX_DEFAULT);
                }

                MatchCollection nextUrlMatches = GetMatches(string.Format(RegularExpression, extension), sourceCode);

                //Iterates through all matches to determine if they are likely a website or file

                foreach (Match match in nextUrlMatches)
                {
                    string urlSubSection = match.Groups[1].ToString();
                    //Gambling that links to downloads will specify a full url with http...
                    if (urlSubSection.Trim().Contains("http"))
                    {
                        //probably a file to download
                        UrlsToDownload.Add(urlSubSection);
                    }
                    else
                    {
                        //probably a another page
                        UrlsToRecurse.Add(GetBaseUrl(Url) + urlSubSection);
                    }
                }
            }
        }
        #endregion

        #region PopulateRecurseUrlsQueue
        private void PopulateRecurseUrlsQueue()
        {
            //add UrlstoRecurse to a queue
            foreach (var item in UrlsToRecurse)
            {
                RecurseUrlsQueue.Enqueue(item);
            }

            //clear for recursive calls
            UrlsToRecurse.Clear();

            //while there is still something in the queue check the next one and remove it
            while (RecurseUrlsQueue.Count > 0)
            {
                DownloadFromWebsite(RecurseUrlsQueue.Dequeue());
            }
        }
        #endregion

        #region PopulateDownloadQueue
        private void PopulateDownloadQueue()
        {
            //add UrlsToDownload to a queue
            foreach (var item in UrlsToDownload)
            {
                DownloadQueue.Enqueue(item);
            }

            //clearing for recursive calls
            UrlsToDownload.Clear();

            //todo: Timer that triggers event after pre-defined amount of time
            //todo: add option to download last item first?
            //System.Timers.Timer t = new System.Timers.Timer();
            //t.Interval = SecondsBetweenDownloads * 1000; // In milliseconds
            //t.AutoReset = false; // Stops it from repeating
            //t.Elapsed += new ElapsedEventHandler(TimerElapsed);
            //t.Start();

            //download files in queue
            while (DownloadQueue.Count > 0)
            {
                string nextDownload = DownloadQueue.Dequeue();
                DownLoadFileAsync(nextDownload, true);
            }
        }
        //todo: event that occurs every pre-defined amount of time
        //private void TimerElapsed(object sender, ElapsedEventArgs e)
        //{
        //    DownLoadFileAsync(DownloadQueue.Dequeue(), true);
        //}
        #endregion

        #region GetMatches
        /// <summary>
        /// Gets matches based on a regex
        /// </summary>
        /// <param name="regularExpression"></param>
        /// <param name="text"></param>
        /// <returns>MatchCollection of matches</returns>
        public static MatchCollection GetMatches(string regularExpression, string text)
        {
            Regex regex = new Regex(regularExpression);
            return regex.Matches(text);
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
                    MatchCollection domainMatch = GetMatches(string.Format(@"(http.+{0})/.*", domain), url);
                    return domainMatch[0].Groups[1].ToString();
                }
                //case when TLD is at end
                else if (url.EndsWith(string.Format("{0}", domain)))
                {
                    MatchCollection domainMatch = GetMatches(string.Format(@"(http.+{0})", domain), url);
                    return domainMatch[0].Groups[1].ToString();
                }
            }
            return null;
        }
        #endregion

        #region GetIsFile

        private bool GetIsFile()
        {
            foreach (var extension in FileExtensions)
            {
                if (!string.IsNullOrEmpty(UrlExtension))
                {
                    if (extension.ToUpper() == UrlExtension.ToUpper())
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            return false;
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
            string sourceCode;
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            using (StreamReader sr = new StreamReader(data))
            {
                sourceCode = sr.ReadToEnd();
            }
            return sourceCode;

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
            //Initializing the SaveFileDialog Box
            string fileNameFromURI = Path.GetFileNameWithoutExtension(Url).Trim();
            fileNameFromURI = ReplaceCharactersInFileName(fileNameFromURI);
            fileNameFromURI = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileNameFromURI);
            string extension = Path.GetExtension(Url.Trim());
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
            bool? dialogResult = saveFileDialog.ShowDialog();

            //if the user clicked saved
            if (dialogResult == true)
            {
                DestinationOfFile = saveFileDialog.FileName;
                DirectoryPath = Path.GetDirectoryName(DestinationOfFile);
                FileName = Path.GetFileNameWithoutExtension(DestinationOfFile).Trim();
                FileExtension = Path.GetExtension(DestinationOfFile).Trim();
                DirectoryName = Path.GetFileNameWithoutExtension(DirectoryPath);

                //MessageBox.Show(string.Format("Directory.GetParent(DestinationOfFile).ToString(): {0}", Directory.GetParent(DestinationOfFile).ToString()));
            }
            else if (dialogResult == false)
            {
                throw new TaskCanceledException("User Cancelled the Download.");
            }
            else
            {
                GetDestinationWithGUI();
            }
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

        #region RenameFilesBasedOnIDv3Tag
        /// <summary>
        /// renames all the files in a directory based on the IDv3 tag
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="regex"></param>
        /// <param name="replaceWith"></param>
        public void RenameFilesBasedOnIDv3Tag(string sourceFolder, string regex, string replaceWith)
        {
            //todo: idv3 renaming of files
            string[] files = Directory.GetFiles(ParentDirectory);
            foreach (var file in files)
            {

            }

        }
        #endregion

        #endregion

        #endregion

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
                        retvalue = string.Format("Please enter a valid, complete URL.\n'{0}' is either not valid or doesn't contain \n'http:...' and/or a top level domain (e.g. .com, .edu)", this.Url);    //define an appropriate messag
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
}


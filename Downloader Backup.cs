using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using Microsoft.Win32;
using System.Windows;

namespace Downloader_Backup
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
        private string FileName { get; set; }
        private string DirectoryName { get; set; }
        private string FileExtension { get; set; }
        private bool IsFirstTimeDownloading { get; set; }
        private bool NewFolderCreated { get; set; }
        private string _Url;
        public string Url
        {
            get { return _Url; }
            set
            {
                if (_Url != value)
                {
                    _Url = value;
                    NotifyPropertyChanged("Url");
                }
            }
        }
        private string _DestinationOfFile;
        public string DestinationOfFile
        {
            get { return _DestinationOfFile; }
            set
            {
                if (_DestinationOfFile != value)
                {
                    _DestinationOfFile = value;
                    NotifyPropertyChanged("DestinationOfFile");
                }
            }
        }
        private string DirectoryPath { get; set; }
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
        public static string[] AudioFileExtensions = { ".mp3", ".flac", ".mp4", ".wav", ".cda", ".mid", ".midi", ".mpa", ".wpl", ".ogg", ".aif" };
        public static string[] TopLevelDomains = { ".com", ".org", ".net", ".edu", ".gov", ".mil", ".arpa", ".ac", ".ae", ".af", ".ag", ".ai", ".al", ".am", ".ao", ".aq", ".ar", ".at", ".au", ".aw", ".ax", ".az", ".ba", ".bb", ".bd", ".be", ".bf", ".bg", ".bh", ".bi", ".bj", ".bm", ".bn", ".bo", ".br", ".bs", ".bt", ".bw", ".by", ".bz", ".ca", ".cc", ".cd", ".cf", ".cg", ".ch", ".ci", ".ck", ".cl", ".cm", ".cn", ".co", ".cr", ".cu", ".cv", ".cw", ".cx", ".cy", ".cz", ".de", ".dj", ".dk", ".dm", ".dz", ".ec", ".ee", ".eg", ".er", ".es", ".et", ".eu", ".fi", ".fj", ".fk", ".fm", ".fo", ".fr", ".ga", ".gd", ".ge", ".gf", ".gg", ".gh", ".gi", ".gl", ".gm", ".gn", ".gp", ".gq", ".gr", ".gs", ".gt", ".gu", ".gw", ".gy", ".hk", ".hm", ".hn", ".hr", ".ht", ".hu", ".id", ".ie", ".il", ".im", ".io", ".iq", ".ir", ".it", ".je", ".jm", ".jo", ".jp", ".ke", ".kg", ".kh", ".ki", ".km", ".kn", ".kp", ".kr", ".kw", ".ky", ".kz", ".la", ".lb", ".lc", ".li", ".lk", ".lr", ".ls", ".lt", ".lu", ".lv", ".ly", ".ma", ".mc", ".md", ".me", ".mg", ".mh", ".mk", ".ml", ".mm", ".mn", ".mo", ".mp", ".mq", ".mr", ".ms", ".mt", ".mu", ".mv", ".mw", ".mx", ".my", ".mz", ".na", ".nc", ".ne", ".nf", ".ng", ".ni", ".nl", ".no", ".nr", ".nu", ".nz", ".om", ".pa", ".pe", ".pf", ".pg", ".ph", ".pk", ".pl", ".pm", ".pn", ".pr", ".ps", ".pt", ".pw", ".py", ".qa", ".re", ".ro", ".rs", ".ru", ".rw", ".sa", ".sb", ".sc", ".sd", ".se", ".sg", ".sh", ".si", ".sk", ".sl", ".sm", ".sn", ".so", ".sr", ".ss", ".st", ".su", ".sv", ".sx", ".sy", ".sz", ".tc", ".td", ".tf", ".tg", ".th", ".tj", ".tk", ".tl", ".tm", ".tn", ".to", ".tr", ".tt", ".tv", ".tw", ".tz", ".ua", ".ug", ".uk", ".us", ".uy", ".uz", ".va", ".vc", ".ve", ".vg", ".vi", ".vn", ".vu", ".wf", ".ws", ".ye", ".yt", ".za", ".zm", ".zw" };
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

        #region MainLogic Methods

        #region StartDownload
        public void StartDownload()
        {
            //this sets the extension
            SetCurrentExtension();

            if (!string.IsNullOrEmpty(Url))
            {

                //todo: test changing file name and different combinations of folders (some half full, empty, etc)

                //Gets the destination for the files if the user didn't specified one
                if (string.IsNullOrEmpty(DestinationOfFile))
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

                //todo: maybe add a field to start download at a certain file?
                //Gets the Prefix for recursive Downloads.  If not recursive the file is just named whatever is given in the SaveDialogbox
                if (!IsFile)
                {
                    if (PrefixFileName == true && FileNamePrefix == FILENAME_PREFIX_DEFAULT)
                    {
                        FileNamePrefix = Path.GetFileNameWithoutExtension(DestinationOfFile);
                    }
                }

                //Create a folder with the value of FileName if the FileName and DirectoryName are the same
                if (DirectoryName.ToUpper() != FileName.ToUpper() && !IsFile)
                {
                    Directory.CreateDirectory(DestinationOfFile);
                    NewFolderCreated = true;
                }

                //Case: File (Downloads file and catches errors)
                if (Url.StartsWith("http") && IsFile == true)
                {
                    //Initializing
                    string message;

                    try
                    {
                        DownLoadFileAsync(Url);
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

                //Case: WEbsite
                else
                {
                    //DownloadFromWebsite()
                    DownloadFromWebsite(Url);
                }
            }
            else
            {
                throw new Exception("Url is empty.  Please specify the URL.");
            }
        }

        #endregion

        #region DownLoadFileAsync and GetDestinationOfFiles
        private void DownLoadFileAsync(string url, bool recursive = false)
        {
            //Sets DestinationOfFiles
            //for recursive calls only
            if (recursive)
            {
                GetDestinationOfFiles(url);
            }
            //Actual downloading
            using (WebClient client = new WebClient())
            {
                //todo: why does it take a while for the event handler to grab info and display?  
                //todo: TOTAL RE-WORK IDEA: two threads one handling downloading (has a delay) and the other getting the urls.
                //todo: add feature to save items in recurse and download queues to a file if program closes and then reload upon next start
                //todo: feature to canel current download and queue up downloads
                //todo: feature to download from Ricoh's TSRC ?

                //flag to only pause if the file exists
                if (!File.Exists(DestinationOfFile))
                {
                    //Updating events
                    client.DownloadProgressChanged += DownloadProgressChanged;
                    client.DownloadFileCompleted += DownloadFileCompleted;

                    //Actually downloading
                    Uri uri = new Uri(url);
                    Stopwatch.Start();

                    if (IsFirstTimeDownloading)
                    {
                        IsFirstTimeDownloading = false;
                    }
                    else
                    {
                        //todo: this is the thread.sleep line
                        //MessageBox.Show(string.Format("Sleeping: {0} ms", (int)(SecondsBetweenDownloads * 1000f)));
                        Thread.Sleep((int)(SecondsBetweenDownloads * 1000f));
                    }
                    client.DownloadFileAsync(uri, DestinationOfFile);
                }
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

        private void GetDestinationOfFiles(string url)
        {
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

                DownLoadFileAsync(DownloadQueue.Dequeue(), true);
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
            foreach (var extension in AudioFileExtensions)
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
                //MessageBox.Show(string.Format("Dest set to: {0}", DestinationOfFile));
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

        public static void RenameFiles(string sourceFolder, string regex, string replaceWith)
        {
            //todo: idea for a new method that renames all the files in a directory based on a regular expression


        }

        #endregion

        #region Event Methods

        #region DownloadFileCompleted
        public void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Stopwatch.Reset();
            if (e.Cancelled == true)
            {
                MessageBox.Show("Download has been canceled.");
            }
            else
            {
                MessageBox.Show(string.Format("Download of '{0}{1}' is complete.", FileName, FileExtension));
            }

        }

        #endregion
        #region DownloadProgressChanged
        public void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var bytesToMBFactor = Math.Pow(2, 20);

            //NetworkToolsWindow.updatelabel.Text = FileName;
            //NetworkToolsWindow.speedlabel.Text = string.Format("{0:##.##}", (e.BytesReceived / bytesToMBFactor / Downloader.Stopwatch.Elapsed.TotalSeconds).ToString("0.00"));
            //NetworkToolsWindow.totallabel.Text = string.Format("{0:##.##}", e.TotalBytesToReceive / bytesToMBFactor);
            //NetworkToolsWindow.downloadedlabel.Text = string.Format("{0:##.##}", e.BytesReceived / bytesToMBFactor);
            //NetworkToolsWindow.progressBar1.Value = e.ProgressPercentage;
        }

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
                    // MessageBox.Show(string.Format("Url: {0}", Url));

                    MatchCollection matches = Regex.Matches(this.Url, @"https?://.+(\.com|\.edu|\.org|\.gov|\.net)");

                    if (String.IsNullOrEmpty(this.Url))  //define your own logic for data validation
                    {
                        retvalue = string.Format("Please enter a valid, complete URL.");
                    }
                    else if (matches.Count < 1)
                    {
                        retvalue = string.Format("Please enter a valid, complete URL.\n'{0}' is either not valid or doesn't contain \n'http:...' and/or a top level domain (e.g. .com, .edu)", this.Url);    //define an appropriate messag
                    }
                }
                if (propertyName == "DestinationOfFile")
                {
                    bool pathExists = Directory.Exists(this.DestinationOfFile);
                    // MessageBox.Show(string.Format("PathExists: {0}", pathExists));


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


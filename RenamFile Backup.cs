using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Downloader2
{
    ///<summary>
    /// renames all the files in a directory based on the IDv3 tag
    /// </summary>
    /// <param name="sourceFolder"></param>
    /// <param name="regex"></param>
    /// <param name="replaceWith"></param>
    #region RenameFile
    class RenameFile
    {
        #region Public Fields
        /// <summary>
        /// Uses the name of the directory in which the files reside as the prefix for the new file name.  Default is true.  This only applies if the prefix separator isn't found.
        /// </summary>
        public static bool UseParentNameAsPrefix = true;
        /// <summary>
        /// The character in the filename of each file that corresponds to the prefix separator.
        /// </summary>
        public static char[] Separator = new char[] { '-' };
        private static char[] ILLEGAL_CHARACTERS = { ':', '\"', '~', '|', '&', '@', '#', '$', '%', '^', '*', '[', ']' };
        #endregion

        #region FromIDv3
        /// <summary>
        /// Renames the file based on IDv3 tag Performers and Title
        /// </summary>
        /// <param name="path">Path to the file as string</param>
        /// <param name="recursion">whether to change name to all mp3 files in subdirectories of path</param>
        public static void FromIDv3(string path, bool recursion = false, string toReplaceRegexPattern = null, string toReplaceWith = " ")
        {
            #region Getting files
            List<string> files = new List<string>();
            if (recursion)
            {
                files = GetFilesRecursively(path);
            }
            else
            {
                string[] temp = Directory.GetFiles(path, "*.mp3");
                files = temp.ToList();
            }
            #endregion

            #region Change File Name Procedure
            foreach (var file in files)
            {
                #region Intitializing
                //MessageBox.Show(string.Format("originalFileName: {0}", originalFileName));
                string prefix = null;
                string oldFileName = file;
                string fileName = Path.GetFileName(oldFileName);

                string directory = Directory.GetParent(oldFileName).ToString();
                string fileExtension = Path.GetExtension(oldFileName);
                #endregion

                #region Getting the current prefix if it exists
                if (Separator.Any(sep => oldFileName.Contains(sep.ToString())))
                {
                    string[] temp = oldFileName.Split(Separator);
                    prefix = Path.GetFileName(temp[0]).ToString();
                }
                #endregion

                #region Using the parent directory as a prefix if no prefix
                else if (UseParentNameAsPrefix == true)
                {
                    prefix = Path.GetFileName(Directory.GetParent(oldFileName).ToString());
                }
                #endregion

                #region Getting IDv3 Info
                TagLib.File tagLibFile = TagLib.File.Create(oldFileName);
                string[] performers = tagLibFile.Tag.Performers;
                string title = tagLibFile.Tag.Title;
                #endregion

                #region Getting filename depending on performer length
                if (!string.IsNullOrEmpty(title))
                {
                    switch (performers.Length)
                    {
                        case 0:
                            {
                                fileName = string.Format("{0} - {1}", prefix.Trim(), title.Trim());
                                break;
                            }
                        default:
                            {
                                fileName = string.Format("{0} - {1} - {2}", prefix.Trim(), performers[0].Trim(), title.Trim());
                                break;
                            }
                    }
                }
                else
                {
                    fileName = string.Format("{0} - {1}", prefix.Trim(), fileName.Trim());
                }

                #endregion

                #region Replacing Illegal Character
                foreach (char character in ILLEGAL_CHARACTERS)
                {
                    if (fileName.Contains(character))
                    {
                        fileName = fileName.Replace(character, ' ');
                    }
                }
                string newFileName = oldFileName.Contains(fileExtension) ? Path.Combine(directory, fileName) : Path.Combine(directory, fileName + fileExtension);
                newFileName = Regex.Replace(newFileName, @"[\s]{2,}", " ");
                if (!string.IsNullOrEmpty(toReplaceRegexPattern))
                {
                    newFileName = Regex.Replace(newFileName, toReplaceRegexPattern, toReplaceWith);
                }
                //MessageBox.Show(string.Format("originalFileName: {0}\n length: {1}\nnewFileName: {2}\n length: {3}", originalFileName, originalFileName.Length, newFileName, newFileName.Length));
                #endregion

                #region Renaming File
                if (!File.Exists(newFileName) && (!string.IsNullOrEmpty(title) || performers.Length > 0 || !string.IsNullOrEmpty(toReplaceRegexPattern)))
                {
                    MessageBox.Show(string.Format("Renaming {0} to fileName: {1}", oldFileName, newFileName));
                    if (!newFileName.Contains(fileExtension))
                    {
                        newFileName += fileExtension;
                    }
                    File.Move(oldFileName, newFileName);
                }
                #endregion
            }
            #endregion
        }
        #endregion

        #region GetFilesRecursively
        public static List<string> GetFilesRecursively(string sourceDir, string searchPattern = "*.mp3")
        {
            List<string> res = new List<string>();
            try
            {
                #region Gets the files in all SubFolders of sourceDir
                foreach (string dir in Directory.GetDirectories(sourceDir))
                {
                    foreach (string file in Directory.GetFiles(dir, searchPattern))
                    {
                        res.Add(file);
                    }
                    GetFilesRecursively(dir);
                }
                #endregion

                #region Gets the files in sourceDir
                foreach (string file in Directory.GetFiles(sourceDir, searchPattern))
                {
                    res.Add(file);
                }
                #endregion
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(string.Format("Error Recursively Getting Files:\n{0}", excpt.Message));
            }
            return res;
        }
        #endregion

        enum RenameFileModes
        {
            KeepPrefix,
            RemovePrefix,
        }
    }
    #endregion
}

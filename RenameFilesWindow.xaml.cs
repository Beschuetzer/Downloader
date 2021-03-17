using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Downloader
{
	/// <summary>
	/// Interaction logic for RenameFilesWindow.xaml
	/// </summary>
	public partial class RenameFilesWindow : Window
	{
        #region Initialization
        public double WindowWidthForTextBox = 745;
		public double WindowWidthStarting = 400;
		private bool WindowLoaded = false;
		#endregion
		#region Constructor
		public RenameFilesWindow()
		{
			InitializeComponent();
			InitializeRenameFile();
		}
		#endregion
		#region InitializeRenameFile
		private void InitializeRenameFile()
		{
			TextBox_Path.Focus();
			RenameFile renameFile = new RenameFile()
			{

			};
			this.DataContext = renameFile;
			this.WindowState = WindowState.Normal;
			WindowLoaded = true;
		}
		#endregion
		#region CommandBinding_Executed
		private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) 
		{
			String Name = ((RoutedCommand)e.Command).Name;
			if (Name == "RenameFilesOk")
			{
				RenameFile renameFile = this.DataContext as RenameFile;
				renameFile.RenameDirectory("", RenameFile.RenameFileModes.FromOutsideFile);
			}
			else if (Name == "PreviewFilenames")
			{
				#region Initialization Stuff
				this.WindowState = WindowState.Maximized;
				this.MaxWidth = 1920;
				ObservableCollection<FilenameChange> fileNamesChanges = new ObservableCollection<FilenameChange>();
				RenameFile renameFile = this.DataContext as RenameFile;
				#endregion
				#region Getting the Changed File Collection
				renameFile.PreviewChanges = true;
				renameFile.PreviewChangesCollection.Clear();
				renameFile.RenameDirectory(TextBox_Path.Text, RenameFile.RenameFileModes.FromOutsideFile);
				#endregion
				DataGrid_ChangePreview.ItemsSource = renameFile.PreviewChangesCollection;
			}
			else if (Name == "ChangeFileNamesCustom")
			{
				int i = 0;
				//todo: find out a way to just call rename file directly rather than repetitive code here
				#region Iterating through the datagrid (This code can all be in rename file renameDirectory?
				foreach (FilenameChange item in DataGrid_ChangePreview.ItemsSource)
				{
					//MessageBox.Show(string.Format("Changing: {0}\n to: {1}", item.FilePath, item.DesiredFileName));
					string extension = Path.GetExtension(item.FilePath);

					string desiredFileName = RenameFile.RemoveInvalidFileNameCharacters(item.DesiredFileName.Trim());
					desiredFileName = desiredFileName.Replace("  ", " ");
					string newDestination = Path.Combine(Directory.GetParent(item.FilePath).ToString(), desiredFileName) + extension;
					if (!File.Exists(newDestination))
					{
						File.Move(item.FilePath, newDestination);
						i++;
					}
				}
				#endregion
				System.Windows.Forms.MessageBox.Show($"Renaming Complete.  {i} files renamed in '{TextBox_Path.Text}'. ", "Success!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
			}
			else if (Name == "Exit")
			{
				this.Close();
			}
		}
		#endregion
		#region CommandBinding_CanExecute
		private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			string Name = ((RoutedCommand)e.Command).Name;
			if (Name == "RenameFilesOk")
			{
                #region Illegal Characters and Max Prefix Count Conditions
                int maxPrefixCountValue;
				RenameFile.IllegalCharacters.AddRange(Path.GetInvalidPathChars().ToList());
				bool ContainsIllegalChars = false;
				foreach (char c in RenameFile.IllegalCharacters)
				{
					if (TextBox_Separator.Text.Contains(c))
					{
						ContainsIllegalChars = true;
						break;
					}
				}
				if (!string.IsNullOrEmpty(TextBox_MaxPrefixSeparatorCount.Text))
				{
					maxPrefixCountValue = Int32.Parse(TextBox_MaxPrefixSeparatorCount.Text);
				}
				else
				{
					maxPrefixCountValue = 0;
				}
				#endregion
				#region Various Conditions
				bool ImageIsValid = RenameFile.ImageFormats.Any(f => Path.GetExtension(TextBox_Path_IDv3Image.Text).ToUpper().Contains(f));
				bool ImagePathCondition = (File.Exists(TextBox_Path_IDv3Image.Text) && ImageIsValid || string.IsNullOrEmpty(TextBox_Path_IDv3Image.Text));
				bool ChangeSomethingIDv3 = CheckBox_WriteIDv3Tags.IsChecked == false || (!string.IsNullOrEmpty(TextBox_IDv3Album.Text) || !string.IsNullOrEmpty(TextBox_IDv3Artist.Text) || !string.IsNullOrEmpty(TextBox_IDv3TitleRegex.Text) || !string.IsNullOrEmpty(TextBox_Path_IDv3Image.Text));
				bool ChangeSomethingFilename = (CheckBox_ChangeFilename.IsChecked == true && !string.IsNullOrEmpty(TextBox_Separator.Text) && !ContainsIllegalChars) || CheckBox_ChangeFilename.IsChecked == false;
				bool FilenameRegexCondition = !string.IsNullOrEmpty(TextBox_FilenameRegex.Text) || CheckBox_FilenameRegex.IsChecked == false;
				bool PrefixSourceCondition = RadioButton_Suffix_UseCustomInput.IsChecked == false && RadioButton_PrefixSource_Textbox.IsChecked == false;
				bool ArtistRegexCondition = !string.IsNullOrEmpty(TextBox_Files_IDv3ArtistRegex.Text) || CheckBox_Files_IDv3ArtistRegex.IsChecked == false;
				bool PrefixRegexCondition = RadioButton_Prefix_UseRegex.IsChecked == false || !string.IsNullOrEmpty(TextBox_PrefixRegex.Text);
				bool WindowWidthCondition = this.MaxWidth == WindowWidthStarting || (CheckBox_ChangeFilename.IsChecked == false && CheckBox_WriteIDv3Tags.IsChecked == true);
				#endregion
				if (maxPrefixCountValue > 0 && PrefixSourceCondition && FilenameRegexCondition && ArtistRegexCondition && PrefixRegexCondition && WindowWidthCondition && ImagePathCondition && Directory.Exists(TextBox_Path.Text) && ChangeSomethingFilename && ChangeSomethingIDv3)
				{
					e.CanExecute = true;			
				}
				else
				{
					e.CanExecute = false;
				}
			}
			else if (Name == "PreviewFilenames")
			{
                //todo: add it so that only executable when textbox prefix regex is not null or empty when radiobutton regex is selected.
                if (string.IsNullOrEmpty(TextBox_PrefixRegex.Text) && RadioButton_Prefix_UseRegex.IsChecked == true )
                {
					e.CanExecute = false;
                }
				else if ((CheckBox_IncludeAllFileTypes.IsChecked == true || Regex.Match(TextBox_FileExtensions.Text,@"\..{2,4}", RegexOptions.IgnoreCase).Success) && Directory.Exists(TextBox_Path.Text) && CheckBox_ChangeFilename.IsChecked == true && (!string.IsNullOrEmpty(TextBox_FilenameRegex.Text) || CheckBox_FilenameRegex.IsChecked == false) && (!string.IsNullOrEmpty(TextBox_Files_IDv3ArtistRegex.Text) || CheckBox_Files_IDv3ArtistRegex.IsChecked == false))
				{
					e.CanExecute = true;
				}
				else
				{
					e.CanExecute = false;
				}
			}
			else if (Name == "ChangeFileNamesCustom")
			{
				RenameFile renameFile = this.DataContext as RenameFile;
				if (WindowLoaded)
				{
					if (renameFile.CanMakeChanges)
					{
						e.CanExecute = true;
					}
					else
					{
						e.CanExecute = false;
					}
				}
			}
            else
            {
				e.CanExecute = true;
            }
		}
		#endregion
		#region Needed For MaxPrefix Depth Text boxes (Only allows numeric input)
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
		#region Needed For Prefix Separator Text box (Only allows one character)
		private void VerifyOneCharacter(object sender, TextCompositionEventArgs e)
		{
			TextBox_Separator.Text = TextBox_Separator.Text.Trim();
			e.Handled = !TextBoxSeparatorIsOneCharacter(e.Text);
		}
		private void MaskTextBoxInput(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				string input = (string)e.DataObject.GetData(typeof(string));
				if (!TextBoxSeparatorIsOneCharacter(input))
					e.CancelCommand();
			}
			else
			{
				e.CancelCommand();
			}
		}
		private bool TextBoxSeparatorIsOneCharacter(string input)
		{
			return TextBox_Separator.Text.Length != 1 ;
		}
		#endregion
		#region Event Handlers
		private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
		private void textbox_GotFocus(object sender, RoutedEventArgs e)
		{
			TextBox textBox = ((TextBox)sender);
			textBox.Text = "";
		}
        private void CheckBox_UseCustom_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_Prefix.IsEnabled = true;
			TextBox_Prefix.Focus();
		}
		private void CheckBox_UseCustom_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_Prefix.IsEnabled = false;
			TextBox_Prefix.Text = "";
			TextBox_Prefix.Focus();
		}
		private void RadioButton_Suffix_UseCustomInput_Checked(object sender, RoutedEventArgs e)
		{
			//this.MaxHeight = 850;
			if (this.MaxWidth < WindowWidthForTextBox)
			{
				this.MaxWidth = WindowWidthForTextBox;
			}
			TextBox_FileNames.Focus();
			TextBox_RegexReplaceWith.IsEnabled = true;
			TextBox_RegexToUse.IsEnabled = true;
			//RadioButton_TitleSource_Textbox.IsChecked = true;
		}
		private void RadioButton_Suffix_UseCustomInput_Unchecked(object sender, RoutedEventArgs e)
        {
			if (RadioButton_PrefixSource_Textbox.IsChecked == false && RadioButton_TitleSource_Textbox.IsChecked == false && Math.Round(this.Width) == WindowWidthForTextBox)
			{
				this.MaxWidth = WindowWidthStarting;
			}
			//this.MaxHeight = 363;			
			TextBox_RegexReplaceWith.IsEnabled = false;
			TextBox_RegexToUse.IsEnabled = false;
			TextBox_RegexReplaceWith.Text = "";
			TextBox_RegexToUse.Text = "";
			//RadioButton_TitleSource_Filename.IsChecked = true;
		}
		public static string StringFromRichTextBox(RichTextBox rtb)
		{
			TextRange textRange = new TextRange(
				// TextPointer to the start of content in the RichTextBox.
				rtb.Document.ContentStart,
				// TextPointer to the end of content in the RichTextBox.
				rtb.Document.ContentEnd
			);

			// The Text property on a TextRange object returns a string
			// representing the plain text content of the TextRange.
			return textRange.Text;
		}
        private void CheckBox_WriteIDv3Tags_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_IDv3Album.IsEnabled = true;
			TextBox_IDv3Artist.IsEnabled = true;
			TextBox_IDv3Artist.Focus();
			TextBox_IDv3TitleRegex.IsEnabled = true;
			RadioButton_TitleSource_Filename.IsEnabled = true;
			RadioButton_TitleSource_Textbox.IsEnabled = true;
			TextBox_Path_IDv3Image.IsEnabled = true;
			Button_Browse_IDv3Image.IsEnabled = true;
		
		}

        private void CheckBox_WriteIDv3Tags_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_IDv3Album.IsEnabled = false;
			TextBox_IDv3Artist.IsEnabled = false;
			TextBox_IDv3TitleRegex.IsEnabled = false;
			RadioButton_TitleSource_Filename.IsEnabled = false;
			RadioButton_TitleSource_Textbox.IsEnabled = false;
			TextBox_Path_IDv3Image.IsEnabled = false;
			Button_Browse_IDv3Image.IsEnabled = false;
			TextBox_IDv3Album.Text = "";
			TextBox_IDv3Artist.Text = "";
			TextBox_IDv3TitleRegex.Text = "";
			TextBox_Path_IDv3Image.Text = "";
		}
		private void CheckBox_ChangeFilename_Checked(object sender, RoutedEventArgs e)
        {
            //todo: finish CheckBox_ChangeFilename implementation and test IDv3 tag writing
            if (RadioButton_Prefix_UseCustom.IsChecked == true)
            {
				TextBox_Prefix.IsEnabled = true;
			}
			TextBox_Separator.IsEnabled = true;
			RadioButton_Prefix_UseCustom.IsEnabled = true;
			RadioButton_Prefix_UseFilePrefix.IsEnabled = true;
			RadioButton_Prefix_UseParentFolder.IsEnabled = true;
			RadioButton_Suffix_UseCustomInput.IsEnabled = true;
			TextBox_RegexReplaceWith.IsEnabled = true;
			TextBox_RegexReplaceWith.Text = "";
			TextBox_RegexToUse.IsEnabled = true;
			TextBox_RegexToUse.Text = "";
			RadioButton_Prefix_UseRegex.IsEnabled = true;
			if (RadioButton_Prefix_UseRegex.IsChecked == true)
			{
				RadioButton_PrefixSource_Filename.IsEnabled = true;
				RadioButton_PrefixSource_Textbox.IsEnabled = true;
				TextBox_PrefixRegex.IsEnabled = true;
			}
			if(RadioButton_Prefix_UseFilePrefix.IsChecked == true)
            {
				TextBox_MaxPrefixSeparatorCount.IsEnabled = true;
			}
			RadioButton_Suffix_UseFilename.IsEnabled = true;
			RadioButton_Suffix_UseIDv3Title.IsEnabled = true;
			TextBox_Suffix_RegexToUse.IsEnabled = true;
		}
		private void CheckBox_ChangeFilename_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_Prefix.IsEnabled = false;
			TextBox_Separator.IsEnabled = false;
			TextBox_MaxPrefixSeparatorCount.IsEnabled = false;
			RadioButton_Prefix_UseCustom.IsEnabled = false;
			RadioButton_Prefix_UseFilePrefix.IsEnabled = false;
			RadioButton_Prefix_UseParentFolder.IsEnabled = false;
			RadioButton_Suffix_UseCustomInput.IsEnabled = false;
			TextBox_RegexReplaceWith.IsEnabled = false;
			TextBox_RegexToUse.IsEnabled = false;
			RadioButton_Prefix_UseRegex.IsEnabled = false;
			RadioButton_PrefixSource_Filename.IsEnabled = false;
			RadioButton_PrefixSource_Textbox.IsEnabled = false;
			TextBox_PrefixRegex.IsEnabled = false;
			RadioButton_Suffix_UseFilename.IsEnabled = false;
			RadioButton_Suffix_UseIDv3Title.IsEnabled = false;
			TextBox_Suffix_RegexToUse.IsEnabled = false;
		}
		private void RadioButton_TitleSource_Textbox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.MaxWidth < WindowWidthForTextBox)
            {
				this.MaxWidth = WindowWidthForTextBox;
			}
			TextBox_FileNames.Focus();
		}
		private void RadioButton_TitleSource_Textbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (RadioButton_Suffix_UseCustomInput.IsChecked == false && RadioButton_PrefixSource_Textbox.IsChecked == false && Math.Round(this.Width) == WindowWidthForTextBox) 
            {
				this.MaxWidth = WindowWidthStarting;
			}
		}
        private void RadioButton_Prefix_UseRegex_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_PrefixRegex.IsEnabled = true;
			TextBox_PrefixRegex.Focus();
			RadioButton_PrefixSource_Filename.IsEnabled = true;
			RadioButton_PrefixSource_Textbox.IsEnabled = true;
		}
        private void RadioButton_Prefix_UseRegex_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_PrefixRegex.Text = "";
			TextBox_PrefixRegex.IsEnabled = false;
			RadioButton_PrefixSource_Filename.IsEnabled = false;
			RadioButton_PrefixSource_Textbox.IsEnabled = false;
		}
        private void RadioButton_PrefixSource_Textbox_Checked(object sender, RoutedEventArgs e)
        {
			if (this.MaxWidth < WindowWidthForTextBox)
			{
				this.MaxWidth = WindowWidthForTextBox;
			}
			TextBox_FileNames.Focus();
		}
		private void RadioButton_PrefixSource_Textbox_Unchecked(object sender, RoutedEventArgs e)
        {
			if (RadioButton_Suffix_UseCustomInput.IsChecked == false && RadioButton_TitleSource_Textbox.IsChecked == false && Math.Round(this.Width) == WindowWidthForTextBox)
			{
				this.MaxWidth = WindowWidthStarting;
			}
		}
        private void RadioButton_Prefix_UseFilePrefix_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_MaxPrefixSeparatorCount.IsEnabled = true;
		}

        private void RadioButton_Prefix_UseFilePrefix_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_MaxPrefixSeparatorCount.IsEnabled = false;
		}

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_FilenameRegex.IsEnabled = true;
			TextBox_FilenameRegex.Focus();
		}

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_FilenameRegex.Text = "";
			TextBox_FilenameRegex.IsEnabled = false;
		}

        private void CheckBox_Files_IDv3ArtistRegex_Checked(object sender, RoutedEventArgs e)
        {
			TextBox_Files_IDv3ArtistRegex.IsEnabled = true;
			TextBox_Files_IDv3ArtistRegex.Focus();
		}

        private void CheckBox_Files_IDv3ArtistRegex_Unchecked(object sender, RoutedEventArgs e)
        {
			TextBox_Files_IDv3ArtistRegex.Text = "";
			TextBox_Files_IDv3ArtistRegex.IsEnabled = false;
		}

        private void Button_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            if (RadioButton_TitleSource_Textbox.IsChecked == true || RadioButton_Suffix_UseCustomInput.IsChecked == true || RadioButton_PrefixSource_Textbox.IsChecked == true)
            {
				this.MaxWidth = WindowWidthForTextBox;
			}
            else
            {
				this.MaxWidth = WindowWidthStarting;
            }
        }
        #endregion
        private void Button_Browse_IDv3Image_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
				//todo: 
                openFileDialog.CheckPathExists = true;
                openFileDialog.CheckFileExists = true;
				openFileDialog.Filter = "Image files|*.JPG;*.JPEG;*.JFIF; *.Exif; *.TIFF; *.GIF; *.BMP; *.PNG; *.PPM; *.PGM; *.PBM; *.PNM| All files (*.*)|*.*";
                openFileDialog.Multiselect = true;
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
					TextBox_Path_IDv3Image.Text = openFileDialog.FileName;
				}
            }
        }
    }
}

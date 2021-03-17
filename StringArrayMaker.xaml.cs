using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Downloader
{
    /// <summary>
    /// Interaction logic for StringArrayMaker.xaml
    /// </summary>
    public partial class StringArrayMaker : Window
    {
        #region Intitialization
        private ObservableCollection<string> Surrounders = new ObservableCollection<string>(){ "'", "\""};
        public StringArrayMaker()
        {
            InitializeComponent();
            //ComboBox_ItemSurrounder.ItemsSource = Surrounders;
          
            TextBox_ListOfItems.Focus();
        }
        #endregion
        #region CommandBinding_Executed
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)      //These two methods are used by RoutedCommand and RoutedUICommand.  Put in the main Window class
		{
			String Name = ((RoutedCommand)e.Command).Name;
			if (Name == "StringArrayMakerOk")
			{
				Downloader.StringArrayMaker(TextBox_ListOfItems.Text, TextBox_ItemSurrounder.Text.ToString(), TextBox_Separator.Text[0], TextBox_FileOut.Text);
                System.Windows.Forms.MessageBox.Show($"StringArrayCreated at {TextBox_FileOut.Text}!", "StringArray Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
		#endregion
		#region CommandBinding_CanExecute
		private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
            string Name = ((RoutedCommand)e.Command).Name;
			if (Name == "StringArrayMakerOk")
			{
                //todo: bug with the separator and surrounder workaround accepting space
                if (Directory.Exists(TextBox_FileOut.Text) && !string.IsNullOrEmpty(TextBox_ItemSurrounder.Text) && Regex.Match(TextBox_ListOfItems.Text, @"\S+").Success)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
			}
            else
            {
                e.CanExecute = true;
            }
		}
		#endregion
		#region Event Handlers
        private void textbox_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Text = "";
            if (textBox.Name == "CheckBox_DefaultToDesktop")
            {
                CheckBox_DefaultToDesktop.IsChecked = false;
            }
        }

        private void CheckBox_DefaultToDesktop_Checked(object sender, RoutedEventArgs e)
        {
            TextBox_FileOut.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);            
        }

        private void CheckBox_DefaultToDesktop_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBox_FileOut.Text = "";
        }
        #endregion
        #region Needed For Surrounder Text box (Only allows one character)
        private void VerifyOneCharacter(object sender, TextCompositionEventArgs e)
        {
            TextBox_ItemSurrounder.Text = TextBox_ItemSurrounder.Text.Trim();
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
            return TextBox_ItemSurrounder.Text.Length != 1 ;
        }
        #endregion
        #region Needed For Surrounder Text box (Only allows one character)
        private void VerifyOneCharacter2(object sender, TextCompositionEventArgs e)
        {
            TextBox_Separator.Text = TextBox_Separator.Text.Trim();
            e.Handled = !TextBoxSeparatorIsOneCharacter2(e.Text);
        }
        private void MaskTextBoxInput2(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextBoxSeparatorIsOneCharacter2(input))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private bool TextBoxSeparatorIsOneCharacter2(string input)
        {
            return TextBox_Separator.Text.Length != 1;
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

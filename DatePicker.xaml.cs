using System;
using System.Windows;
using System.Windows.Input;

namespace Downloader
{
    /// <summary>
    /// Interaction logic for DatePicker.xaml
    /// </summary>
    public partial class DatePicker : Window
    {
        public string Answer
        {
            get { return DatePickerTextBox.Text;}
        }
        public DatePicker(string title, string question, string startingTextForTextBox = "")
        {
            InitializeComponent();
            this.Title = title;
            lblQuestion.Content = question;
        }
       
        #region CommandBinding_Executed
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)      //These two methods are used by RoutedCommand and RoutedUICommand.  Put in the main Window class
        {
            String Name = ((RoutedCommand)e.Command).Name;
            if (Name == "SubmitDate")
            {
                this.DialogResult = true;
            }
        }
        #endregion

        #region CommandBinding_CanExecute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            string Name = ((RoutedCommand)e.Command).Name;
            if (Name == "SubmitDate")
            {
                DateTime res;
                //determining if the UIElement should be turned on or off (CanExecute)
                if (DateTime.TryParse(DatePickerTextBox.Text, out res) )     //this was for a textbox
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
        }
        #endregion
    }
}

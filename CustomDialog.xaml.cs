using System.Windows;
using System.Windows.Controls;

namespace Downloader
{
    /// <summary>
    /// Interaction logic for GetRegexWindow.xaml
    /// </summary>
    public partial class CustomDialog : Window
    {
		public string Answer
		{
			get { return txtAnswer.Text; }
		}
		public CustomDialog(string title, string question, string startingTextForTextBox = "")
		{
			InitializeComponent();
			lblQuestion.Content = question;
			txtAnswer.Text = startingTextForTextBox;
			this.Title = title;
			txtAnswer.Focus();
		}

		private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void textbox_GotFocus(object sender, RoutedEventArgs e)
		{
			TextBox textBox = ((TextBox)sender);
			textBox.Text = "";
		}

	}
}

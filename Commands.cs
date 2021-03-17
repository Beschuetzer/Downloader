using System.Windows.Input;

namespace Downloader
{
    public static class Command
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand(
            "Exit",                                                           //this is just a description. 
            "Exit",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
            new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                        new KeyGesture(Key.F4, ModifierKeys.Alt)
            }
        );

        public static readonly RoutedUICommand Download = new RoutedUICommand(
            "Start Download",                                                           //this is just a description. 
            "Download",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
            new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                        new KeyGesture(Key.W, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ResetBoxes = new RoutedUICommand(
            "Reset URL and Destination",                                                           //this is just a description. 
            "ResetBoxes",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
            new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
			            new KeyGesture(Key.R, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand Cancel = new RoutedUICommand(
            "Cancels the current download",                                                           //this is just a description. 
            "Cancel",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
            new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                        new KeyGesture(Key.C, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand SubmitDate = new RoutedUICommand(
            "Allows submission for Date Picker Date",                                                           //this is just a description. 
            "SubmitDate",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(DatePicker),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                        new KeyGesture(Key.D, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ShowDownloadLog = new RoutedUICommand(
            "Expands the main window to show the Download Log",         //this is just a description. 
            "ShowDownloadLog",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.L, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ShowRenamingWindow = new RoutedUICommand(
            "Shows window for renaming a DirectoryPath",         //this is just a description. 
            "ShowRenamingWindow",                                   //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.R, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand RenameFilesOk = new RoutedUICommand(
            "When clicking ok in rename files",         //this is just a description. 
            "RenameFilesOk",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.O, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ShowStringArrayMakerWindow = new RoutedUICommand(
            "Opens StringArrayMaker Window",         //this is just a description. 
            "ShowStringArrayMakerWindow",                                                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.S, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand StringArrayMakerOk = new RoutedUICommand(
            "Executes StringArrayMaker",         //this is just a description. 
            "StringArrayMakerOk",                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.T, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand PreviewFilenames = new RoutedUICommand(
            "Loads the files and their new names into lvChangePreview",         //this is just a description. 
            "PreviewFilenames",                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.P, ModifierKeys.Shift | ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ChangeFileNamesCustom = new RoutedUICommand(
            "Changes the filenames based on what's in lvChangePreview",         //this is just a description. 
            "ChangeFileNamesCustom",                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.C, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand ShowFinderWindow = new RoutedUICommand(
            "Show Finder Window",                          //this is just a description. 
            "ShowFinderWindow",                         //this is what you refer to the command as in the CanExecute and Executed Methods
            typeof(MainWindow),
             new InputGestureCollection()                //this seems to be how to create a custom keyboard shortcut
            {
                    new KeyGesture(Key.F, ModifierKeys.Control)
            }
        );
        
    }
}

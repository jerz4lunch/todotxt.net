﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TrayMainWindows _tray;
        HotKeyMainWindows _hotkey;
        WindowLocation _previousWindowLocaiton;

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += (o, e) =>
            {
                    e.Handled = true;
					e.Exception.Handle("An unexpected error occurred");
            };

            InitializeComponent();

			SetupTrayIcon();

			CheckForUpdates();

			webBrowser1.Navigate("about:blank");

                SetFont();

			SetWindowPosition();

			lbTasks.Focus();
		}

		public MainWindowViewModel ViewModel { get; set; }

		private void CheckForUpdates()
                {
			var updateChecker = new UpdateChecker(this);
			updateChecker.BeginCheck();
                }

		private void SetupTrayIcon()
		{
			if (User.Default.MinimiseToSystemTray)
			{
				_tray = new TrayMainWindows(this);
				_hotkey = new HotKeyMainWindows(this, ModifierKeys.Windows | ModifierKeys.Alt, System.Windows.Forms.Keys.T);
			}
		}

		private void SetWindowPosition()
            {
			Height = User.Default.WindowHeight;
			Width = User.Default.WindowWidth;
			Left = User.Default.WindowLeft;
			Top = User.Default.WindowTop;
        }

        protected override void OnClosed(EventArgs e)
        {
			if (ViewModel.HelpPage != null)
				ViewModel.HelpPage.Close();

            base.OnClosed(e);
        }

        /// <summary>
        /// Helper function that converts the values stored in the settings into the font values
        /// and then sets the tasklist font values.
        /// </summary>
		public void SetFont()
        {
            var family = new FontFamily(User.Default.TaskListFontFamily);

            double size = User.Default.TaskListFontSize;

            var styleConverter = new FontStyleConverter();

            FontStyle style = (FontStyle)styleConverter.ConvertFromString(User.Default.TaskListFontStyle);

            var stretchConverter = new FontStretchConverter();
            FontStretch stretch = (FontStretch)stretchConverter.ConvertFromString(User.Default.TaskListFontStretch);

            var weightConverter = new FontWeightConverter();
            FontWeight weight = (FontWeight)weightConverter.ConvertFromString(User.Default.TaskListFontWeight);

            Color color = (Color)ColorConverter.ConvertFromString(User.Default.TaskListFontBrushColor);

			lbTasks.FontFamily = family;
			lbTasks.FontSize = size;
			lbTasks.FontStyle = style;
			lbTasks.FontStretch = stretch;
			lbTasks.FontWeight = weight;
			lbTasks.Foreground = new SolidColorBrush(color);
        }

        void SetSelected(MenuItem item)
        {
            var sortMenu = (MenuItem)item.Parent;
            foreach (MenuItem i in sortMenu.Items)
                i.IsChecked = false;

            item.IsChecked = true;

        }

        private void Filter(object sender, RoutedEventArgs e)
        {
			ViewModel.ShowFilterDialog();
        }

		public void ToggleUpdateMenu(string version)
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (version != assemblyVersion)
            {
				UpdateMenu.Header = "New version: " + version;
				UpdateMenu.Visibility = Visibility.Visible;
            }
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			ViewModel = new MainWindowViewModel(this);
			DataContext = ViewModel;
        }

		#region window location handlers
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            _previousWindowLocaiton = new WindowLocation();

            if (Left >= 0 && Top >= 0)
            {
				User.Default.WindowLeft = Left;
				User.Default.WindowTop = Top;
                User.Default.Save();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                User.Default.WindowLeft = _previousWindowLocaiton.Left;
                User.Default.WindowTop = _previousWindowLocaiton.Top;
                User.Default.WindowHeight = _previousWindowLocaiton.Height;
                User.Default.WindowWidth = _previousWindowLocaiton.Width;
                User.Default.Save();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height > 0 && e.NewSize.Width > 0 && WindowState != System.Windows.WindowState.Maximized)
            {
                User.Default.WindowHeight = e.NewSize.Height;
                User.Default.WindowWidth = e.NewSize.Width;
                User.Default.Save();
            }
        }
        #endregion

        #region command CanExecute methods

        private void WhenTasksSelectedCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbTasks.SelectedItems.Count > 0) && (!taskText.IsFocused);
            e.Handled = true;
        }

        private void WhenSingleTaskSelectedCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbTasks.SelectedItems.Count == 1) && (!taskText.IsFocused);
            e.Handled = true;
        }

        private void WhenTasksLoadedCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (lbTasks.Items.Count > 0);
            e.Handled = true;
        }

        private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        #endregion

        #region file menu

        private void OpenFileExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenFile();
        }

        public void NewFileExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.NewFile();
        }

                private void PrintPreviewFileExecuted(object sender, RoutedEventArgs e)
        {
            string printContents;
			printContents = ViewModel.GetPrintContents();

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.close();

			ViewModel.SetPrintControlsVisibility(true);
        }

        private void PrintFileExecuted(object sender, RoutedEventArgs e)
        {
            string printContents;
			printContents = ViewModel.GetPrintContents();

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.execCommand("Print", true, 0);
            doc.close();
        }
		
        private void ArchiveCompletedTasksExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ArchiveCompleted();
        }

        public void ReloadFileExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ReloadFile();
        }

        public void OptionsExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowOptionsDialog();
        }

        private void ExitApplicationExecuted(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Edit Menu

        private void CopyTasksExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.CopySelectedTasksToClipboard();
        }

        private void CopySelectedTaskToNewTaskExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.CopySelectedTaskToTextBox();
        }

        #endregion

        #region task menu

        private void NewTaskExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.AddNewTask();
        }

        private void UpdateTaskExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateTask();
        }

        private void DeleteTaskExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteTask();
        }

        private void ToggleCompletionExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleCompletion();
        }

        private void IncreasePriorityExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.IncreasePriority();
        }

        private void DecreasePriorityExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.DecreasePriority();
        }

        private void RemovePriorityExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePriority();
        }

        private void PostponeTaskExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.PostponeTask();
        }

        #endregion

        #region sort menu
        
        private void SetSelectedMenuItem(MenuItem menu, string selectedMenuItemTag)
        {
            foreach (var item in menu.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = (MenuItem)item;
                    menuItem.IsChecked = selectedMenuItemTag.Equals(menuItem.Tag);
                }
            }
        }

        private void SortByFileOrderExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.SortType = SortType.None;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "File");
        }

        private void SortByContextExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.SortType = SortType.Context;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "Context");
        }

        private void SortByCompletedExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.SortType = SortType.Completed;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "Completed");
        }

        private void SortByDueDateExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.SortType = SortType.DueDate;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "DueDate");
        }

        private void SortByProjectExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.SortType = SortType.Project;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "Project");
        }

        private void SortByAlphabeticalExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.SortType = SortType.Alphabetical;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "Alphabetical");
        }

        public void SortByCreatedDateExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.SortType = SortType.Created;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "CreatedDate");
        }

        private void SortByPriorityExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.SortType = SortType.Priority;
            ViewModel.UpdateDisplayedTasks();
            SetSelectedMenuItem(sortMenu, "Priority");
        }

        #endregion

        #region filter menu

        private void DefineFiltersExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowFilterDialog();
        }

        private void RemoveFilterExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ApplyFilterPreset0();
            SetSelectedMenuItem(filterMenu, "None");
        }

        private void ApplyFilterPreset1Executed(object sender, RoutedEventArgs e)
        {
            ViewModel.ApplyFilterPreset1();
            SetSelectedMenuItem(filterMenu, "Preset1");
        }

        private void ApplyFilterPreset2Executed(object sender, RoutedEventArgs e)
        {
            ViewModel.ApplyFilterPreset2();
            SetSelectedMenuItem(filterMenu, "Preset2");
        }

        private void ApplyFilterPreset3Executed(object sender, RoutedEventArgs e)
        {
            ViewModel.ApplyFilterPreset3();
            SetSelectedMenuItem(filterMenu, "Preset3");
        }

        #endregion

        #region help menu

        private void HelpAboutExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowHelpDialog();
        }

        private void ViewLogExecuted(object sender, RoutedEventArgs e)
        {
			ViewModel.ViewLog();
        }

        private void DonateExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.Donate();
        }

        private void ShowCalendarExecuted(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCalendarToTitle();
        }
        
        #endregion

		#region update menu

		private void GetUpdate(object sender, RoutedEventArgs e)
        {
			try 
			{
				Process.Start(UpdateChecker.updateClientUrl);
			}
			catch (Exception ex)
			{
				ex.Handle("Error while launching " + UpdateChecker.updateClientUrl);
        }
		}

        #endregion

        #region lbTasks

		// Using KeyDown allows for holding the key to navigate
        private void lbTasks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			ViewModel.TaskListPreviewKeyDown(e);
        }

        #endregion

		#region printing

		private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
			mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
			doc.execCommand("Print", true, 0);
			doc.close();

			ViewModel.SetPrintControlsVisibility(false);
        }

		private void btnCancelPrint_Click(object sender, RoutedEventArgs e)
        {
			ViewModel.SetPrintControlsVisibility(false);
                lbTasks.Focus();
            }


        private void File_PrintPreview(object sender, RoutedEventArgs e)
        {
            string printContents;
			printContents = ViewModel.GetPrintContents();

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.close();

			ViewModel.SetPrintControlsVisibility(true);
        }

        private void File_Print(object sender, RoutedEventArgs e)
        {
            string printContents;
			printContents = ViewModel.GetPrintContents();

            mshtml.IHTMLDocument2 doc = webBrowser1.Document as mshtml.IHTMLDocument2;
            doc.clear();
            doc.write(printContents);
            doc.execCommand("Print", true, 0);
            doc.close();
        }
		#endregion  //printing

		#region taskText

		private void taskText_PreviewKeyUp(object sender, KeyEventArgs e)
        {
			ViewModel.TaskTextPreviewKeyUp(e);
        }

        private void taskText_PreviewTextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.TaskTextPreviewTextChanged(e);
        }

        #endregion

        #region intellisense

        private void Intellisense_KeyDown(object sender, KeyEventArgs e)
        {
			ViewModel.IntellisenseKeyDown(e);
            e.Handled = true;
        }

        private void ShowIntellisense(IEnumerable<string> s, Rect placement)
        {
			ViewModel.ShowIntellisense(s, placement);
        }

        private void IntellisenseList_MouseUp(object sender, MouseButtonEventArgs e)
        {
			ViewModel.IntellisenseMouseUp();
        }
        #endregion

    }
}


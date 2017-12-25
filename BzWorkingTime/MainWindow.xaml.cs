using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using Xceed.Wpf.Toolkit;

namespace BzWorkingTime {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private MySqlClient mySqlClient;

		public DateTime DateTimeSelected { get; set; }
		public DateTime DateTimeReportStart { get; set; }
		public DateTime DateTimeReportFinish { get; set; }

		private DateTime _timeStart;
		public DateTime TimeStart {
			get {
				return _timeStart;
			}
			set {
				if (value != _timeStart) {
					_timeStart = value;
					NotifyPropertyChanged();
					Duration = TimeEnd - TimeStart;
				}
			}
		}

		private DateTime _timeEnd;
		public DateTime TimeEnd {
			get {
				return _timeEnd;
			}
			set {
				if (value != _timeEnd) {
					_timeEnd = value;
					NotifyPropertyChanged();
					Duration = TimeEnd - TimeStart;
				}
			}
		}

		private ItemEmployee _selectedEmployee;
		public ItemEmployee SelectedEmployee {
			get {
				return _selectedEmployee;
			}
			set {
				if (value != _selectedEmployee) {
					_selectedEmployee = value;
					NotifyPropertyChanged();
				}
			}
		}

		private TimeSpan _duration;
		public TimeSpan Duration {
			get {
				return _duration;
			}
			set {
				if (value != _duration) {
					_duration = value;
					NotifyPropertyChanged();
				}
			}
		}

		public ObservableCollection<ItemWorkPeriod> WorkPeriods { get; set; }
		public ObservableCollection<ItemEmployee> ReportEmployees { get; set; }

		private ListSortDirection _sortDirection;
		private GridViewColumnHeader _sortColumn;


		public MainWindow() {
			WorkPeriods = new ObservableCollection<ItemWorkPeriod>();
			ReportEmployees = new ObservableCollection<ItemEmployee>();
			mySqlClient = new MySqlClient();
			DateTimeSelected = DateTime.Now;
			DateTimeReportStart = DateTime.Now;
			DateTimeReportFinish = DateTime.Now;
			TimeStart = GetSelectedDateTime();
			TimeEnd = GetSelectedDateTime();
			DataContext = this;
			InitializeComponent();
			Closed += MainWindow_Closed;
		}




		private void ButtonSelect_Click(object sender, RoutedEventArgs e) {
			WindowUserSearch userSearch = new WindowUserSearch(mySqlClient) { Owner = this };
			if (userSearch.ShowDialog() != true)
				return;

			SelectedEmployee = userSearch.SelectedEmployee;
			UpdateWorkPeriods();
		}

		private void ButtonAddOrChange_Click(object sender, RoutedEventArgs e) {
			DateTime? start = TimePickerStart.Value;
			DateTime? finish = TimePickerEnd.Value;

			string error = string.Empty;

			if (start == null)
				error = "Не выбрано время начала";

			if (finish == null)
				error = "Не выбрано время окончания";

			if ((DateTime)finish <= (DateTime)start)
				error = "Время окончания не может быть меньше или равно времени начала";
			
			if (!string.IsNullOrEmpty(error)) {
				System.Windows.MessageBox.Show(this, "Время окончания не может быть меньше или равно времени начала", string.Empty,
					MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			if (sender == ButtonAdd)
				mySqlClient.Insert(SelectedEmployee.ID, (DateTime)start, (DateTime)finish);
			else if (sender == buttonChange)
				mySqlClient.Update((ListViewWorkTimes.SelectedItem as ItemWorkPeriod).Id, (DateTime)start, (DateTime)finish);
			
			UpdateWorkPeriods();
		}
		
		private void ButtonDelete_Click(object sender, RoutedEventArgs e) {
			if (System.Windows.MessageBox.Show(this, "Вы уверены, что хотите удалить выбранную запись?", "", 
				MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
				return;

			mySqlClient.Delete(SelectedEmployee.ID);
			UpdateWorkPeriods();
		}



		private void MainWindow_Closed(object sender, EventArgs e) {
			mySqlClient.CloseConnection();
		}

		private void DatePickerSelected_SelectedDateChanged(object sender, SelectionChangedEventArgs e) {
			UpdateWorkPeriods();
		}

		private void UpdateWorkPeriods() {
			if (SelectedEmployee == null || DateTimeSelected == null)
				return;

			List<ItemWorkPeriod> workPeriods = mySqlClient.GetWorkPeriods(SelectedEmployee.ID,
				DateTimeSelected.ToString("yyyy-MM-dd"));

			WorkPeriods.Clear();
			ButtonAdd.IsEnabled = false;
			buttonChange.IsEnabled = false;
			ButtonDelete.IsEnabled = false;

			if (workPeriods.Count == 0) {
				WorkPeriods.Add(new ItemWorkPeriod());
				ButtonAdd.IsEnabled = true;
				return;
			}

			foreach (ItemWorkPeriod item in workPeriods)
				WorkPeriods.Add(item);

			ListViewWorkTimes.SelectedItem = ListViewWorkTimes.Items[0];

			buttonChange.IsEnabled = true;
			ButtonDelete.IsEnabled = true;
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (ListViewWorkTimes.SelectedItems.Count == 0) {
				TimeStart = GetSelectedDateTime();
				TimeEnd = GetSelectedDateTime();
				return;
			}

			ItemWorkPeriod workPeriod = ListViewWorkTimes.SelectedItem as ItemWorkPeriod;
			TimeStart = workPeriod.DateStart == null ? GetSelectedDateTime() : (DateTime)workPeriod.DateStart;
			TimeEnd = workPeriod.DateFinish == null ? GetSelectedDateTime() : (DateTime)workPeriod.DateFinish;

			if (TimeStart != null && TimeEnd != null) {
				Duration = TimeEnd - TimeStart;
			} else {
				Duration = new TimeSpan();
			}
		}

		private DateTime GetSelectedDateTime() {
			return new DateTime(DateTimeSelected.Year, DateTimeSelected.Month, DateTimeSelected.Day, 0, 0, 0);
		}



		private void CheckBoxReportForAll_CheckedChanged(object sender, RoutedEventArgs e) {
			bool isChecked = CheckBoxReportForAll.IsChecked ?? false;

			if (ListViewReportEmployees != null)
				ListViewReportEmployees.IsEnabled = !isChecked;

			if (StackPanelButtonsForReportListView != null)
				StackPanelButtonsForReportListView.IsEnabled = !isChecked;
		}



		private void ListViewReportEmployees_Click(object sender, RoutedEventArgs e) {
			SortListViewColumn(sender, e, ref _sortColumn, ref _sortDirection);
		}

		private void ListViewReportEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ButtonRemoveEmployeeFromReport.IsEnabled = ListViewReportEmployees.SelectedItems.Count > 0;
		}

		private void SortListViewColumn(object sender, RoutedEventArgs e, ref GridViewColumnHeader columnHeader, ref ListSortDirection sortDirection) {
			GridViewColumnHeader column = e.OriginalSource as GridViewColumnHeader;
			if (column == null)
				return;

			if (columnHeader == column)
				sortDirection = sortDirection == ListSortDirection.Ascending ?
												 ListSortDirection.Descending :
												 ListSortDirection.Ascending;
			else {
				if (columnHeader != null) {
					columnHeader.Column.HeaderTemplate = null;
					columnHeader.Column.Width = columnHeader.ActualWidth - 20;
				}

				columnHeader = column;
				sortDirection = ListSortDirection.Ascending;
				column.Column.Width = column.ActualWidth + 20;
			}

			if (sortDirection == ListSortDirection.Ascending)
				column.Column.HeaderTemplate = Resources["ArrowUp"] as DataTemplate;
			else
				column.Column.HeaderTemplate = Resources["ArrowDown"] as DataTemplate;

			string header = string.Empty;

			Binding b = columnHeader.Column.DisplayMemberBinding as Binding;
			if (b != null)
				header = b.Path.Path;

			ICollectionView resultDataView = CollectionViewSource.GetDefaultView((sender as ListView).ItemsSource);
			resultDataView.SortDescriptions.Clear();
			resultDataView.SortDescriptions.Add(new SortDescription(header, sortDirection));
		}



		private void ButtonEmployeeAddToReport_Click(object sender, RoutedEventArgs e) {
			WindowUserSearch userSearch = new WindowUserSearch(mySqlClient) { Owner = this };
			if (userSearch.ShowDialog() != true)
				return;

			ItemEmployee employee = userSearch.SelectedEmployee;

			if (!ReportEmployees.Contains(employee))
				ReportEmployees.Add(employee);
		}

		private void ButtonRemoveEmployeeFromReport_Click(object sender, RoutedEventArgs e) {
			List<ItemEmployee> selected = new List<ItemEmployee>();

			foreach (ItemEmployee item in ListViewReportEmployees.SelectedItems)
				selected.Add(item);

			foreach (ItemEmployee item in selected)
				ReportEmployees.Remove(item);

			ButtonRemoveEmployeeFromReport.IsEnabled = ReportEmployees.Count > 0;
		}

		private void ButtonGenerateReport_Click(object sender, RoutedEventArgs e) {
			if (DateTimeReportFinish < DateTimeReportStart) {
				System.Windows.MessageBox.Show(this, 
					"Дата окончания отчетного периода не может быть меньше даты начала", string.Empty, 
					MessageBoxButton.OK, MessageBoxImage.Asterisk);
				return;
			}

			IsEnabled = false;
			ProgressBarReport.Visibility = Visibility.Visible;

			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += BackgroundWorker_DoWork;
			backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
			backgroundWorker.WorkerReportsProgress = true;
			backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
			backgroundWorker.RunWorkerAsync(CheckBoxReportForAll.IsChecked);
		}

		private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			ProgressBarReport.Value = e.ProgressPercentage;
		}

		private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			IsEnabled = true;
			ProgressBarReport.Visibility = Visibility.Hidden;
			ProgressBarReport.Value = 0;

			if (e.Error != null) {
				System.Windows.MessageBox.Show(
					this, e.Error.Message + Environment.NewLine + e.Error.StackTrace, 
					string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
			}

			System.Windows.MessageBox.Show(this, "Завершено", string.Empty,
				MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
			int progressCurrent = 0;
			(sender as BackgroundWorker).ReportProgress(progressCurrent);

			List<string> employees = new List<string>();
			if ((bool)e.Argument != true) {
				foreach (ItemEmployee item in ReportEmployees)
					employees.Add(item.ID);

				if (employees.Count == 0) {
					System.Windows.MessageBox.Show("Не выбрано ни одного сотрудника", string.Empty,
						MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			progressCurrent += 10;
			(sender as BackgroundWorker).ReportProgress(progressCurrent);

			DataTable dataTable = mySqlClient.GetReport(
				DateTimeReportStart, DateTimeReportFinish.AddDays(1).AddSeconds(-1), employees);

			if (dataTable.Rows.Count == 0) {
				System.Windows.MessageBox.Show("Нет результатов для выгрузки", string.Empty, 
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			progressCurrent += 10;
			(sender as BackgroundWorker).ReportProgress(progressCurrent);

			string result = NpoiExcel.WriteToExcel(dataTable, sender as BackgroundWorker, progressCurrent, 100);

			if (!File.Exists(result)) {
				System.Windows.MessageBox.Show("Не удалось сформировать отчет", 
					string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Process.Start(result);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BzWorkingTime {
	/// <summary>
	/// Логика взаимодействия для WindowUserSearch.xaml
	/// </summary>
	public partial class WindowUserSearch : Window {
		private MySqlClient mySqlClient;
		public string EnteredName { get; set; }
		public ObservableCollection<ItemEmployee> Employees { get; set; }
		private ListSortDirection _sortDirection;
		private GridViewColumnHeader _sortColumn;
		public ItemEmployee SelectedEmployee { get; set; }

		public WindowUserSearch(MySqlClient mySqlClient) {
			this.mySqlClient = mySqlClient;
			Employees = new ObservableCollection<ItemEmployee>();
			DataContext = this;
			InitializeComponent();

			Loaded += WindowUserSearch_Loaded;
		}

		private void WindowUserSearch_Loaded(object sender, RoutedEventArgs e) {
			TextBoxName.Focus();
		}

		private void ButtonSelect_Click(object sender, RoutedEventArgs e) {
			if (SelectedEmployee == null) {
				MessageBox.Show(this, "Не выбран сотрудник", "", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			DialogResult = true;
			Close();
		}

		private void ButtonSearch_Click(object sender, RoutedEventArgs e) {
			string enteredName = TextBoxName.Text;
			if (string.IsNullOrEmpty(enteredName) || 
				string.IsNullOrWhiteSpace(enteredName)) {
				MessageBox.Show(this, "Введите имя для поиска", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			Employees.Clear();

			List<ItemEmployee> employees = mySqlClient.GetEmployees(TextBoxName.Text);
			if (employees.Count == 0) {
				Employees.Add(new ItemEmployee() { FullName = "Нет результатов" });
				return;
			}

			foreach (ItemEmployee item in employees)
				Employees.Add(item);
		}

		private void ListView_Click(object sender, RoutedEventArgs e) {
			SortListViewColumn(sender, e, ref _sortColumn, ref _sortDirection);
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

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			ButtonSelect_Click(ButtonSelect, new RoutedEventArgs());
		}

		private void TextBoxName_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter)
				ButtonSearch_Click(ButtonSearch, new RoutedEventArgs());
		}
	}
}

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BzWorkingTime {
	public class MySqlClient {
		private MySqlConnection connection;
		private string queryGetEmployees;
		private string queryGetWorkPeriods;
		private string nonQueryInsert;
		private string nonQueryUpdate;
		private string nonQueryDelete;
		private string queryReportMain;
		private string queryReportUsers;
		private string queryReportOrder;
		private Random random;

		public MySqlClient() {
			string myConnectionString =
				"Server=" + Properties.Settings.Default.MySqlDbAddress +
				";Database=" + Properties.Settings.Default.MySqlDbName +
				";Uid=" + Properties.Settings.Default.MySqlUid +
				";Pwd=" + Properties.Settings.Default.MySqlPwd;
			connection = new MySqlConnection(myConnectionString);
			IsConnectionOpened();
			queryGetEmployees = Properties.Settings.Default.MySqlQueryGetEmployees;
			queryGetWorkPeriods = Properties.Settings.Default.MySqlQueryGetWorkPeriods;
			nonQueryInsert = Properties.Settings.Default.MySqlNonQueryInsert;
			nonQueryUpdate = Properties.Settings.Default.MySqlNonQueryUpdate;
			nonQueryDelete = Properties.Settings.Default.MySqlNonQueryDelete;
			queryReportMain = Properties.Settings.Default.MySqlQueryReportMain;
			queryReportUsers = Properties.Settings.Default.MySqlQueryReportUsers;
			queryReportOrder = Properties.Settings.Default.MySqlQueryReportOrder;
			random = new Random();
		}

		private bool IsConnectionOpened() {
			if (connection.State != ConnectionState.Open) {
				try {
					connection.Open();
				} catch (Exception e) {
					MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, "Ошибка подключения к базе MySql", 
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			return connection.State == ConnectionState.Open;
		}

		public DataTable GetReport(DateTime dateStart, DateTime dateFinish, List<string> usersIdList = null) {
			string query = queryReportMain;
			string users = string.Empty;

			if (usersIdList != null && usersIdList.Count > 0) {
				query += Environment.NewLine + queryReportUsers;
				users = string.Join(",", usersIdList);
			}

			query += Environment.NewLine + queryReportOrder;

			Dictionary<string, object> parameters = new Dictionary<string, object>() {
				{"@dateStart", dateStart },
				{"@dateFinish", dateFinish }
			};

			if (!string.IsNullOrEmpty(users))
				parameters.Add("@usersIdList", users);
			
			return GetDataTable(query, parameters);
		}

		public void Delete(string id) {
			Dictionary<string, object> parameters = new Dictionary<string, object>() {
				{ "@id", id }
			};

			ExecuteNonQuery(nonQueryDelete, parameters);
		}

		public void Update(string id, DateTime start, DateTime finish) {
			ApplyTimeOffset(ref start, ref finish);

			Dictionary<string, object> parameters = new Dictionary<string, object>() {
				{ "@timestampX", DateTime.Now },
				{ "@dateStart", start },
				{ "@dateFinish", finish },
				{ "@timeStart", start.Hour * 3600 + start.Minute * 60 + start.Second },
				{ "@timeFinish", finish.Hour * 3600 + finish.Minute * 60 + finish.Second },
				{ "@duration", (finish - start).TotalSeconds },
				{ "@id", id }
			};

			ExecuteNonQuery(nonQueryUpdate, parameters);
		}

		public void Insert(string userId, DateTime start, DateTime finish) {
			ApplyTimeOffset(ref start, ref finish);

			Dictionary<string, object> parameters = new Dictionary<string, object>() {
				{ "@timestampX", DateTime.Now },
				{ "@userId", userId },
				{ "@dateStart", start },
				{ "@dateFinish", finish },
				{ "@timeStart", start.Hour * 3600 + start.Minute * 60 + start.Second },
				{ "@timeFinish", finish.Hour * 3600 + finish.Minute * 60 + finish.Second},
				{ "@duration", (finish - start).TotalSeconds },
				{ "@ip", GetLocalIPAddress() }
			};

			ExecuteNonQuery(nonQueryInsert, parameters);
		}

		private void ApplyTimeOffset(ref DateTime start, ref DateTime finish) {
			int minutesOffset = 15;

			if (start.TimeOfDay.TotalMinutes < 15)
				minutesOffset = (int)start.TimeOfDay.TotalMinutes;

			start -= new TimeSpan(0, random.Next(0, minutesOffset), random.Next(0, 59));

			minutesOffset = 15;

			if (finish.TimeOfDay.TotalMinutes > 1440)
				minutesOffset = 1440 - (int)finish.TimeOfDay.TotalMinutes;

			finish += new TimeSpan(0, random.Next(0, minutesOffset), random.Next(0, 59));
		}

		private static string GetLocalIPAddress() {
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
				if (ip.AddressFamily == AddressFamily.InterNetwork)
					return ip.ToString();

			return string.Empty;
		}

		public List<ItemWorkPeriod> GetWorkPeriods(string userId, string date) {
			List<ItemWorkPeriod> workPeriods = new List<ItemWorkPeriod>();

			DataTable dataTable = GetDataTable(queryGetWorkPeriods,
				new Dictionary<string, object> {
					{ "@userId", userId },
					{ "@date", "%" + date + "%" }
				});

			foreach (DataRow row in dataTable.Rows) {
				try {
					string id = row["ID"].ToString();
					string timestampX = row["TIMESTAMP_X"].ToString();
					string dateStart = row["DATE_START"].ToString();
					string dateFinish = row["DATE_FINISH"].ToString();
					string duration = row["DURATION"].ToString();

					ItemWorkPeriod workPeriod = new ItemWorkPeriod() { Id = id };

					if (DateTime.TryParse(timestampX, out DateTime timestamp))
						workPeriod.TimestampX = timestamp;
					if (DateTime.TryParse(dateStart, out DateTime start))
						workPeriod.DateStart = start;
					if (DateTime.TryParse(dateFinish, out DateTime finish))
						workPeriod.DateFinish = finish;
					if (!string.IsNullOrEmpty(duration) &&
						!duration.Equals("0"))
						workPeriod.Duration = TimeSpan.FromSeconds(int.Parse(duration));

					workPeriods.Add(workPeriod);
				} catch (Exception e) {
					MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, "Ошибка разбора строки MySql",
						MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
			}

			return workPeriods;
		}

		public List<ItemEmployee> GetEmployees(string enteredName) {
			List<ItemEmployee> employees = new List<ItemEmployee>();

			DataTable dataTable = GetDataTable(queryGetEmployees, 
				new Dictionary<string, object> { { "@enteredName", enteredName + "%" } });

			foreach (DataRow row in dataTable.Rows) {
				try {
					string id = row["ID"].ToString();
					string fullName = row["FULLNAME"].ToString();
					string company = row["WORK_COMPANY"].ToString();
					string department = row["WORK_DEPARTMENT"].ToString();
					string position = row["WORK_POSITION"].ToString();
					string city = row["WORK_CITY"].ToString();
					
					ItemEmployee itemEmployee = new ItemEmployee() {
						ID = id,
						FullName = fullName,
						Company = company,
						Department = department,
						Position = position,
						City = city
					};

					employees.Add(itemEmployee);
				} catch (Exception e) {
					MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, "Ошибка разбора строки MySql",
						MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
			}
			
			return employees.OrderBy(e => e.FullName).ToList();
		}

		public void CloseConnection() {
			connection.Close();
		}

		private DataTable GetDataTable(string query, Dictionary<string, object> parameters) {
			DataTable dataTable = new DataTable();

			if (!IsConnectionOpened())
				return dataTable;

			try {
				MySqlCommand command = new MySqlCommand(query, connection);
				command.Prepare();

				if (parameters.Count > 0)
					foreach (KeyValuePair<string, object> parameter in parameters)
						command.Parameters.AddWithValue(parameter.Key, parameter.Value);

				MySqlDataAdapter adapter = new MySqlDataAdapter(command);
				adapter.Fill(dataTable);
			} catch (Exception e) {
				MessageBox.Show("GetDataTable exception: " + query +
					Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace,
					"Ошибка получения данных", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			return dataTable;
		}

		private void ExecuteNonQuery(string nonQuery, Dictionary<string, object> parameters) {
			if (!IsConnectionOpened())
				return;

			try {
				MySqlCommand command = new MySqlCommand(nonQuery, connection);
				command.Prepare();

				if (parameters.Count > 0)
					foreach (KeyValuePair<string, object> parameter in parameters)
						command.Parameters.AddWithValue(parameter.Key, parameter.Value);

				command.ExecuteNonQuery();
			} catch (Exception e) {
				MessageBox.Show("ExecuteNonQuery exception: " + nonQuery +
					Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace,
					"Ошибка выполнения команды", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}

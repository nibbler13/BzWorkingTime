using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace BzWorkingTime {
	class NpoiExcel {
		public static string WriteToExcel(DataTable dataTable, BackgroundWorker backgroundWorker,
			double progressFrom, double progressTo) {
			double progressCurrent = progressFrom;

			string templateFile = Environment.CurrentDirectory + "\\Template.xlsx";
			string resultFilePrefix = "Result_";

			backgroundWorker.ReportProgress((int)progressCurrent, "Выгрузка данных в Excel");

			if (!File.Exists(templateFile)) {
				backgroundWorker.ReportProgress((int)progressCurrent);
				return "Не удалось найти файл шаблона: " + templateFile;
			}

			string resultPath = Path.Combine(Environment.CurrentDirectory, "Results");
			if (!Directory.Exists(resultPath))
				Directory.CreateDirectory(resultPath);

			string resultFile = Path.Combine(resultPath, resultFilePrefix + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
			try {
				File.Copy(templateFile, resultFile);
			} catch (Exception e) {
				backgroundWorker.ReportProgress((int)progressCurrent);
				return "Не удалось скопировать файл шаблона в новый файл: " + resultFile + ", " + e.Message;
			}
			
			IWorkbook workbook;
			using (FileStream stream = new FileStream(resultFile, FileMode.Open, FileAccess.Read)) {
				workbook = new XSSFWorkbook(stream);
				stream.Close();
			}

			progressCurrent += 10;
			backgroundWorker.ReportProgress((int)progressCurrent);

			int rowNumber = 1;
			int columnNumber = 0;

			ISheet sheet = workbook.GetSheet("Data");

			double progressStep = (progressTo / 2 - progressCurrent) / dataTable.Rows.Count;

			foreach (DataRow dataRow in dataTable.Rows) {
				backgroundWorker.ReportProgress((int)progressCurrent);
				progressCurrent += progressStep;

				IRow row = sheet.CreateRow(rowNumber);
				foreach (DataColumn dataColumn in dataTable.Columns) {
					if (dataColumn.ColumnName.Equals("DATE_START")) {
						ICell cellDate = row.CreateCell(columnNumber);
						string stringDate = dataRow[dataColumn].ToString();

						if (DateTime.TryParse(stringDate, out DateTime dateTime))
							cellDate.SetCellValue(dateTime.Date.ToOADate());

						columnNumber++;
					}
					
					ICell cell = row.CreateCell(columnNumber);
					string value = dataRow[dataColumn].ToString();

					if (dataColumn.ColumnName.Contains("DATE")) {
						if (DateTime.TryParse(value, out DateTime dateTime))
							cell.SetCellValue(dateTime.TimeOfDay.TotalDays);
					} else if (dataColumn.ColumnName.Equals("DURATION")) {
						if (double.TryParse(value, out double duration))
							cell.SetCellValue(TimeSpan.FromSeconds(duration).TotalDays);
					} else {
						cell.SetCellValue(value);
					}
					
					columnNumber++;
				}

				columnNumber = 0;
				rowNumber++;
			}

			progressStep = (progressTo - progressCurrent) / 9;

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			using (FileStream stream = new FileStream(resultFile, FileMode.Open, FileAccess.Write)) {
				workbook.Write(stream);
				stream.Close();
			}

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			workbook.Close();

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			Excel.Application xlApp = new Excel.Application();

			if (xlApp == null)
				return "Не удалось открыть приложение Excel";

			xlApp.Visible = false;

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			Excel.Workbook wb = xlApp.Workbooks.Open(resultFile);

			if (wb == null)
				return "Не удалось открыть книгу " + resultFile;

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			Excel.Worksheet ws = wb.Sheets["Data"];

			if (ws == null)
				return "Не удалось открыть лист Data";

			try {
				AddPivotTable(wb, ws, xlApp);
			} catch (Exception e) {
				MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
			}

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			wb.Save();

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			wb.Close();

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			xlApp.Quit();

			progressCurrent += progressStep;
			backgroundWorker.ReportProgress((int)progressCurrent);

			return resultFile;
		}

		private static void AddPivotTable(Excel.Workbook wb, Excel.Worksheet ws, Excel.Application xlApp) {
			ws.Columns["A:A"].Select();
			xlApp.Selection.Copy();
			ws.Columns["B:B"].Select();
			xlApp.Selection.Insert(Excel.XlInsertShiftDirection.xlShiftToRight);
			ws.Cells[1, 2].Value = "ФИО";
			ws.Columns[2].ColumnWidth = 1;

			Excel.Range range = ws.Columns["G:G"];
			range.NumberFormat = "m/d/yyyy";
			ws.Columns["H:H"].NumberFormat = "[$-x-systime]ч:мм:сс AM/PM";
			ws.Columns["I:I"].NumberFormat = "[$-x-systime]ч:мм:сс AM/PM";
			ws.Columns["J:J"].NumberFormat = "[ч]:мм:сс;@";

			ws.Cells[1, 1].Select();

			string pivotTableName = @"WorkTimePivotTable";
			Excel.Worksheet wsPivote = wb.Sheets["Pivot"];

			Excel.PivotCache pivotCache = wb.PivotCaches().Create(Excel.XlPivotTableSourceType.xlDatabase, ws.UsedRange, 6);
			Excel.PivotTable pivotTable = pivotCache.CreatePivotTable(wsPivote.Cells[1,1], pivotTableName, true, 6);

			pivotTable = (Excel.PivotTable)wsPivote.PivotTables(pivotTableName);
			
			pivotTable.PivotFields("Город").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Город").Position = 1;

			pivotTable.PivotFields("Компания").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Компания").Position = 2;

			pivotTable.PivotFields("Подразделение").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Подразделение").Position = 3;

			pivotTable.PivotFields("Должность").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Должность").Position = 4;

			pivotTable.PivotFields("Физическое лицо").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Физическое лицо").Position = 5;

			pivotTable.PivotFields("Дата").Orientation = Excel.XlPivotFieldOrientation.xlRowField;
			pivotTable.PivotFields("Дата").Position = 6;
			
			pivotTable.PivotFields("ФИО").Orientation = Excel.XlPivotFieldOrientation.xlPageField;
			pivotTable.PivotFields("ФИО").Position = 1;

			pivotTable.AddDataField(pivotTable.PivotFields("Начало рабочего дня"), "Среднее по полю Начало рабочего дня", Excel.XlConsolidationFunction.xlAverage);
			pivotTable.AddDataField(pivotTable.PivotFields("Окончание рабочего дня"), "Среднее по полю Окончание рабочего дня", Excel.XlConsolidationFunction.xlAverage);
			pivotTable.AddDataField(pivotTable.PivotFields("Длительность"), "Сумма по полю Длительность", Excel.XlConsolidationFunction.xlSum);
			pivotTable.AddDataField(pivotTable.PivotFields("Длительность"), "Среднее по полю Длительность", Excel.XlConsolidationFunction.xlAverage);
			pivotTable.AddDataField(pivotTable.PivotFields("Начало рабочего дня"), "Количество по полю Начало рабочего дня", Excel.XlConsolidationFunction.xlCount);

			pivotTable.PivotFields("Среднее по полю Начало рабочего дня").NumberFormat = "[$-x-systime]ч:мм:сс AM/PM";
			pivotTable.PivotFields("Среднее по полю Окончание рабочего дня").NumberFormat = "[$-x-systime]ч:мм:сс AM/PM";
			pivotTable.PivotFields("Сумма по полю Длительность").NumberFormat = "[ч]:мм:сс;@";
			pivotTable.PivotFields("Среднее по полю Длительность").NumberFormat = "[$-x-systime]ч:мм:сс AM/PM";
			
			pivotTable.PivotFields("Город").ShowDetail = false;
			pivotTable.PivotFields("Компания").ShowDetail = false;
			pivotTable.PivotFields("Подразделение").ShowDetail = false;
			pivotTable.PivotFields("Должность").ShowDetail = false;
			pivotTable.PivotFields("Физическое лицо").ShowDetail = false;

			pivotTable.HasAutoFormat = false;

			wsPivote.Columns[1].ColumnWidth = 80;
			wsPivote.Columns[2].ColumnWidth = 15;
			wsPivote.Columns[3].ColumnWidth = 15;
			wsPivote.Columns[4].ColumnWidth = 15;
			wsPivote.Columns[5].ColumnWidth = 15;
			wsPivote.Columns[6].ColumnWidth = 15;
			wsPivote.Rows[3].WrapText = true;
			wsPivote.Activate();

			wb.ShowPivotTableFieldList = false;
		}
	}
}

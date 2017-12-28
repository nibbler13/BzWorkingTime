using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BzWorkingTime {
	public class ItemWorkPeriod {
		public string ID { get; set; }
		private string empty = "---";

		public DateTime? TimestampX { get; set; }
		public string ModificationTime {
			get {
				if (TimestampX == null) {
					return empty;
				} else {
					return TimestampX.Value.ToString();
				}
			}
		}

		public DateTime? DateStart { get; set; }
		public string Start {
			get {
				if (DateStart == null) {
					return empty;
				} else {
					if (string.IsNullOrEmpty(ID))
						return empty;

					return DateStart.Value.ToLongTimeString();
				}
			}
		}

		public DateTime? DateFinish { get; set; }
		public string Finish {
			get {
				if (DateFinish == null) {
					return empty;
				} else {
					if (DateFinish.Value.Date == DateStart.Value.Date)
						return (DateFinish.Value.ToLongTimeString());
					return DateFinish.Value.ToString();
				}
			}
		}

		public TimeSpan? Duration { get; set; }
		public string DurationTime {
			get {
				if (Duration == null) {
					return empty;
				} else {
					return Duration.Value.ToString();
				}
			}
		}

		public string DayOfWeek {
			get {
				if (Date == null) return empty;
				return DateTimeFormatInfo.CurrentInfo.GetDayName(Date.DayOfWeek);
			}
		}

		public DateTime Date { get; set; }
		public string DateString {
			get {
				return Date.ToShortDateString();
			}
		}

		public ItemWorkPeriod(DateTime date) {
			Date = date;
		}
	}
}

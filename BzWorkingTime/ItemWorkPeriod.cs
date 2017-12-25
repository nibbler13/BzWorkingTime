using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BzWorkingTime {
	public class ItemWorkPeriod {
		public string Id { get; set; }
		private string empty = "---";

		public DateTime? TimestampX { get; set; }
		public string ModificationTime {
			get {
				if (TimestampX == null) {
					return empty;
				} else {
					return ((DateTime)TimestampX).ToString();
				}
			}
		}

		public DateTime? DateStart { get; set; }
		public string Start {
			get {
				if (DateStart == null) {
					return empty;
				} else {
					return ((DateTime)DateStart).ToString("HH:mm:ss");
				}
			}
		}

		public DateTime? DateFinish { get; set; }
		public string Finish {
			get {
				if (DateFinish == null) {
					return empty;
				} else {
					return ((DateTime)DateFinish).ToString("HH:mm:ss");
				}
			}
		}

		public TimeSpan? Duration { get; set; }
		public string DurationTime {
			get {
				if (Duration == null) {
					return empty;
				} else {
					return ((TimeSpan)Duration).ToString();
				}
			}
		}

		public ItemWorkPeriod() {

		}
	}
}

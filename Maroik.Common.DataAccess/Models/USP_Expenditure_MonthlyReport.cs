using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Models
{
    public class USP_Expenditure_MonthlyReport
    {
        public string MainClass { get; set; }
        public long? CountValue { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public decimal? MeanValue { get; set; }
        public double? MedianValue { get; set; }
        public long? RangeValue { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Variance { get; set; }
        public double? Percentile25 { get; set; }
        public double? Percentile50 { get; set; }
        public double? Percentile75 { get; set; }
        public double? Skewness { get; set; }
        public double? Kurtosis { get; set; }
        public long? ModeValue { get; set; }
        public long? ModeCount { get; set; }
        public decimal? OverConsumptionIndex { get; set; }
    }
}

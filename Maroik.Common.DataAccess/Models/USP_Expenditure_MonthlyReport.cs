namespace Maroik.Common.DataAccess.Models
{
    public class USP_Expenditure_MonthlyReport
    {
        public string MainClass { get; set; }
        public decimal? SumValue { get; set; }
        public long? CountValue { get; set; }
        public decimal? ModeValue { get; set; }
        public long? ModeCount { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? RangeValue { get; set; }
        public decimal? MeanValue { get; set; }
        public double? Variance { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Q1 { get; set; }
        public double? Q2 { get; set; }
        public double? Q3 { get; set; }
        public double? Skewness { get; set; }
        public double? Kurtosis { get; set; }
        public decimal? OverConsumptionIndex { get; set; }
    }
}

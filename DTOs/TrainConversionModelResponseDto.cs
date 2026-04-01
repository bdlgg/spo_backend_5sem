namespace backend_spo.DTOs;

public class TrainConversionModelResponseDto
{
    public int DataRows { get; set; }
    public double Accuracy { get; set; }
    public double Auc { get; set; }
    public double F1Score { get; set; }
    public double PositivePrecision { get; set; }
    public double PositiveRecall { get; set; }
    public string ModelFile { get; set; } = string.Empty;
}
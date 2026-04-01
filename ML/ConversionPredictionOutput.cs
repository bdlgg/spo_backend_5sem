using Microsoft.ML.Data;

namespace backend_spo.ML;

public class ConversionPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
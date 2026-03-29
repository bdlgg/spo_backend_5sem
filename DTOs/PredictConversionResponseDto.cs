namespace backend_spo.DTOs;

public class PredictConversionResponseDto
{
    public float Probability { get; set; }
    public string Level {get; set;} = string.Empty;
    public string Explanation {get; set;} = string.Empty;
}
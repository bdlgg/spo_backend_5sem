using Microsoft.ML.Data;

namespace backend_spo.ML;

public class CarLeadCsvRow
{
    [LoadColumn(0)] public string LeadId { get; set; } = string.Empty;
    [LoadColumn(1)] public string CreatedAt { get; set; } = string.Empty;
    [LoadColumn(2)] public string SourceChannel { get; set; } = string.Empty;
    [LoadColumn(3)] public string CarBrand { get; set; } = string.Empty;
    [LoadColumn(4)] public string CarModel { get; set; } = string.Empty;
    [LoadColumn(5)] public string CarCategory { get; set; } = string.Empty;
    [LoadColumn(6)] public float PriceAtLead { get; set; }
    [LoadColumn(7)] public float IsNewCar { get; set; }
    [LoadColumn(8)] public string ClientCity { get; set; } = string.Empty;
    [LoadColumn(9)] public float ContactCount { get; set; }
    [LoadColumn(10)] public float HadTestDrive { get; set; }
    [LoadColumn(11)] public float InterestedInCredit { get; set; }
    [LoadColumn(12)] public float InterestedInTradeIn { get; set; }
    [LoadColumn(13)] public string LeadStatusBeforeFinal { get; set; } = string.Empty;
    [LoadColumn(14)] public float DaysToDecision { get; set; }
    [LoadColumn(15)] public string ManagerId { get; set; } = string.Empty;
    [LoadColumn(16)] public string ClientAgeGroup { get; set; } = string.Empty;
    [LoadColumn(17)] public string PreferredTransmission { get; set; } = string.Empty;
    [LoadColumn(18)] public string PreferredFuelType { get; set; } = string.Empty;
    [LoadColumn(19)] public string MileageRange { get; set; } = string.Empty;
    [LoadColumn(20)] public float VisitToSalon { get; set; }
    [LoadColumn(21)] public float NumberOfViewedCars { get; set; }
    [LoadColumn(22)] public float DiscountOffered { get; set; }
    [LoadColumn(23)] public float InitialBudget { get; set; }
    [LoadColumn(24)] public string MonthlyIncomeRange { get; set; } = string.Empty;
    [LoadColumn(25)] public float IsConverted { get; set; }
}
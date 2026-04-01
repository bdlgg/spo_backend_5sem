namespace backend_spo.ML;

public class ConversionModelInput
{
    public string SourceChannel { get; set; } = "unknown";
    public string CarBrand { get; set; } = "unknown";
    public string CarModel { get; set; } = "unknown";
    public string CarCategory { get; set; } = "unknown";

    public float PriceAtLead { get; set; }
    public float IsNewCar { get; set; }

    public string ClientCity { get; set; } = "unknown";

    public float ContactCount { get; set; }
    public float HadTestDrive { get; set; }
    public float InterestedInCredit { get; set; }
    public float InterestedInTradeIn { get; set; }

    public string ClientAgeGroup { get; set; } = "unknown";
    public string PreferredTransmission { get; set; } = "unknown";
    public string PreferredFuelType { get; set; } = "unknown";
    public string MileageRange { get; set; } = "unknown";

    public float VisitToSalon { get; set; }
    public float NumberOfViewedCars { get; set; }
    public float DiscountOffered { get; set; }
    public float InitialBudget { get; set; }
    public string MonthlyIncomeRange { get; set; } = "unknown";

    public float LeadMonth { get; set; }

    public bool Label { get; set; }
}
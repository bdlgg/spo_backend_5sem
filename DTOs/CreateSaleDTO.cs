namespace backend_spo.DTOs
{
	public class CreateSaleDto
	{
		public int CarId { get; set; }
		public int ClientId { get; set; }
		public int UserId { get; set; }
		public decimal FinalPrice { get; set; }
		public string Status { get; set; } = string.Empty;
	}
}
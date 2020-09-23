namespace TaskerTimer.TgClient.Models
{
#pragma warning disable IDE1006 // Naming Styles
	public class TgResult<T>
		where T: notnull
	{
		public bool ok { get; set; }
		public int error_code { get; set; }
		public string description { get; set; }
		public T result { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}

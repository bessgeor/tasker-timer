namespace TaskerTimer.TgClient.Models
{
#pragma warning disable IDE1006 // Naming Styles
	public class CallbackQuery
	{
		public string id { get; set; }
		public string inline_message_id { get; set; }
		public Message message { get; set; }
		public string data { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}

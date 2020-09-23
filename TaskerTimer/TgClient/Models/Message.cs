using System;
using Newtonsoft.Json;

namespace TaskerTimer.TgClient.Models
{
#pragma warning disable IDE1006 // Naming Styles
	public class Message
	{
		public string message_id { get; set; }
		public int date { get; set; }
		[JsonIgnore]
		public DateTime Date => new DateTime( 1970, 1, 1, 0, 0, 0 ).AddSeconds( date );
		public Chat chat { get; set; }
		public string text { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}

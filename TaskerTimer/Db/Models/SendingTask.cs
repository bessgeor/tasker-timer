namespace TaskerTimer.Db.Models
{
	public readonly struct SendingTask
	{
		public SendingTask( int id, string chatId, bool isHtml, string message )
		{
			Id = id;
			ChatId = chatId;
			IsHtml = isHtml;
			Message = message;
		}

		public int Id { get; }
		public string ChatId { get; }
		public bool IsHtml { get; }
		public string Message { get; }
	}
}

namespace TaskerTimer.Db.Models
{
	public readonly struct SendingTask
	{
		public SendingTask( int id, string chatId, bool isHtml, string message, bool isFirst )
		{
			Id = id;
			ChatId = chatId;
			IsHtml = isHtml;
			Message = message;
			IsFirst = isFirst;
		}

		public int Id { get; }
		public string ChatId { get; }
		public bool IsHtml { get; }
		public string Message { get; }
		public bool IsFirst { get; }
	}
}

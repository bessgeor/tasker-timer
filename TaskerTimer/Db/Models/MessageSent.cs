using System;

namespace TaskerTimer.Db.Models
{
	public readonly struct MessageSent
	{
		public MessageSent( int sendingTaskId, string chatId, string messageId, DateTime sentAt )
		{
			SendingTaskId = sendingTaskId;
			ChatId = chatId;
			MessageId = messageId;
			SentAt = sentAt;
		}

		public int SendingTaskId { get; }
		public string ChatId { get; }
		public string MessageId { get; }
		public DateTime SentAt { get; }

		public void Deconstruct( out int sendingTaskId, out string chatId, out string messageId )
			=> (sendingTaskId, chatId, messageId) = (SendingTaskId, ChatId, MessageId);
	}
}

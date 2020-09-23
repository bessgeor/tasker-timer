using System;
using System.Collections.Concurrent;
using System.Text;

namespace TaskerTimer.TgClient
{
	public static class Urls
	{
		private static readonly ConcurrentBag<StringBuilder> _sbPool = new ConcurrentBag<StringBuilder>();

		private const string _messageIdParam = "message_id=";
		private const string _chatIdParam = "chat_id=";
		private const string _textParam = "text=";

		private static StringBuilder GetBaseUrl( string method )
		{
			const string baseUrl = "https://api.telegram.org/bot";
			if ( !_sbPool.TryTake( out var sb ) )
				sb = new StringBuilder( 50 );
			return sb
				.Append( baseUrl )
				.Append( EnvironmentSettings.BotToken )
				.Append( '/' )
				.Append( method )
			;
		}

		private static string MaterializeAndReturnToPool( StringBuilder sb )
		{
			var result = sb.ToString();
			sb.Clear();
			_sbPool.Add( sb );
			return result;
		}

		public static string GetSendMesage( string chatId, string text, bool isHtml, int? callbackId, string replyToMessageId )
		{
			var sb = GetBaseUrl( "sendMessage" )
				.Append( '?' )
				.Append( _chatIdParam )
				.Append( chatId )
				.Append( '&' )
				.Append( _textParam )
				.Append( Uri.EscapeDataString( text ) )
			;
			if ( callbackId.HasValue )
				sb = sb.Append( "&reply_markup={%22inline_keyboard%22:[[{%22text%22:%22Готово!%22,%22callback_data%22:" ).Append( callbackId.Value ).Append( "}]]}" );
			if ( isHtml )
				sb = sb.Append( "&parse_mode=HTML" );
			if ( replyToMessageId != null )
				sb = sb.Append( "&reply_to_message_id=" ).Append( replyToMessageId );
			var result = MaterializeAndReturnToPool( sb );
			return result;
		}

		public static string GetUpdateMessageAndDropKeyboard( string messageId, string chatId, string newText, bool isHtml )
		{
			var sb = GetBaseUrl( "editMessageText" )
				.Append( '?' )
				.Append( _messageIdParam )
				.Append( messageId )
				.Append( '&' )
				.Append( _chatIdParam )
				.Append( chatId )
				.Append( '&' )
				.Append( _textParam )
				.Append( Uri.EscapeDataString( newText ) )
			;
			if ( isHtml )
				sb = sb.Append( "&parse_mode=HTML" );
			return MaterializeAndReturnToPool( sb );
		}

		public static string GetDropKeyboard( string messageId, string chatId )
		{
			var sb = GetBaseUrl( "editMessageReplyMarkup" )
				.Append( '?' )
				.Append( _messageIdParam )
				.Append( messageId )
				.Append( '&' )
				.Append( _chatIdParam )
				.Append( chatId )
			;
			return MaterializeAndReturnToPool( sb );
		}

		public static string GetDeleteMessage( string messageId, string chatId )
		{
			var sb = GetBaseUrl( "deleteMessage" )
				.Append( '?' )
				.Append( _messageIdParam )
				.Append( messageId )
				.Append( '&' )
				.Append( _chatIdParam )
				.Append( chatId )
			;
			return MaterializeAndReturnToPool( sb );
		}

		public static string GetAnswerInlineMessage( string queryId )
		{
			const string queryIdParam = "callback_query_id=";
			var sb = GetBaseUrl( "answerCallbackQuery" )
				.Append( '?' )
				.Append( queryIdParam )
				.Append( queryId )
			;
			return MaterializeAndReturnToPool( sb );
		}
	}
}

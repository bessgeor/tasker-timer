using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TaskerTimer.TgClient;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer
{
	internal static class Telegram
	{
		private static async Task<string> SendAsync<T>( string command, ILogger logger, T errorContext, Func<T, string> getErrorPrefix, CancellationToken cancellation )
		{
			try
			{
				logger.LogInformation( command );
				using var client = new HttpClient();
				using var message = new HttpRequestMessage( HttpMethod.Get, command );
				using var response = await client.SendAsync( message, HttpCompletionOption.ResponseHeadersRead, cancellation ).ConfigureAwait( false );
				if ( !response.IsSuccessStatusCode )
				{
					logger.LogError( $"{getErrorPrefix( errorContext )}: {response.StatusCode.ToString()} {response.ReasonPhrase}" );
					var body = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
					logger.LogError( body );
					return body;
				}
				else
				{
					logger.LogInformation( "success status" );
					var body = await response.Content.ReadAsStringAsync().ConfigureAwait( false );
					logger.LogInformation( body );
					return body;
				}
			}
			catch ( Exception e ) when ( Log( e ) )
			{
				return null;
			}
			bool Log( Exception e )
			{
				logger.LogError( $"{e.GetType().Name}: {e.Message}" );
				return true;
			}
		}

		public static async Task<Message> SendMessageAsync
		(
			string chatId,
			bool isHtml,
			string text,
			int? taskId,
			ILogger logger,
			CancellationToken cancellation,
			string replyToMessageId = null
		)
		{
			var url = Urls.GetSendMesage( chatId, text, isHtml, taskId, replyToMessageId );
			var resp = await SendAsync( url, logger, chatId, id => $"chat id {id}", cancellation ).ConfigureAwait( false );
			var response = JsonConvert.DeserializeObject<TgResult<Message>>( resp );
			if ( !response.ok )
				logger.LogError( "tg non-success" );
			return response.result;
		}

		public static Task AnswerCallbackAsync( CallbackQuery query, ILogger logger, CancellationToken cancellation )
			=> SendAsync( Urls.GetAnswerInlineMessage( query.id ), logger, query, q => $"answer {q.id}", cancellation );

		public static Task DropKeyboardAsync( string messageId, string chatId, ILogger logger, CancellationToken cancellation )
			=> SendAsync( Urls.GetDropKeyboard( messageId, chatId ), logger, messageId, q => $"drop keyboard {q}", cancellation );

		public static async Task<TgResult<bool>> DeleteMessageAsync( string messageId, string chatId, ILogger logger, CancellationToken cancellation )
		{
			var url = Urls.GetDeleteMessage( messageId, chatId );
			var resp = await SendAsync( url, logger, messageId, q => $"delete message {q}", cancellation ).ConfigureAwait( false );
			var response = JsonConvert.DeserializeObject<TgResult<bool>>( resp );
			return response;
		}

		public static Task UpdateMessageAndDropKeyboardAsync( string messageId, string chatId, string newText, bool isHtml, ILogger logger, CancellationToken cancellation )
			=> SendAsync( Urls.GetUpdateMessageAndDropKeyboard( messageId, chatId, newText, isHtml ), logger, messageId, m => $"update & drop KB {m}", cancellation );
	}
}

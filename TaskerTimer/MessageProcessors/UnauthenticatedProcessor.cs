using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	internal sealed class UnauthenticatedProcessor : IMessageProcessor
	{
		public bool IsExclusive => true;
		public bool RequiresAuthentication => false;

		public bool CanProcess( Message message ) => message.text.StartsWith( "/" );

		public async Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			var chatId = message?.chat?.id ?? throw new ArgumentNullException( nameof( message ) );
			var response = $"Sorry, but you are not authenticated.\nuser id: <code>{message.from.id}</code>\n<code>{message.from.username}</code>";
			var m = await Telegram.SendMessageAsync( chatId, isHtml: true, response, taskId: null, logger, cancellation, message.message_id ).ConfigureAwait( false );
			logger.LogInformation( $"sent message {m.message_id}" );
		}
	}
}

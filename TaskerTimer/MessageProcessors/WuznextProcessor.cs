using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	internal sealed class WuznextProcessor : CommandProcessor
	{
		protected override string Command => "/wuznext";

		public override async Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			var chatId = message?.chat?.id ?? throw new ArgumentNullException( nameof( message ) );
			var response = await Db.Client.GetCurrentTasksTextForChatAsync( chatId, limit: 1, cancellation ).ConfigureAwait( false );
			var m = await Telegram.SendMessageAsync( chatId, isHtml: true, response, taskId: null, logger, cancellation, message.message_id ).ConfigureAwait( false );
			logger.LogInformation( $"sent message {m.message_id}" );
		}
	}
}

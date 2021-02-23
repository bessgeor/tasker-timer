using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	internal abstract class MutesAbstractProcessor : CommandProcessor
	{
		protected abstract bool ShouldMute { get; }
		protected abstract Func<string, bool, CancellationToken, Task<int>> DbExecutor { get; }

		protected abstract string LogDescription { get; }

		public override async Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			var chatId = message?.chat?.id ?? throw new ArgumentNullException( nameof( message ) );
			logger.LogInformation( "{0} for chat id {1}", LogDescription, chatId );
			var response = await DbExecutor(chatId, ShouldMute, cancellation).ConfigureAwait( false );
			var responseMessage = $"{LogDescription} done for {response} tasks";
			var m = await Telegram.SendMessageAsync( chatId, isHtml: false, responseMessage, taskId: null, logger, cancellation, message.message_id ).ConfigureAwait( false );
			logger.LogInformation( $"sent message {m.message_id}" );
		}
	}
}

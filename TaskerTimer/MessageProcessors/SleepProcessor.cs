using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	class SleepProcessor : CommandProcessor
	{
		protected override string Command => "/sleep";

		public override async Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			var chatId = message?.chat?.id ?? throw new ArgumentNullException( nameof( message ) );
			await Db.Client.DeleteTasksScheduledForIntervalAsync().ConfigureAwait( true );
		}
	}
}

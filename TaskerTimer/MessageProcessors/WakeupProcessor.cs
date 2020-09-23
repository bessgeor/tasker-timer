using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	class WakeupProcessor : CommandProcessor
	{
		protected override string Command => "/wakeup";

		public override async Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			var chatId = message?.chat?.id ?? throw new ArgumentNullException( nameof( message ) );
			await Db.Client.CreateTasksScheduledForIntervalAsync().ConfigureAwait( true );
		}
	}
}

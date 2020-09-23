using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer.MessageProcessors
{
	internal abstract class CommandProcessor : IMessageProcessor
	{
		public bool CanProcess( Message message ) => message?.text?.StartsWith( Command ) == true;

		protected abstract string Command { get; }
		public abstract Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation );
	}
}

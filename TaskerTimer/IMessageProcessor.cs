using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer
{
	internal interface IMessageProcessor
	{
		bool IsExclusive { get; }
		bool RequiresAuthentication { get; }
		bool CanProcess( Message message );
		Task ProcessAsync( Message message, ILogger logger, CancellationToken cancellation );
	}
}

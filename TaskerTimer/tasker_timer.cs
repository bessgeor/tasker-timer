using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TaskerTimer
{
	public static class TaskerTimer
	{
		[FunctionName( "tasker_timer" )]
#pragma warning disable IDE0060 // Remove unused parameter
		public static async Task RunAsync( [TimerTrigger( "0 */1 * * * *" )] TimerInfo myTimer, ILogger log )
		{
			log = LogDisabler.GetLogger( log );
			await SchedulingLogic.DoSchedulingAsync( log ).ConfigureAwait( true );
			await SchedulingLogic.ClearAllRepeatedMessagesAsync( log, default ).ConfigureAwait( true );
		}
	}
}

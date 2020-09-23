using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TaskerTimer
{
	public static class TaskerGenerateCurrentTasks
	{
		[FunctionName( "tasker_generate_current_tasks" )]
#pragma warning disable IDE0060 // Remove unused parameter
		public static async Task RunAsync( [TimerTrigger( "0 0 0/24 * * *" )]TimerInfo myTimer, ILogger log )
#pragma warning restore IDE0060 // Remove unused parameter
			=> await Db.Client.CreateTasksScheduledForIntervalAsync( System.TimeSpan.FromDays( 1 ) ).ConfigureAwait( true );
	}
}

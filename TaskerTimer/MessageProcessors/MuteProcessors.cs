using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskerTimer.MessageProcessors
{
	internal sealed class MuteTodayProcessor : MutesAbstractProcessor
	{
		protected override string Command => "/mute_today";
		protected override bool ShouldMute => true;
		protected override string LogDescription => "Muting today tasks";
		protected override Func<string, bool, CancellationToken, Task<int>> DbExecutor { get; } = Db.Client.MuteTodayTasksAsync;
	}
	internal sealed class UnmuteTodayProcessor : MutesAbstractProcessor
	{
		protected override string Command => "/unmute_today";
		protected override bool ShouldMute => false;
		protected override string LogDescription => "Unmuting today tasks";
		protected override Func<string, bool, CancellationToken, Task<int>> DbExecutor { get; } = Db.Client.MuteTodayTasksAsync;
	}
	internal sealed class MuteProcessor : MutesAbstractProcessor
	{
		protected override string Command => "/mute";
		protected override bool ShouldMute => true;
		protected override string LogDescription => "Muting all tasks";
		protected override Func<string, bool, CancellationToken, Task<int>> DbExecutor { get; } = Db.Client.MuteAllTasksAsync;
	}
	internal sealed class UnmuteProcessor : MutesAbstractProcessor
	{
		protected override string Command => "/unmute";
		protected override bool ShouldMute => false;
		protected override string LogDescription => "Unmuting all tasks";
		protected override Func<string, bool, CancellationToken, Task<int>> DbExecutor { get; } = Db.Client.MuteAllTasksAsync;
	}
}

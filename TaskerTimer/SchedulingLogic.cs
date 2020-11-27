using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.Db.Models;

namespace TaskerTimer
{
	internal static class SchedulingLogic
	{
		public static async Task DoneTaskAsync( int sendingTaskId, TgClient.Models.CallbackQuery callback, ILogger log, CancellationToken token )
		{
			var doneResult = await Db.Client.DoneTaskAsync( sendingTaskId, token ).ConfigureAwait( false );

			await Telegram.AnswerCallbackAsync( callback, log, token ).ConfigureAwait( false );

			if ( !doneResult.HasValue )
			{
				await Telegram.DeleteMessageAsync( callback.message.message_id, callback.message.chat.id, log, token ).ConfigureAwait( false );
				return;
			}

			var (toUpdate, count) = doneResult.Value;

			var (task_id, chat_id, tg_message_id) = toUpdate;

			if ( count == 1 )
				await Telegram.DropKeyboardAsync( tg_message_id, chat_id, log, token ).ConfigureAwait( false );
			else
			{
				var oldText = await Db.Client.GetTaskMessageAsync( task_id, token ).ConfigureAwait( false );

				var end =
					( count % 10 ) switch
					{
						1 => "е",
						int n when n > 1 && n < 5 => "я",
						_ => "й"
					};
				var newText = $"{oldText}\n\n<i>понадобилось всего {count} напоминани{end}</i>";
				await Telegram.UpdateMessageAndDropKeyboardAsync( tg_message_id, chat_id, newText, isHtml: true, log, token ).ConfigureAwait( false );
			}

			await ClearRepeatedMessagesAsync( task_id, log, token ).ConfigureAwait( false );
		}

		public static async Task ClearAllRepeatedMessagesAsync( ILogger logger, CancellationToken token )
		{
			logger.LogInformation( "going to clear message repeats" );
			using var ctsInnerTimeout = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );
			using var cts = CancellationTokenSource.CreateLinkedTokenSource( ctsInnerTimeout.Token, token );
			var deletedCount = 0;
			do
			{
				deletedCount = await ClearRepeatedMessagesAsync( relatedSendingTaskId: null, logger, cts.Token ).ConfigureAwait( false );
				logger.LogInformation( $"cleared {deletedCount.ToString()}" );
				if ( deletedCount > 0 )
					await Task.Delay( millisecondsDelay: 500, cts.Token ).ConfigureAwait( false );
			}
			while ( !cts.Token.IsCancellationRequested && deletedCount > 0 );
			if ( cts.Token.IsCancellationRequested )
				logger.LogWarning( "canceled" );
		}

		public static async Task<int> ClearRepeatedMessagesAsync( int? relatedSendingTaskId, ILogger logger, CancellationToken token )
		{
			var tasks = await Db.Client.GetMessagesToDeleteAsync( maxCount: 100, relatedSendingTaskId, token ).ConfigureAwait( false );

			logger.LogInformation( $"found {tasks.Count.ToString()} deletion tasks" );

			if ( tasks.Count == 0 )
				return 0;

			var deletions = await Task
				.WhenAll( tasks.Select( t => DeleteMessageAsync( t.chatId, t.messageId, t.taskId ) ) )
				.ConfigureAwait( false )
			;

			var succeededDeletions = deletions
				.Where( v => v.deletionSucceeded )
				.Select( v => v.taskId )
				.ToArray()
			;

			await Db.Client.ClearDeletionTasksAsync( succeededDeletions, token ).ConfigureAwait( false );

			return tasks.Count;

			async Task<(bool deletionSucceeded, int taskId)> DeleteMessageAsync( string chatId, string messageId, int taskId )
			{
				var response = await Telegram
					.DeleteMessageAsync( messageId, chatId, logger, token )
					.ConfigureAwait( false );
				var shouldCountDeletionAsSuccessfull = response.ok && response.result
					|| response.error_code == 400 && response.description.Equals( "Bad Request: message to delete not found", StringComparison.Ordinal )
					|| response.error_code == 400 && response.description.Equals( "Bad Request: message can't be deleted", StringComparison.Ordinal )
				;
				if (!shouldCountDeletionAsSuccessfull )
					logger.LogWarning( $"unsuccessful deletion id: {taskId.ToString()}" );
			return (shouldCountDeletionAsSuccessfull, taskId);
			}
		}

		public static async Task DoSchedulingAsync( ILogger log )
		{
			var now = DateTime.UtcNow;
			now = now.AddSeconds( -now.Second ).AddMilliseconds( -now.Millisecond );

			var results = new List<(int taskId, string messageId)>();

			// TODO: simplify with IAsyncEnumerable + ChunkBy
			var hotTasks = new Task[ 10 ];
			Array.Fill( hotTasks, Task.CompletedTask );
			var coldTasks = new Queue<((ILogger, SendingTask), Func<(ILogger, SendingTask), Task>)>();

			var tasks = await Db.Client.CreateSendingMessagesForCurrentMinuteAsync( now ).ConfigureAwait( false );
			log.LogInformation( $"downloaded {tasks.Count.ToString()} tasks" );

			if ( tasks.Count == 0 )
				return;

			foreach ( var task in tasks )
				HandleTaskAdd( (log, task), v => SendMessageAsync( v ) );

			while ( coldTasks.Count > 0 )
			{
				var hasFreeSlots = false;
				for ( var i = 0; i < hotTasks.Length; i++ )
					if ( hotTasks[ i ].IsCompleted )
					{
						var (deps, cold) = coldTasks.Dequeue();
						hotTasks[ i ] = cold( deps );
						hasFreeSlots = true;
						break;
					}
				if ( !hasFreeSlots )
					await Task.WhenAny( hotTasks ).ConfigureAwait( false );
			}
			await Task.WhenAll( hotTasks ).ConfigureAwait( false );

			log.LogInformation( $"results count: {results.Count}" );
			if ( results.Count == 0 )
				return;

			void HandleTaskAdd( (ILogger, SendingTask) deps, Func<(ILogger, SendingTask), Task> cold )
			{
				var madeHot = false;
				for ( var i = 0; i < hotTasks.Length; i++ )
					if ( hotTasks[ i ].IsCompleted )
					{
						hotTasks[ i ] = cold( deps );
						madeHot = true;
						break;
					}
				if ( !madeHot )
					coldTasks.Enqueue( (deps, cold) );
			}

			static async Task SendMessageAsync( (ILogger, SendingTask) deps )
			{
				var (log, message) = deps;

				if ( message.IsFirst )
				{
					var chatId = message.ChatId;
					var response = await Db.Client.GetCurrentTasksTextForChatAsync( chatId, limit: 100, CancellationToken.None ).ConfigureAwait( false );
					var m = await Telegram.SendMessageAsync( chatId, isHtml: true, response, taskId: null, log, CancellationToken.None ).ConfigureAwait( false );
					log.LogInformation( $"sent overview message {m.message_id}" );
				}

				var msg = await Telegram
					.SendMessageAsync( message.ChatId, message.IsHtml, message.Message, message.Id, log, CancellationToken.None )
					.ConfigureAwait( false )
				;
				log.LogInformation( $"message sent succesfully. {( msg is null ? "unparsed" : msg.message_id is null ? "message id is null" : msg.message_id )}" );

				if ( msg == null || msg.message_id == null )
					return;

				await Db.Client.SaveTelegramMessageSentAsync( new MessageSent( message.Id, message.ChatId, msg.message_id, msg.Date ) ).ConfigureAwait( false );
			}
		}
	}
}

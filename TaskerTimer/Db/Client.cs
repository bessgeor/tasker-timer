using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Npgsql;
using TaskerTimer.Db.Models;

namespace TaskerTimer.Db
{
	internal static class Client
	{
		private readonly struct CloseOnDisposeConnection : IDisposable
		{
			private readonly NpgsqlConnection _connection;

			public CloseOnDisposeConnection( NpgsqlConnection connection ) => _connection = connection;

			public NpgsqlCommand CreateCommand() => _connection.CreateCommand();

			public void Dispose()
			{
				_connection.Close();
				_connection.Dispose();
			}
		}

		private static async Task<CloseOnDisposeConnection> GetConnectionAsync( CancellationToken cancellationToken = default )
		{
			var connection = new NpgsqlConnection( EnvironmentSettings.ConnectionString );
			using ( var own = new CancellationTokenSource( TimeSpan.FromSeconds( 5 ) ) )
			using ( var outer = CancellationTokenSource.CreateLinkedTokenSource( own.Token, cancellationToken ) )
				await connection.OpenAsync( outer.Token ).ConfigureAwait( false );
			return new CloseOnDisposeConnection( connection );
		}

		private const string _getCurrentTasksSql = @"
select title, send_at
from current_tasks
where not is_sending_created
and chat_id = @chat_id
order by send_at
limit @limit
";

		public static async Task<string> GetCurrentTasksTextForChatAsync( string chatId, int limit, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var comm = conn.CreateCommand();
			comm.CommandText = _getCurrentTasksSql;
			comm.Parameters.Add( new NpgsqlParameter( "chat_id", chatId ) );
			comm.Parameters.Add( new NpgsqlParameter( "limit", limit ) );

			var result = new StringBuilder();

			using var reader = await comm.ExecuteReaderAsync( token ).ConfigureAwait( false );
			while ( await reader.ReadAsync( token ).ConfigureAwait( false ) )
			{
				var title = reader.GetString( 0 );
				var sendAt = reader.GetDateTime( 1 );
				result
					.Append( "<b>" )
					.Append( sendAt.AddHours( 3 ).ToString( "HH:mm" ) )
					.Append( "</b> — " )
					.Append( title )
					.Append( '\n' );
			}
			return result.ToString();
		}


		private const string _isAuthenticatedSql = @"
select count(*) = 1 from allowed_users where id = @id and username = @username
";

		public static async Task<bool> IsAuthenticatedAsync( long userId, string username, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var comm = conn.CreateCommand();
			comm.CommandText = _isAuthenticatedSql;
			comm.Parameters.Add( new NpgsqlParameter( "id", userId ) );
			comm.Parameters.Add( new NpgsqlParameter( "username", username ) );

			using var reader = await comm.ExecuteReaderAsync( token ).ConfigureAwait( false );
			while ( await reader.ReadAsync( token ).ConfigureAwait( false ) )
				return reader.GetBoolean( 0 );
			return false;
		}

		private const string _createTodayTasksSql = @"
delete from current_tasks where send_at < @current_date and not exists (select 1 from sending_tasks where task_to_send = id);
with today as (
	select chat_id, task_name, message, is_html, starts_at
	from scheduled_tasks
	where
	starts_at < (@current_date + @creation_interval)
	and
	(
		extract( epoch from (@current_date + @creation_interval) - starts_at )::int
		%
		extract( epoch from period )::int
		<
		extract( epoch from @creation_interval )::int
	)
	order by starts_at::time
)
, tmp as (
	select
	(select today.task_name from today where starts_at::time = '17:10') as lang,
	today.*
	from today order by starts_at::time limit 1
)
, lang as (
	select chat_id,
	case lang
		when 'Злоебучий диплом' then 'Сегодня мы говорим по-русски'
		when 'Svenska Språk' then 'idag talar vi Svenska'
		when 'Deutsche Sprache' then 'heute sprechen wir Deutsch'
		when 'English Language' then 'we speak English today'
		else lang
	end task_name,
	message, is_html, starts_at
	from tmp
)
, today_full as (
	select * from lang
	union
	select * from today
)
insert into current_tasks
(chat_id, title, message, is_html, send_at)
select chat_id, task_name, coalesce(message, task_name), is_html, @current_date + starts_at::time
from today_full
";

		public static async Task CreateTasksScheduledForIntervalAsync( TimeSpan interval )
		{
			var currentDate = LocalDate.FromDateTime( DateTime.UtcNow.Date.AddHours( 2 ) ).ToDateTimeUnspecified();
			using var conn = await GetConnectionAsync().ConfigureAwait( false );
			using var command = conn.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<DateTime>( "current_date", currentDate ) );
			command.Parameters.Add( new NpgsqlParameter<TimeSpan>( "creation_interval", interval ) );
			command.CommandText = _createTodayTasksSql;
			await command.ExecuteNonQueryAsync().ConfigureAwait( false );
		}

		private const string _taskDoneSql = @"
with to_delete as (
	select sending_task_id, chat_id, tg_message_id, sent_at
	from tg_messages
	inner join current_tasks on id = sending_task_id
	where sending_task_id = @current_task_id
), deleted_sendings as (
	delete from sending_tasks
	using to_delete
	where sending_task_id = task_to_send
	returning task_to_send id
), deletion_info as (
	select sending_task_id, chat_id, tg_message_id, sent_at
	from to_delete
	inner join deleted_sendings
		on sending_task_id = id
), deletion_insert as (
	insert into deletion_tasks
	(chat_id, tg_message_id, sending_task_id, sent_at)
	select chat_id, tg_message_id, sending_task_id, sent_at
	from deletion_info
	order by sending_task_id, sent_at
	offset 1
	on conflict (chat_id, tg_message_id) do nothing
	returning 1
)
select
	sending_task_id,
	chat_id,
	tg_message_id,
	sent_at,
	(select count(*) from deletion_info) as cnt,
	(select count(*) from deletion_insert) as __
from deletion_info
order by sending_task_id, sent_at
limit 1
";

		public static async Task<(MessageSent message, int notificationsCount)?> DoneTaskAsync( int sendingTaskId, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var command = conn.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<int>( "current_task_id", sendingTaskId ) );
			command.CommandText = _taskDoneSql;

			using var reader = await command.ExecuteReaderAsync( token ).ConfigureAwait( false );
			var hasData = await reader.ReadAsync( token ).ConfigureAwait( false );

			if ( !hasData )
				return null;


			var taskId = reader.GetInt32( 0 );
			var chatId = reader.GetString( 1 );
			var messId = reader.GetString( 2 );
			var sentAt = reader.GetDateTime( 3 );

			var msg = new MessageSent( taskId, chatId, messId, sentAt );
			var cnt = reader.GetInt32( 4 );

			return (msg, cnt);
		}

		private const string _getMessagesToDeleteSql = @"
select id, chat_id, tg_message_id
from deletion_tasks
where @related_sending_task_id = -666 or sending_task_id = @related_sending_task_id
order by sent_at
limit @cnt
";

		public static async Task<IReadOnlyList<(int taskId, string chatId, string messageId)>> GetMessagesToDeleteAsync( int maxCount, int? relatedSendingTaskId, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var command = conn.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<int>( "cnt", maxCount ) );
			command.Parameters.Add( new NpgsqlParameter<int>( "related_sending_task_id", relatedSendingTaskId ?? -666 ) );
			command.CommandText = _getMessagesToDeleteSql;

			var result = new List<(int, string, string)>();

			using var reader = await command.ExecuteReaderAsync( token ).ConfigureAwait( false );
			while ( await reader.ReadAsync( token ).ConfigureAwait( false ) )
			{
				var taskId = reader.GetInt32( 0 );
				var chatId = reader.GetString( 1 );
				var messId = reader.GetString( 2 );
				result.Add( (taskId, chatId, messId) );
			}

			return result;
		}

		private const string _clearDeletionTasksSql = @"
delete
from deletion_tasks
where id = any(@ids)
";

		public static async Task<int> ClearDeletionTasksAsync( int[] ids, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var command = conn.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<int[]>( "ids", ids ) );
			command.CommandText = _clearDeletionTasksSql;
			return await command.ExecuteNonQueryAsync( token ).ConfigureAwait( false );
		}

		private const string _getTaskMessageSql = @"select message from current_tasks where id = @current_task_id";

		public static async Task<string> GetTaskMessageAsync( int currentTaskId, CancellationToken token )
		{
			using var conn = await GetConnectionAsync( token ).ConfigureAwait( false );
			using var command = conn.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<int>( "current_task_id", currentTaskId ) );
			command.CommandText = _getTaskMessageSql;
			var messageObj = await command.ExecuteScalarAsync( token ).ConfigureAwait( false );
			return (string) messageObj;
		}

		private const string _createSendingMessagesForCurrentMinuteSql = @"
with to_send as (
	update current_tasks
	set is_sending_created = true
	where
	not is_sending_created
	and send_at < @now + interval '30 seconds'
	returning id
)
, inserted_task_ids as (
	insert into sending_tasks
	select id
	from to_send
	returning task_to_send id
)
, task_ids as (
	select id from inserted_task_ids
	union distinct
	select task_to_send id from sending_tasks
)
select id, chat_id, is_html, message
from current_tasks
inner join task_ids using(id)
";

		public static async Task<IReadOnlyList<SendingTask>> CreateSendingMessagesForCurrentMinuteAsync( DateTime now )
		{
			var result = new List<SendingTask>();

			using var connection = await GetConnectionAsync().ConfigureAwait( false );
			using var command = connection.CreateCommand();
			command.Parameters.Add( new NpgsqlParameter<DateTime>( "now", now ) );
			command.CommandText = _createSendingMessagesForCurrentMinuteSql;
			using var reader = await command.ExecuteReaderAsync().ConfigureAwait( false );
			while ( await reader.ReadAsync().ConfigureAwait( false ) )
			{
				var taskId = reader.GetInt32( 0 );
				var chatId = reader.GetString( 1 );
				var isHtml = reader.GetBoolean( 2 );
				var message = reader.GetString( 3 );
				result.Add( new SendingTask( taskId, chatId, isHtml, message ) );
			}
			return result;
		}

		private const string _saveTelegramMessageSentSql = @"
insert into tg_messages
(sending_task_id, tg_message_id, sent_at)
values (@task_id, @message_id, @sent_at)
";

		public static async Task SaveTelegramMessageSentAsync( MessageSent message )
		{
			using var conn = await GetConnectionAsync().ConfigureAwait( false );
			using var comm = conn.CreateCommand();
			comm.CommandText = _saveTelegramMessageSentSql;
			comm.Parameters.Add( new NpgsqlParameter( "task_id", message.SendingTaskId ) );
			comm.Parameters.Add( new NpgsqlParameter( "message_id", message.MessageId ) );
			comm.Parameters.Add( new NpgsqlParameter( "sent_at", message.SentAt ) );
			await comm.ExecuteNonQueryAsync().ConfigureAwait( false );
		}
	}
}

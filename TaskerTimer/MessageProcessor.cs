using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskerTimer.TgClient.Models;

namespace TaskerTimer
{
	internal static class MessageProcessor
	{
		private static readonly (string name, IMessageProcessor processor)[] _processors = typeof( IMessageProcessor )
			.Assembly
			.GetTypes()
			.Where( t => !t.IsAbstract )
			.Where( t => typeof( IMessageProcessor ).IsAssignableFrom( t ) )
			.Select( t => Activator.CreateInstance( t ) )
			.Cast<IMessageProcessor>()
			.Select( v => (v.GetType().Name, v) )
			.ToArray()
			;

		internal static async Task ProcessMessageAsync( Message message, ILogger logger, CancellationToken cancellation )
		{
			if ( _processors.Length == 0 )
			{
				logger.LogWarning( "no processors found" );
				return;
			}
			foreach ( var (name, processor) in _processors )
			{
				var canProcess = processor.CanProcess( message );
				logger.LogInformation( $"processor {name} can{( canProcess ? "" : "'t" )} process message {message.text}" );
				if ( canProcess )
					await processor.ProcessAsync( message, logger, cancellation ).ConfigureAwait( false );
			}
		}
	}
}

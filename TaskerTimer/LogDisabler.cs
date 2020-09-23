using System;
using Microsoft.Extensions.Logging;

namespace TaskerTimer
{
	internal static class LogDisabler
	{
		private const bool _isDisabled = true;

		private class NoopLogger : ILogger
		{
			private class DisposableMock : IDisposable
			{
				public static IDisposable Instance { get; } = new DisposableMock();
				public void Dispose() { }
			}

			public static ILogger Instance { get; } = new NoopLogger();

			public IDisposable BeginScope<TState>( TState state ) => DisposableMock.Instance;
			public bool IsEnabled( LogLevel logLevel ) => false;
			public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter )
			{

			}
		}

		public static ILogger GetLogger( ILogger logger )
			=> _isDisabled
			? NoopLogger.Instance
			: logger;
	}
}

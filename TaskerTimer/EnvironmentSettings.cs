using System;

namespace TaskerTimer
{
	internal static class EnvironmentSettings
	{
		public static string BotToken { get; } = Environment.GetEnvironmentVariable( "BOT_TOKEN" );
		public static string ConnectionString { get; } = Environment.GetEnvironmentVariable( "CONNECTION_STRING" );
	}
}

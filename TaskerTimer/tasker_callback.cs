using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TaskerTimer
{
	public static class TaskerCallback
	{
		private class Req
		{
#pragma warning disable IDE1006 // Naming Styles
			public TgClient.Models.CallbackQuery callback_query { get; set; }
			public TgClient.Models.Message message { get; set; }
#pragma warning restore IDE1006 // Naming Styles
		}

		[FunctionName( "tasker_callback" )]
		public static async Task<IActionResult> RunAsync
		(
			[HttpTrigger( AuthorizationLevel.Anonymous, "get", "post", Route = "callback" )]
			HttpRequest req,
			ILogger log
		)
		{
			log = LogDisabler.GetLogger( log );

			log.LogInformation( req.Method );

			if ( req.Method == HttpMethods.Get )
				return new OkObjectResult( "works" );

			string requestBody;
			using ( var streamReader = new StreamReader( req.Body ) )
				requestBody = await streamReader.ReadToEndAsync().ConfigureAwait( false );
			log.LogInformation( requestBody );

			var cancellation = req.HttpContext.RequestAborted;

			var query = JsonConvert.DeserializeObject<Req>( requestBody );

			if ( query.callback_query == null && query.message == null )
				return new OkObjectResult( "wtf telegram" );

			if ( query.callback_query != null )
			{
				log.LogInformation( "received callback" );

				var taskId = Int32.Parse( query.callback_query.data );

				await SchedulingLogic.DoneTaskAsync( taskId, query.callback_query, log, cancellation ).ConfigureAwait( false );
			}
			if ( query.message != null )
			{
				log.LogInformation( "received message" );
				await MessageProcessor.ProcessMessageAsync( query.message, log, cancellation ).ConfigureAwait( false );
			}
			return new OkObjectResult( "Ok" );
		}
	}
}

namespace TaskParser
module TaskParser =
  open Parsers
  open System
  open Types
  open FParsec

  let private parse =
    pTextSection
    .>>.? pSending
    |> tuple2
    <| eof
    |>> fun (((a,b),c),_) -> (a,b,c)
    |> run

  let private mapParsed timezoneOffset (now: DateTime) = function
    | Success((title,message,time),_,_) ->
      Result.Ok {
        Title = title
        Message = message
          |> Option.map (fun v -> { Text = v; IsHtml = v.Contains("<") && v.Contains(">") })
        Sending = {
          Date = Fixed <| now.Date
          Time = time
        }
      }
    | Failure(error,_,_) -> Result.Error error

  let Parse timezoneOffset now =
    parse
    >> mapParsed timezoneOffset now

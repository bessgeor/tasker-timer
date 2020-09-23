module Tests

  open System
  open Xunit
  open TaskParser.Types
  open TaskParser.TaskParser
  open Newtonsoft.Json

  let dateForTests = DateTime(2019, 10, 19, 0, 0, 0, 0, Globalization.GregorianCalendar(), DateTimeKind.Utc)
  let timeForTests = TimeSpan(0, 03, 30, 0, 0)
  let nowForTests = dateForTests + timeForTests
  let timezoneOffsetForTests = 0.0

  let parserForTests = Parse timezoneOffsetForTests nowForTests

  type TimeSpan
    with member this.AddMinutes minutes = this + TimeSpan(0, minutes, 0)

  let plainTextLines =
    seq {
      yield "singleWord"
      yield "multiple words"
      yield "tab\tseparated"
      yield "punctuation massacre !@#@$%$#$^&*(){}[]:;'\"\\|/?.,"
      yield "кириллица"
      yield "больше кириллицы"
    }

  let htmlTextLines =
    let tagify text tag =
      sprintf "<%s>%s</%s>" tag text tag

    let multiTagify tags text =
      tags
      |> Seq.map (tagify text)

    let boldTags =
      seq {
        yield "b"
        yield "strong"
      }

    let italicTags =
      seq {
        yield "i"
        yield "em"
      }

    let multilineCodeTag = "pre"

    let singleLineCodeTags =
      seq {
        yield "code"
        yield multilineCodeTag
      }

    seq {
      yield! plainTextLines
      yield @"<a href=""https://some.site/what/ever/link13214124-you_could%20find.jpg"">link</a>"
      yield! multiTagify boldTags "bold"
      yield! multiTagify italicTags "italic"
      yield! multiTagify singleLineCodeTags "single line code"
      yield tagify "multi\nline\ncode" multilineCodeTag
    }

  let joinPairs (separator: string) (first, last) = String.Join(separator, first, last)

  let rec generateComplex stepsCount step separator (currentValue: string seq) =
    if step = stepsCount
    then currentValue
    else
      htmlTextLines
      |> Seq.allPairs currentValue
      |> Seq.map (joinPairs separator)
      |> generateComplex stepsCount (step + 1) separator

  let complexLines firstLines =
    generateComplex 2 0 " " firstLines

  let lineDelimeters =
    seq {
      yield "\r"
      yield "\n"
      yield "\r\n"
    }

  let multiLines lines count =
    if count <= 1
    then lines
    else
      lineDelimeters
      |> Seq.map (generateComplex count 0)
      |> Seq.collect (fun generator -> generator lines)

  let allVariants lines =
    seq {
      yield! lines
      (*let complexLines = complexLines lines
      yield! complexLines
      yield! multiLines lines 3
      yield! multiLines complexLines 3*)
    }

  let messages =
    seq {
      let toMessages isHtml texts =
        texts |> Seq.map (fun text -> { IsHtml = isHtml; Text = text })
      let toPlainMessages = toMessages false
      let toHtmlMessages = toMessages true

      yield! allVariants plainTextLines |> toPlainMessages

      yield! allVariants htmlTextLines |> toHtmlMessages
    }

  let titlesAndMessages =
    let messages =
      messages
      |> Seq.map Some
      |> Seq.append (seq { yield Option<Message>.None })
    messages
    |> Seq.allPairs plainTextLines

  let nonPeriodicSendings =
    seq {
      let beforeNow = timeForTests.AddMinutes -1
      yield beforeNow.ToString(@"hh\:mm"), beforeNow, dateForTests.AddDays(1.0)

      let hourFormats = ["HH";"H"] // DateTime uses different format specifiers
      let minuteFormats = ["mm";"m"]
      let timeFormats =
        minuteFormats
        |> Seq.allPairs hourFormats
        |> Seq.map (fun (hr, min) -> hr + ":" + min)

      let dayFormats = ["dd"; "d"]
      let monthFormats = ["MM"; "M"]
      let yearFormats = ["yy"; "yyyy"]
      let dateFormats =
        dayFormats
        |> List.toSeq
        |> Seq.allPairs monthFormats
        |> Seq.map(fun (month,day) -> day + "." + month)
      let fullDateFormats =
        dateFormats
        |> Seq.allPairs yearFormats
        |> Seq.map(fun (year, date) -> date + "." + year)
      let allDateFormats =
        dateFormats
        |> Seq.append fullDateFormats
        |> Seq.map Some
        |> Seq.append (seq { yield None })

      let allFormats =
        timeFormats
        |> Seq.allPairs allDateFormats
        |> Seq.map (
                    function
                    | (Some date, time) -> date + " " + time
                    | (None, time) -> time
                    )

      let afterNow = dateForTests.Add(timeForTests).AddMinutes(1.0)

      yield!
        allFormats
        |> Seq.map afterNow.ToString
        |> Seq.map (fun x -> x, afterNow.TimeOfDay, dateForTests)
    }

  let allSendings =
    nonPeriodicSendings
    |> Seq.map (fun (messageTime, expectedTime, expectedDate) -> messageTime, { Date = Fixed expectedDate; Time = expectedTime })
    |> Seq.allPairs titlesAndMessages
    |> Seq.allPairs lineDelimeters
    |> Seq.cache

  type ``parsing a task provides expected results`` () =
    static member TestCases
      with get () =
        allSendings
        |> Seq.map (
          fun (lineDelimeter,((title,message),(messageTime, expectedSending))) ->
            let messageText =
              message
              |> Option.map (fun message -> title,message.Text)
              |> Option.map (joinPairs lineDelimeter)
              |> Option.defaultValue title

            String.Join(lineDelimeter,messageText,messageTime),
            JsonConvert.SerializeObject({
              Title = title;
              Message = message;
              Sending = expectedSending;
            })
          )
        |> Seq.map (fun (text, message) -> [| box text; box message |])

    [<Theory>]
    [<MemberData("TestCases", DisableDiscoveryEnumeration = false)>]
    member __.``success cases`` (unparsedMessage: string, expectedSerialized: string): unit =
      let expected = JsonConvert.DeserializeObject<ParsedTask>(expectedSerialized)
      let parsed = parserForTests unparsedMessage
      Assert.Equal(parsed, Result<ParsedTask, string>.Ok expected)

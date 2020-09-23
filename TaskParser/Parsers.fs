namespace TaskParser
module internal Parsers =
  open System
  open FParsec

  let pTextLine pNewLine x =
    noneOf "\r\n"
    |> many1Chars
    .>> pNewLine
    <| x

  let pTitle x = pTextLine skipNewline x;
  let pMessage x =
    pTextLine unicodeNewline
    |> many1
    |>> String.concat ""
    |> opt
    <| x
  let pTextSection x =
    pTitle
    .>>.? pMessage
    .>> skipNewline
    <| x

  let charToInt (x: char) = (int x) - (int '0')

  let pDigitInRange min max =
    "0123456789".Substring(min, max - min + 1)
    |> anyOf
    |>> charToInt
  let pDigit x = pDigitInRange x x

  let pNumber x = x |>> fun (d,o) -> d * 10 + o

  let pHours: Parser<int, unit> =
    (
      ((pDigit 0 |> opt) <|> (pDigit 1 |>> Some) .>>.? (digit |>> charToInt))
      <|>
      (pDigit 2 |>> Some .>>.? pDigitInRange 0 4)
    )
    |>> function
      | (None, o) -> 0, o
      | (Some d, o) -> d, o
    |> pNumber

  let pMinutes =
    tuple2 (pDigitInRange 0 5) (digit |>> charToInt)
    |> pNumber

  let pTime =
    pipe3 pHours (skipChar ':') pMinutes
      (fun h -> fun _ -> fun m -> TimeSpan(0, h, m, 0, 0))
    
  let pSending = pTime

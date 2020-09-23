namespace TaskParser

module Types =
  open System

  type Message =
    {
      IsHtml: bool
      Text: string
    }
  type PeriodSpecification =
    {
      StartsAt: DateTime
      Period: TimeSpan
    }
  type DateSpecification =
    | Fixed of DateTime
    | Periodic of PeriodSpecification

  type SendingSpecification =
    {
      Date: DateSpecification
      Time: TimeSpan
    }
  type ParsedTask =
    {
      Title: string
      Message: Message option
      Sending: SendingSpecification
    }


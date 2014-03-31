namespace TraceRouteFinder

open System;
open TraceRouteFinder;

module ConsoleApp =

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv

        let traceresult = TraceRouteFinder.CommandHandler.ExecuteTracertCommand "www.vg.no" "d:\\traceout.txt"

        Console.ReadLine() |> ignore;

        0 // return an integer exit code

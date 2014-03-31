namespace TraceRouteFinder

open System.Diagnostics;
open System;

module CommandHandler = 
    let ExecuteTracertCommand url outPath =

        let cmd = "cmd";
        let startInfo = new ProcessStartInfo(cmd, "/c tracert www.vg.no");
        
        startInfo.UseShellExecute <- false;
        startInfo.CreateNoWindow <- false;
        startInfo.RedirectStandardOutput <- true;
        
        let proc = new Process();
        proc.StartInfo <- startInfo;
        proc.Start() |> ignore;

        let result = proc.StandardOutput.ReadToEnd();
        result;


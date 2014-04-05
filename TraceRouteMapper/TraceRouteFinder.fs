namespace TraceRouteFinder

open System.Diagnostics
open System
open System.Reflection
open System.IO
open System.Text.RegularExpressions
open RestSharp
open RestSharp.Serializers
open FSharp.Data.JsonExtensions
open System.Windows.Forms
open System.Drawing

type GeoLocation = { 
    City: string; 
    CountryCode:string; 
    IP:string; 
    Lat:string; 
    Lng:string; 
    RegionName:string; 
    Id:int; 
    }

module Globals = 
    let mutable TraceUrl = ""

module FileHelpers = 
    let WriteToFile content =
        let filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + sprintf "\Trace log for %s.txt" (Globals.TraceUrl.Replace('.', '-'))
        File.AppendAllText(filepath, content)
        Console.WriteLine(sprintf "Stored traceresult to %s" filepath)

    let SaveImageToDisk (bytes:byte array) =
        let filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + sprintf "\Trace img for %s.png" (Globals.TraceUrl.Replace('.', '-'))
        File.WriteAllBytes(filepath, bytes)
        Console.WriteLine(sprintf "Stored image to %s" filepath)

module RegexHelpers =
     let regex = new Regex "(?:[0-9]{1,3}\.){3}[0-9]{1,3}"
     let ExtractIPs (text:string) =
         let matchesRaw = regex.Matches text
         [for m in matchesRaw do yield m.Value] |> List.tail
    
module ImageHelpers =
    let CreateImage (bytes:byte array) =
        FileHelpers.SaveImageToDisk bytes
        let imgStream = new MemoryStream(bytes)
        let image = Image.FromStream imgStream
        image

module CommandLineHandler = 
    let FakeTracert = "Tracing route to www.vg.no [195.88.54.16]
                        over a maximum of 30 hops:

                          1    <1 ms    <1 ms    <1 ms  192.168.1.1 
                          2    11 ms    11 ms     9 ms  10.128.96.1 
                          3     9 ms    11 ms    11 ms  62.92.249.85 
                          4    19 ms    19 ms    20 ms  ti0003c400-ae23-0.ti.telenor.net [146.172.19.133] 
                          5    20 ms    19 ms    16 ms  ti0001c360-ae1-0.ti.telenor.net [146.172.99.146] 
                          6    16 ms    22 ms    18 ms  ti0001a400-ae0-0.ti.telenor.net [146.172.99.170] 
                          7    11 ms    36 ms    18 ms  xe-2-3-0.cr1-osl2.n.bitbit.net [62.92.230.2] 
                          8    19 ms    19 ms    20 ms  vlan-9.cs1-osl2.n.bitbit.net [87.238.62.87] 
                          9    20 ms    15 ms    19 ms  www.vg.no [195.88.54.16] 

                        Trace complete."

    let ExecuteTracertCommand url =
        Globals.TraceUrl <- url
        let startInfo = new ProcessStartInfo("cmd", "/c tracert " + url);
        
        startInfo.UseShellExecute <- false;
        startInfo.CreateNoWindow <- false;
        startInfo.RedirectStandardOutput <- true;
        
        let proc = new Process();
        proc.StartInfo <- startInfo;
        proc.Start() |> ignore;

        let result = proc.StandardOutput.ReadToEnd();
        FileHelpers.WriteToFile result
        result;

module GeoLocator =
    let mutable counter = 0
    let GetGeoLocationForIP (ip:string) =
            let geoapi = "http://freegeoip.net/json/" + ip

            let client = new RestClient()

            let request = new RestRequest(geoapi, Method.GET)

            let response = client.Get request
            let content = response.Content

            let des = FSharp.Data.JsonValue.Parse content
            
            let geoLoc = { 
                City = des?city.AsString(); 
                CountryCode = des?country_code.AsString();
                IP = des?ip.AsString();
                Lat = des?latitude.AsString();
                Lng = des?longitude.AsString();
                RegionName = des?region_name.AsString();
                Id = counter;
              }
            counter <- counter + 1
            geoLoc

    let GetLocationsForIPs (ips:string seq) =
        counter <- 0
        ips |> Seq.map (fun x -> GetGeoLocationForIP x) |> Seq.toList

    let RemoveWhereCountryNotSet (geoLocations:GeoLocation seq) =
        // Remove entries in the sequence where the country code is set to "RD", (meaning 'Reserved') which equals no country is set
        geoLocations |> Seq.filter(fun x -> x.CountryCode <> "RD")

module StaticGoogleMap =
    let CreateMapUrl (geoLocations: GeoLocation list) = 
        let baseUrl = "http://maps.googleapis.com/maps/api/staticmap?"
        let zoomLevel = "4"
        let width = "800"
        let height = "600"
        let maptype = "roadmap"
        let scale = "2"
        let markerColor = "red"
        let centerLat = (geoLocations.Item 0).Lat
        let centerLng = (geoLocations.Item 0).Lng     
        
        let urlCenter = sprintf "center=%s,%s" centerLat centerLng
        let urlZoom = sprintf "zoom=%s" zoomLevel
        let urlSize = sprintf "size=%sx%s" width height
        let urlMaptype = sprintf "maptype=%s" maptype
        let urlSensor = "sensor=false"
        let urlScale = sprintf "scale=%s" scale

        let CreatePathQueryString (geoLoc:GeoLocation) =
            sprintf "|%s,%s" geoLoc.Lat geoLoc.Lng

        let CreateMarkerQueryString (geoLoc:GeoLocation) =
            let color = "red"
            let markerStr = String.Format("&markers=color:{0}|label:{3}|{1},{2}", color, geoLoc.Lat, geoLoc.Lng, geoLoc.Id)
            markerStr
        
        let markerStrings = [for geoLoc in geoLocations do yield CreateMarkerQueryString geoLoc]
        let urlMarkers = markerStrings |> List.reduce(+)

        let pathStrings = [for geoLoc in geoLocations do yield CreatePathQueryString geoLoc]
        let pathConcat = pathStrings |> List.reduce(+)
        let urlPath = "path=color:0x0000ff|weight:5" + pathConcat
        
        let fullUrl = sprintf "%s%s&%s%s&%s&%s" baseUrl urlSize urlMaptype urlMarkers urlPath urlSensor
        fullUrl
    
    let GetStaticMapBytes (url:string) =
        let client = new RestClient()
        let request = new RestRequest(url, Method.GET)
        let response = client.Execute request
        if response.StatusCode.Equals Net.HttpStatusCode.OK then
            response.RawBytes
        else
            null

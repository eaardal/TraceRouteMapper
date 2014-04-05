namespace TraceRouteFinder

open System
open TraceRouteFinder
open System.Windows.Forms
open System.Drawing

module WinFormsWrapper =
    let ShowMap image =
        let form = new Form()
        let imgBox = new PictureBox()
        imgBox.Image <- image
        imgBox.SizeMode <- PictureBoxSizeMode.AutoSize
        form.Controls.Add imgBox
        form.Width <- imgBox.Width
        form.Height <- imgBox.Height
        form.StartPosition <- FormStartPosition.CenterScreen
        Application.Run form

module ConsoleApp =
    [<EntryPoint>]
    let main argv = 
       
        // Get input
        Console.WriteLine "Enter url to trace:"
        let input = Console.ReadLine()
        Console.WriteLine(sprintf "Tracing %s, this may take a while" input)
       
        // Run tracert command and retrieve result
        let traceresult = CommandLineHandler.ExecuteTracertCommand input

        // Find all IPs in the tracert result
        let ips = RegexHelpers.ExtractIPs traceresult

        // Query external api for geo locations for each IP
        let geolocs = GeoLocator.GetLocationsForIPs ips

        // Construct a url to be sent to google's static map api
        let url = StaticGoogleMap.CreateMapUrl (GeoLocator.RemoveWhereCountryNotSet geolocs |> Seq.toList)

        // Query google static map api, get the raw byte data from the result
        let rawImageData = StaticGoogleMap.GetStaticMapBytes url

        // Construct the image from the raw byte data
        let image = ImageHelpers.CreateImage rawImageData
                
        let PrintGeoLocations (geoLocs:GeoLocation list) =
            [for geoLoc in geoLocs do Console.WriteLine(String.Format("#{0}: {1} - {2}({3}) - {4},{5}", geoLoc.Id, geoLoc.IP, geoLoc.CountryCode, geoLoc.City, geoLoc.Lat, geoLoc.Lng))]

        // Print all geo locations to console. Id's matches the marker labels in the map
        PrintGeoLocations geolocs |> ignore

        // Display the map in a winforms window
        WinFormsWrapper.ShowMap image

        Console.ReadLine() |> ignore;
        0

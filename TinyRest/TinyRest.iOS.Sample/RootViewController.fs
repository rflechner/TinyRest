namespace TinyRest.iOS.Sample

open System
open System.Drawing
open System.Diagnostics

open UIKit
open Foundation
open CoreGraphics

open TinyRestServerPCL
open TinyRestServer
open Http
open Routing
open System.Net

type UiLogger(edit:UITextView) =
    interface ILogger with
        member x.Log (text:string) =
                edit.BeginInvokeOnMainThread(new Action(fun () -> edit.Text <- edit.Text + "\n" + text)) |> ignore

[<Register ("RootViewController")>]
type RootViewController () =
    inherit UIViewController ()

    let withX (x:nfloat) (r:CGRect) = new CGRect(x, r.Y, r.Width, r.Height)
    let withY (y:nfloat) (r:CGRect) = new CGRect(r.X, y, r.Width, r.Height)
    let withW (w:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, w, r.Height)
    let withH (h:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, r.Width, h)

    let addX (x:nfloat) (r:CGRect) = new CGRect(r.X + x, r.Y, r.Width, r.Height)
    let addY (y:nfloat) (r:CGRect) = new CGRect(r.X, r.Y + y, r.Width, r.Height)
    let addW (w:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, r.Width + w, r.Height)
    let addH (h:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, r.Width, r.Height + h)

    let remX (x:nfloat) (r:CGRect) = new CGRect(r.X - x, r.Y, r.Width, r.Height)
    let remY (y:nfloat) (r:CGRect) = new CGRect(r.X, r.Y - y, r.Width, r.Height)
    let remW (w:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, r.Width - w, r.Height)
    let remH (h:nfloat) (r:CGRect) = new CGRect(r.X, r.Y, r.Width, r.Height - h)

    // Release any cached data, images, etc that aren't in use.
    override this.DidReceiveMemoryWarning () =
        base.DidReceiveMemoryWarning ()

    // Perform any additional setup after loading the view, typically from a nib.
    override this.ViewDidLoad () =
        base.ViewDidLoad ()

        this.View.BackgroundColor <- UIColor.White

        let button = new UIButton(new CGRect(new CGPoint(50., 50.), new CGSize(nfloat 40., nfloat 40.)))
        button.SetTitle("Start server", UIControlState.Normal)
        button.BackgroundColor <- UIColor.LightGray
        button.TintColor <- UIColor.Black

        this.View.Add(button)

        let textView = new UITextView(button.Frame |> withY button.Frame.Bottom |> withH (this.View.Frame.Height - button.Frame.Bottom))
        this.View.Add(textView)

        button.TouchDown.Add(fun s -> 
                let logger = new UiLogger(textView)
                let listener = new Listener()
          
                async {
                    
                    ServiceImplementation.startServer listener logger

                    let client = new WebClient()
                    let content = client.DownloadString(new Uri("http://localhost:8009/TinyRest1"))
                    Debug.WriteLine(content)

                    } |> Async.RunSynchronously
            )

    // Return true for supported orientations
    override this.ShouldAutorotateToInterfaceOrientation (orientation) =
        true

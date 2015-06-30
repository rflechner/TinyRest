namespace TinyRest.Droid.Example

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open System.Net
open System.Net.NetworkInformation
open System.Threading.Tasks
open System.Diagnostics

open TinyRestServerPCL
open TinyRestServer

type UiLogger(edit:EditText) =
    interface ILogger with
        member x.Log (text:string) =
                edit.Post(new Action(fun () -> edit.Text <- edit.Text + "\n" + text)) |> ignore

[<Activity (Label = "TinyRest.Droid.Example", MainLauncher = true)>]
type MainActivity () =
    inherit Activity ()

    let mutable count:int = 1

    override this.OnCreate (bundle) =

        base.OnCreate (bundle)

        this.SetContentView (Resource_Layout.Main)

        let button = this.FindViewById<Button>(Resource_Id.MyButton)
        let editText1 = this.FindViewById<EditText>(Resource_Id.editText1)

        button.Click.Add (fun args -> 
            editText1.Text <- ""
            button.Text <- "Started"
            button.Enabled <- false

            let logger = new UiLogger(editText1)
            let listener = new Listener()

            async {
                for a in Java.Net.Inet4Address.GetAllByName("localhost") do
                    editText1.Post(new Action(fun () -> editText1.Text <- "\n" + a.HostAddress)) |> ignore
                 
                ServiceImplementation.startServer listener logger


                let client = new WebClient()
                let content = client.DownloadString(new Uri("http://localhost:8009/TinyRest1"))
                Debug.WriteLine(content)

                } |> Async.RunSynchronously
            
        )

        


namespace TinyRest.iOS.Sample

open System
open UIKit
open Foundation

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    let window = new UIWindow (UIScreen.MainScreen.Bounds)

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        // If you have defined a root view controller, set it here:
        //window.RootViewController <- new RootViewController ()
        window.MakeKeyAndVisible ()
        true

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        true

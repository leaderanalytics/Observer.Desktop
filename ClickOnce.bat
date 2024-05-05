dotnet mage -al Observer.exe -td Observer.Desktop\bin\Release\net8
dotnet mage -new Application -t  Observer.Desktop\bin\Release\net8\Observer.Manifest -fd Observer.Desktop\bin\Release\net8 -v 1.0.0.0
dotnet mage -new Deployment -Install true -pub "Leader Analytics" -v 1.0.0.0 -AppManifest Observer.Desktop\bin\Release\net8\Observer.manifest -t Observer.application
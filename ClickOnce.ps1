# https://www.rubberchickenparadise.com/blog/2021-11-18-automating-clickonce-deployment-for-net-5-0-winforms/

param(
    [Parameter()]
    [string]
    $Version,
    [Parameter()]
    [string]
    $packageName,
    [Parameter()]
    [string]
    $confituration,
    [Parameter()]
    [string]
    $networkDeployPath,
    [Parameter()]
    [string]
    $winformsProjLocation,
    [Parameter()]
    [string]
    $packageOutDir
)

$appFileFolder="$($packageName)_$($version.replace(".", "_"))"


#This stuff stays the same
$applicationFileSubFolder="Application Files"
$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin"
$buildOutputDir = "$packageOutDir\bin"

#Automated build below here.  Unless something really changes this should never change
#Lets Clean things up
remove-item -recurse $buildOutputDir

&"$msbuildPath\MSBuild.exe" /t:Clean $winformsProjLocation

#Now build the application
&"$msbuildPath\MSBuild.exe" -p:Configuration=$confituration -p:OutDir=$buildOutputDir /p:Version=$Version /p:ThisProjectNameOverrideAssemblyName=$packageName $winformsProjLocation

#Remove some stuff that shouldn't get released
remove-item -recurse "$buildOutputDir\ref"

#Build our local version of the network file path
$appFileDir = "$packageOutDir\$applicationFileSubFolder\$appFileFolder"

if(!(test-path $appFileDir)){
    New-Item -Path $appFileDir -ItemType Directory
}
#Copy the files
copy-item -recurse "$buildOutputDir\*" $appFileDir

#Lets use mage for the clickonce package build
#Have to add a launcher
dotnet mage -al "$packageName.exe" -td $appFileDir

#now lets create a file manifest
$manifestName = "$appFileDir\$packageName.manifest"
#Its always new because its a new deploy folder
dotnet mage -new Application -t $manifestName -fd $appFileDir -v $Version

#Now lets create or update our click once app
$clickOnceAppFile = "$packageOutDir\$packageName.application"

if(!(test-path $clickOnceAppFile)){

    dotnet mage -new Deployment -Install true -pub "Company Name" -v $Version -AppManifest $manifestName -t $clickOnceAppFile
    dotnet mage -Update $clickOnceAppFile -pub "Company Name" -v $Version -AppManifest $manifestName -MinVersion $Version
} else{
    dotnet mage -Update $clickOnceAppFile -Install true -pub "Company Name" -v $Version -AppManifest $manifestName
    dotnet mage -Update $clickOnceAppFile -pub "Company Name" -v $Version -AppManifest $manifestName -MinVersion $Version
}


copy-item -recurse "$appFileDir" "$networkDeployPath\$applicationFileSubFolder\$appFileFolder"
copy-item $clickOnceAppFile $networkDeployPath
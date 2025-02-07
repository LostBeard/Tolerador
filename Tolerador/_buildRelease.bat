@echo off

set outputPath=bin\PublishRelease
set outputPathCompat=bin\PublishReleaseCompat

REM build default
echo "Creating Release publish build"
rmdir /Q /S "%outputPath%"
dotnet publish --nologo --configuration Release --output "%outputPath%"

REM build compat (allows Blazor to run on older CPUs)
echo "Creating Release publish compat build"
rmdir /Q /S "%outputPathCompat%"
dotnet publish --nologo --no-restore --configuration Release -p:WasmEnableSIMD=false -p:BlazorWebAssemblyJiterpreter=false -p:SpawnDevBrowserExtensionPlatforms=0 --output "%outputPathCompat%"

REM combine builds (including extension platform builds)
echo "Combine builds"

echo "Copying compat framework to published output"
xcopy /I /E /Y "%outputPathCompat%\wwwroot\_framework" "%outputPath%\wwwroot\_frameworkCompat"

echo "Copying compat framework to Firefox extension"
xcopy /I /E /Y "%outputPathCompat%\wwwroot\_framework" "%outputPath%\Firefox\app\_frameworkCompat"

echo "Copying compat framework to Chrome extension"
xcopy /I /E /Y "%outputPathCompat%\wwwroot\_framework" "%outputPath%\Chrome\app\_frameworkCompat"

REM cleanup
rmdir /Q /S "%outputPathCompat%"

REM copy to network share (custom)
echo "Copying to network shared folder"
rmdir /Q /S "x:\ReleaseExtension\"

xcopy /I /E /Y %outputPath% "x:\ReleaseExtension"

echo "Build complete."

pause

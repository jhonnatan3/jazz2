# [![Jazz² Resurrection](https://github.com/deathkiller/jazz2/raw/master/Docs/Logo.gif)](https://github.com/deathkiller/jazz2)
Jazz² Resurrection is reimplementation of game ***Jazz Jackrabbit 2*** from year 1998. Supports various versions of the game (Shareware Demo, Holiday Hare '98, The Secret Files and Christmas Chronicles). Also, it partially supports some features of [JJ2+](http://jj2.plus/) extension and MLLE.

Uses parts of [Duality - A 2D GameDev Framework](https://duality.adamslair.net/).


## Dependencies
### Windows
* .NET Framework 4.5.2 (or newer)
* [OpenALSoft](https://github.com/opentk/opentk-dependencies)
  * Copy `x86/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x86.dll`
  * Copy `x64/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x64.dll`
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/)
  * Copy `libopenmpt.dll` (*x86*, and its dependencies) to `‹Game›` directory

### Linux
* [Mono 4.6 (or newer)](http://www.mono-project.com/download/#download-lin)
* OpenAL
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/)
  * Copy `libopenmpt.so` (*x86*, and its dependencies) to `‹Game›` directory

### macOS
* [Mono 4.6 (or newer)](http://www.mono-project.com/download/#download-mac)
* [libopenmpt](https://lib.openmpt.org/libopenmpt/)

### Android
* Xamarin
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/) (included for *armeabi-v7a* and *x86*)

Requires [Microsoft Visual Studio 2017](https://www.visualstudio.com/) (or equivalent Mono compiler) to build the solution.


## Running the application
### Windows / Linux / macOS
* Build the solution
* Copy `Content` directory to `‹Game›/Content`
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
  * On macOS, you can run `mono Import.exe "Path to JJ2"`
* Run `‹Game›/Jazz2.exe`
  * On macOS, you can run `mono Jazz2.exe`

*You can run `Import.exe` without parameters to show additional options.*

### Android
* Build the solution
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
* Copy `‹Game›/Content` directory to `‹SDCard›/Jazz2.Android/Content` 
* Copy files from `Jazz2.Android/Shaders` directory to `‹SDCard›/Jazz2.Android/Content/Shaders` 
* Copy files from `Jazz2.Android/Shaders/Internal` directory to `‹SDCard›/Jazz2.Android/Content/Internal`
* *Create empty file `.nomedia` in `‹SDCard›/Jazz2.Android` to hide game files in Android Gallery (optimal)*
* Install APK file on Android
* Run the application

*Requires device with Android 4.4 (or newer) and OpenGL ES 3.0. `‹SDCard›` could be internal or external storage. The application tries to autodetect correct path. Also, you can use `‹SDCard›/Android/Data/Jazz2.Android` or `‹SDCard›/Download/Jazz2.Android` instead.*


## Building the solution
### Windows · [![Build status](https://ci.appveyor.com/api/projects/status/m4iy9vaot84bsrqn?svg=true)](https://ci.appveyor.com/project/deathkiller/jazz2)
* Open the solution in [Microsoft Visual Studio 2017](https://www.visualstudio.com/) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Copy dependencies to `/Jazz2/Bin/Debug/` or `/Jazz2/Bin/Release/`
* If you build Release configuration, you have to replace `Debug` with `Release` in paths above

### Linux
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-lin)
* Run following commands in directory with the solution file (.sln):
```bash
sudo apt install nuget
nuget restore
msbuild
```
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Obtain and copy `libopenmpt.so` to `/Jazz2/Bin/Debug/libopenmpt.so` to enable music playback
* Then you can rebuild the solution only with `msbuild` command
* Use `msbuild /p:Configuration=Release` to build Release configuration, you have to replace `Debug` with `Release` in paths above

### macOS
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-mac)
* Open the solution in [Microsoft Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* If you build Release configuration, you have to replace `Debug` with `Release` in paths above

*Errors about `Jazz2.Android` project can be ignored, if you don't need Android build.*

### Android · [![Build status](https://build.appcenter.ms/v0.1/apps/a88b4a01-69a6-483a-93ba-e58726182e66/branches/master/badge)](https://github.com/deathkiller/jazz2)
* Install **Mobile development in .NET** to Microsoft Visual Studio 2017
* Open the solution and build `Jazz2.Android` project


## Extensions
### OpenGL ES 2.0
Alternative OpenGL ES 2.0 backend can be built separatelly. It does not contain all features
that are available in default OpenGL 2.1 backend, but it should run faster on low-end configurations.
**Don't use it if you have no reason for it!**

To use it, build `Extensions/Es20Backend` project. It should be copied to
`/Jazz2/Bin/Debug/Extensions/Es20Backend.core.dll` automatically.
Then copy all files from `Content/_ES20` directory to `/Jazz2/Bin/Debug/Content` and replace them.

Also, you have to remove `/Jazz2/Bin/Debug/Extensions/GL21Backend.core.dll` file to disable default OpenGL 2.1 backend.


## License
This software is licensed under the [GNU General Public License v3.0](./LICENSE).
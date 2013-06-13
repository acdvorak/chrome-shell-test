Replacing the Windows explorer.exe shell with Google Chrome
===========================================================

Simple test project that shows how to fix a [bug introduced in Google Chrome 23][crbug]
that causes Chrome to display blank white pages when ```explorer.exe``` is not running.

```ShellReplacement.cs``` calls the [undocumented Windows kernel function ```SetShellWindow()```][wine]
in ```user32.dll``` and passes it a handle to a ```NativeWindow``` which gets used by
```GetShellWindow()``` in Chrome.

Yes, the fix is indeed as simple as calling a single (undocumented) function :-)

This project happens to be written in C# / .NET 4.0, but since it invokes native unmanaged
Win32 API code, it could easily be adapted to C / C++ as well.

[crbug]: http://crbug.com/169652#c43
[wine]: http://www.winehq.org/pipermail/wine-devel/2003-October/021368.htm
[autorestartshell]: http://technet.microsoft.com/en-us/library/cc939703.aspx

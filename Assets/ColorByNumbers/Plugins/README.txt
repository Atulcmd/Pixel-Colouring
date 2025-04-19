
SharePlugin setup for Android:

1. Double click the SharePlugin.unitypackage located in the same folder as this README

2. The plugin will be exported to Assets/Plugins/Android/SharePlugin along with some
   supporting android librarys. NOTE: The SharePlugin cannot be moved to another Plugins
   folder, it must reside in the root Plugins folder: Assets/Plugins/Android or the plugin
   will not work.

3. Open the AndroidManifest.xml located in Assets/Plugins/Android/SharePlugin and
   change the package name on line 4 and line 9 to the package name in your Player Settings.
   NOTE: If you ever change the package name in your Player Settings you must also
   change the package name in the AndroidManifest.xml or the plugin will not work. 

***************
Android Plugins
***************

There are three android jar plugins: ImagePickerPlugin, UtilsPlugin, and SharePlugin

ImagePickerPlugin:
- Used to open the content picker on android to choose an image to import into the
  app and use in CREATE mode.

UtilsPlugin:
- Has a single method used to check if the user has granted a given permission.
- It is currently being used in Assets/Scripts/Framework/NativePlugin.cs to check
  if the device has permission to use the camera.

SharePlugin:
- Has methods used to share an image to various platforms (Instagram, Twitter, and Other)
- It is also used in Assets/Scripts/Framework/NativePlugin.cs

SharePlugin Setup:

Double click the SharePlugin.unitypackage located in Assets/ColorByNumbers/Plugins
and the plugin will be exported to the correct location.

NOTE: The SharePlugin is an Android Library that uses a resource file. For some reason
it will not work properly unless it it placed in the root Plugin folder. So make
sure it its located in Assets/Plugins/Android and NOT Assets/ColorByNumbers/Plugins/Android.

The SharePlugin uses the package name as part of the authority for the FileProvider.
If you ever change the package name in Player Settings you must also change the package
name in the AndroidManifest.xml file. There are two places in the file where you need
to change the package name:

Line 4: package="your.package.name"
Line 9: android:authorities="your.package.name.fileprovider"

***********
iOS Plugins
***********

There is only one iOS plugin located in Assets/ColorByNumbers/Plugins/iOS. The plugin
contains the same functionallity as the android plugins. No setup is required.
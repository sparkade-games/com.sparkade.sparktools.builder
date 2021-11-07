# SparkTools: Builder
A multi-platform build solution with pattern matching outputs.

# Installation
It is recommened to install through the Unity Package Manager.

Another SparkTools package, Custom Project Settings, is required.

If you wish to manually install, clone the repository into the `Packages` folder of your project.

# How it works
Creates a settings file containing all the platforms you wish to build for, including settings specific to a platform.

Build paths use pattern matching, so the output path is dynamic.

# How to Use
In the menu bar go to SparkTools->Builder->Settings. Here you can specify an output folder and pattern, as well as adjust settings for individual platforms.

To view available keywords for pattern matching press the '?' button. This will also show you the output path for the active build target.

When all your settings are set up you can press Build, Build and Run, and Build All from either the SparkTools menu or the Builder Settings themself.
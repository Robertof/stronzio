Stronzio
========

Stronzio is an easy-to-use software capable of compressing images to an user-defined size.

![](https://i.imgur.com/8ZNz1GJ.png)

Stronzio is available in English and Italian.

Usage
-----

Using Stronzio is as easy as drinking water. Just download the binary or compile the project, then execute it.
Pick an image, the desired output size and press the "Compress" button. Voilà.

The compressed image is available at the same path of the input one. You can't miss it: there's a "-compressed"
after the original filename.

Requirements
------------

Stronzio requires:

- **[.NET Framework 4.0](http://www.microsoft.com/en-us/download/details.aspx?id=17851)**
- **[Visual C++ Redistributable for Visual Studio 2012](http://www.microsoft.com/en-us/download/details.aspx?id=30679)**
- [Magick.NET](http://magick.codeplex.com/) (included)
- [Extended WPF Toolkit](http://wpftoolkit.codeplex.com/) (included)

Troubleshooting
---------------

### Nothing happens when I click on the executable.

Are you sure you are double clicking it...?

Joking aside, ensure that **both** `Magick.NET-AnyCPU.dll` and `Xceed.Wpf.Toolkit.dll` are in the same path of
`Stronzio.exe`. Bad stuff (like this) happens otherwise.

### Nothing happens when I press the "Compress" button.

That's probably because you forgot to install the *"Visual C++ Redistributable for Visual Studio 2012"*.
Have a look at the [Requirements](#requirements).

### I'm a Linux user, and I'm mad at you because I can't use this.

Hey, Linux user! Don't worry. You may not (as now) use this fancy GUI, but you have access to ImageMagick.
Here's how to do the *exact same thing* with one command:

```sh
convert -define jpeg:extent=1234kb "$input_file" "$output_file"
```

Obviously, this requires [ImageMagick](http://www.imagemagick.org/). More info is available
[here](http://www.imagemagick.org/script/command-line-options.php#define).

License
-------

This software is licensed under the [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0).
A plain text copy is available in the "LICENSE" file.

[Magick.NET](http://magick.codeplex.com/) is licensed under the Apache 2.0 License, available
[here](http://www.apache.org/licenses/LICENSE-2.0).

[Extended WPF Toolkit](http://wpftoolkit.codeplex.com/) is licensed under the Microsoft Public License (Ms-PL),
available [here](http://opensource.org/licenses/ms-pl.html).

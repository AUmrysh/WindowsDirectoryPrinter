# WindowsDirectoryPrinter
This project is for a small program which runs on windows. The purpose of this program is to watch a specified directory for PDF files, print them, and then move them to the desired folder when completed.

Right now there is a dependency on adobe acrobat, which must be installed first. It may be possible to use other PDF viewers as well.

The acrobat window will open momentarily but closes after 10 seconds.

You can also run this as a service if you'd like it to start when the computer does, via the windows services settings on your computer.
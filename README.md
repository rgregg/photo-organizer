photo-organizer
===============

A simple command line tool for organizing photos and videos into a particular folder hierarchy based on the date the photo was taken.

For example, if your archive looks like \Originals\2014\01 Jan\ _filename_ you can use the command line to move a whole batch of photos into their home in your archive with this command line:

```
PhotoOrganizer.exe -s "source_folder" -r -d "\Originals\" --dest-format "yyyy\\MM MMM" 
```

A simple and easy way to keep your photo archive nice and orderly.

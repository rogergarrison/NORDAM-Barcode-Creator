# NORDAM-Barcode-Creator
NORDAM-Barcode-Creator is a C# application to create a barcode on a ZPL printer based on the specifications for NORDAM


### Information ###
- NORDAM recently started requesting suppliers to attach a barcode on packages for their convenience. A friend of mine was asked me to create an application that would allow him to put in the information and it build the barcode based on [NORDAM's specifications](https://github.com/rogergarrison/NORDAM-Barcode-Creator/blob/master/Supplier%202D%20Barcode%20Specs.pdf)
- Barcode printers from Zebra use a specialized language "ZPL" for which to command the printer. This language is hard to navigate for many people, so I created a a wrapper over ZPL in C#

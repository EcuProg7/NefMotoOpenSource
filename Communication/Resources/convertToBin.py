import os;
import binascii;
import intelhex;

ihex = intelhex.IntelHex();
ihex.loadhex("LOAD.HEX");

loader = ihex.tobinarray()

loaderFile = open("BootstrapLoader.bin",'wb');
loaderFile.write(loader);
loaderFile.close();

del ihex, loader, loaderFile;

ihex = intelhex.IntelHex();
ihex.loadhex("LOADK.HEX");

loader = ihex.tobinarray()

loaderFile = open("BootstrapLoaderKline.bin",'wb');
loaderFile.write(loader);
loaderFile.close();

del ihex, loader, loaderFile;

ihex = intelhex.IntelHex();
ihex.loadhex("MINIMON.HEX");

loader = ihex.tobinarray()

File = open("minimon.bin",'wb');
File.write(loader);
File.close();

del ihex, loader, File;

ihex = intelhex.IntelHex();
ihex.loadhex("MINIMONK.HEX");

loader = ihex.tobinarray()

File = open("minimonKline.bin",'wb');
File.write(loader);
File.close();

del ihex, loader, File;

ihex = intelhex.IntelHex();
ihex.loadhex("C167CR16.HEX");

loader = ihex.tobinarray()

File = open("C167CR16.bin",'wb');
File.write(loader);
File.close();

del ihex, loader, File;

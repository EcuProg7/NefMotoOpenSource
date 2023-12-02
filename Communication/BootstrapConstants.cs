/*
Nefarious Motorsports ME7 ECU Flasher
Copyright (C) 2017  Nefarious Motorsports Inc

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

Contact by Email: tony@nefariousmotorsports.com
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml.Serialization;
using Shared;

namespace Communication
{
    public enum BootstrapAcknowledge : byte
    {
        acknowledgeFunctionCode = 0xaa,
        acknowledgeParameter = 0xea,
    };

    public enum BootstrapFunctionCode : byte
    {
        //---------------------  Function Codes ----------------------------

        writeWord                   = 0x82, //Parameters: 3Bytes address(LSB first), 2 Bytes value(LSB first); Return: none
        writeBlockBytes             = 0x84, //Parameters: 3Bytes startaddress(LSB first), 2 Bytes length(LSB first), <length> bytes values; Return: none
        readBlockBytes              = 0x85, //Parameters: 3Bytes startaddress(LSB first), 2 Bytes length(LSB first), ; Return:  <length> bytes values
        readWord                    = 0xcd, //Parameters: 3Bytes startaddress(LSB first); Return: 2 Bytes value(LSB first)
        writeWordSequence           = 0xce, //Parameters: 3Bytes buffer startaddress(LSB first), 1 Byte buffer lenth; Return: none

        callSubroutine              = 0x9F, //Parameters: 3Bytes startaddress(LSB first), 8 Word parameters R8-R15(LSB first), ; Return:  8 Word parameters R8-R15(LSB first)
        gotoSubroutine              = 0x41, //Parameters: 3Bytes startaddress(LSB first) ; Return:  none

        eInit                       = 0x31, //Parameters: none ; Return:  none
        swReset                     = 0x32, //Parameters: none ; Return:  none
        getChekcsum                 = 0x33, //Parameters: none ; Return:  1 Word checksum of previous sent block

        testCommunication           = 0x93, //Parameters: none ; Return:  none
        asc0ToAsc1                  = 0xcc, //Parameters: none ; Return:  none




    };

    public enum BootstrapInitConstants : byte
    {
        //--------------------- Initialisation Acknowledges --------------

        I_LOADER_STARTED = 0x001,  //Loader successfully launched
        I_APPLICATION_LOADED = 0x002,   //Application succ. loaded
        I_APPLICATION_STARTED = 0x003,  //Application succ. launched
        I_AUTOBAUD_ACKNOWLEDGE = 0x004,  //Autobaud detection acknowledge
    };

    public enum BootstrapBlockLength : byte
    {
        DEFAULT     = Block128,
        Block32     = 32,
        Block64     = 64,
        Block128    = 128,
    };

    public enum Register : UInt16
    {
        SysCon      = 0xFF12,
        BusCon0     = 0xFF0C,
        BusCon1     = 0xFF14,
        BusCon2     = 0xFF16,
        BusCon3     = 0xFF18,
        BusCon4     = 0xFF1A,
        AddrSel1    = 0xFE18,
        AddrSel2    = 0xFE1A,
        AddrSel3    = 0xFF1C,
        AddrSel4    = 0xFF1E,
    };

}

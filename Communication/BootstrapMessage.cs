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
using System.Collections.ObjectModel;
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
	public delegate void MessageChangedDelegateBoot(BootstrapInterface commInterface, BootstrapMessage message);
	public delegate void MessageSendFinishedDelegateBoot(BootstrapInterface commInterface, BootstrapMessage message, bool sentProperly, bool receivedAnyReplies, bool waitedForAllReplies, uint numRetries);

	public class BootstrapMessage
	{
		public const uint DEFAULT_MAX_NUM_MESSAGE_RETRIES = 1;//2 is default according to ISO14230-2 spec

		//public KWP2000AddressMode mAddressMode;
		public byte mSource;
		public byte mDestination;
		public byte mServiceID;
		public byte[] mData;

		public uint mMaxNumRetries;
		public event MessageSendFinishedDelegate ResponsesFinishedEvent;

		public BootstrapMessage( byte sourceAddress, byte destAddress, byte serviceID, uint maxNumRetries, byte[] data)
		{
			//mAddressMode = addressMode;
			mSource = sourceAddress;
			mDestination = destAddress;
			mServiceID = serviceID;
			mData = data;
			mMaxNumRetries = maxNumRetries;
		}

		public BootstrapMessage( byte sourceAddress, byte destAddress, byte serviceID, byte[] data)
			: this( sourceAddress, destAddress, serviceID, DEFAULT_MAX_NUM_MESSAGE_RETRIES, data)
		{
		}



		public MulticastDelegate GetResponsesFinishedEvent()
		{
			return ResponsesFinishedEvent;
		}

		public int DataLength
		{
			get
			{
				if (mData != null)
				{
					return mData.Length;
				}

				return 0;
			}
		}
	};

	public abstract class BootstrapMessageHelpers
	{



	}
}
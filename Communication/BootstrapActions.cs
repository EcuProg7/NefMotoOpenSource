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

//#define GENERAL_REJECT_ON_REQUEST_UPLOAD_DOWNLOAD_IS_ECU_LOCK_OUT
//#define PROFILE_TRANSFER_DATA

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using Shared;

namespace Communication
{
    public abstract class BootstrapAction : CommunicationAction
    {
        public BootstrapAction(BootstrapInterface commInterface)
            : base(commInterface)
        {
        }

        public override bool Start()
        {
            bool result = base.Start();

            if (result)
            {
                KWP2000CommInterface.ReceivedMessageEvent += this.ReceivedMessageHandler;
            }

            return result;
        }

        private void FinishedReceivingResponsesHandler(KWP2000Interface commInterface, KWP2000Message message, bool sentProperly, bool receivedAnyReplies, bool waitedForAllReplies, uint numRetries)
        {
            lock (this)//lock to ensure we don't accidentally get other callbacks while handling this one
            {
                message.ResponsesFinishedEvent -= this.FinishedReceivingResponsesHandler;

                if (!IsComplete)//always need to check this in case we are getting callbacks after we complete
                {
                    if (!sentProperly)
                    {
                        DisplayStatusMessage("Message failed to send properly.", StatusMessageType.LOG);
                    }
                    else if (!receivedAnyReplies)
                    {
                        DisplayStatusMessage("Did not receive any replies to message.", StatusMessageType.LOG);
                    }

                    ResponsesFinishedHandler(commInterface, message, sentProperly, receivedAnyReplies, waitedForAllReplies, numRetries);
                }
            }
        }

        protected virtual void ResponsesFinishedHandler(KWP2000Interface commInterface, KWP2000Message message, bool sentProperly, bool receivedAnyReplies, bool waitedForAllReplies, uint numRetries)
        {
            if (!sentProperly || !receivedAnyReplies)
            {
                ActionCompletedInternal(false, true);
            }
        }

        private void ReceivedMessageHandler(KWP2000Interface commInterface, KWP2000Message message)
        {
            lock (this)//lock to ensure we don't accidentally get other callbacks while handling this one
            {
                if (!IsComplete)//always need to check this in case we are getting callbacks after we complete
                {
                    try
                    {
                        bool handledMessage = MessageHandler(commInterface, message);

                        if (!handledMessage)
                        {
                            DisplayStatusMessage("Received unhandled message with service ID: " + KWP2000CommInterface.GetServiceIDString(message.mServiceID), StatusMessageType.LOG);

                            if (message.mServiceID == (byte)KWP2000ServiceID.NegativeResponse)
                            {
                                if (message.DataLength >= 2)
                                {
                                    DisplayStatusMessage("Unhandled negative response, request ID: " + KWP2000CommInterface.GetServiceIDString(message.mData[0]) + " response code: " + KWP2000CommInterface.GetResponseCodeString(message.mData[1]), StatusMessageType.LOG);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Fail("Exception: " + e.Message);
                    }
                }
            }
        }

        protected virtual bool MessageHandler(KWP2000Interface commInterface, KWP2000Message message)
        {
            bool handled = false;

            switch (message.mServiceID)
            {
                case (byte)KWP2000ServiceID.StartCommunicationPositiveResponse:
                    {
                        handled = true;
                        break;
                    }
                case (byte)KWP2000ServiceID.StopCommunicationPositiveResponse:
                    {
                        handled = true;
                        break;
                    }
                case (byte)KWP2000ServiceID.TesterPresentPositiveReponse:
                    {
                        handled = true;
                        break;
                    }
                case (byte)KWP2000ServiceID.NegativeResponse:
                    {
                        if (message.DataLength >= 2)
                        {
                            if (message.mData[1] == (byte)KWP2000ResponseCode.RequestCorrectlyReceived_ResponsePending)
                            {
                                handled = true;
                            }
                            else if (message.mData[1] == (byte)KWP2000ResponseCode.Busy_RepeastRequest)
                            {
                                handled = true;
                            }
                            else if (message.mData[0] == (byte)KWP2000ServiceID.StartCommunication)
                            {
                                handled = true;
                            }
                            else if (message.mData[0] == (byte)KWP2000ServiceID.StopCommunication)
                            {
                                handled = true;
                            }
                        }

                        break;
                    }
            }

            return handled;
        }

        protected override void ActionCompletedInternal(bool success, bool communicationError)
        {
            if (!IsComplete)
            {
                KWP2000CommInterface.ReceivedMessageEvent -= this.ReceivedMessageHandler;

                base.ActionCompletedInternal(success, communicationError);
            }
        }

        protected KWP2000Message SendMessage(byte serviceID)
        {
            var message = KWP2000CommInterface.SendMessage(serviceID);

            if (message != null)
            {
                message.ResponsesFinishedEvent += this.FinishedReceivingResponsesHandler;
            }

            return message;
        }

        protected KWP2000Message SendMessage(byte serviceID, byte[] data)
        {
            var message = KWP2000CommInterface.SendMessage(serviceID, data);

            if (message != null)
            {
                message.ResponsesFinishedEvent += this.FinishedReceivingResponsesHandler;
            }

            return message;
        }

        protected KWP2000Message SendMessage(byte serviceID, uint maxNumRetries, byte[] data)
        {
            var message = KWP2000CommInterface.SendMessage(serviceID, maxNumRetries, data);

            if (message != null)
            {
                message.ResponsesFinishedEvent += this.FinishedReceivingResponsesHandler;
            }

            return message;
        }

        protected BootstrapInterface BootstrapCommInterface
        {
            get
            {
                return CommInterface as BootstrapInterface;
            }
            set
            {
                CommInterface = value;
            }
        }
    };

    public class Configure4MBActionBoot : BootstrapAction
    {
        public Configure4MBActionBoot(BootstrapInterface commInteface)
            : base(commInteface)
        {
            
        }

        public override bool Start()
        {
            bool started = false;

            if (base.Start())
            {
                byte[] messageData = new byte[1];
                messageData[0] = mLocalIdentifier;
                SendMessage((byte)KWP2000ServiceID.ReadDataByLocalIdentifier, messageData);

                started = true;
            }

            return started;
        }

        protected override bool MessageHandler(BootstrapInterface commInterface, KWP2000Message message)
        {
            bool handled = base.MessageHandler(commInterface, message);

            if (!handled)
            {
            }

            if (!handled)
            {
                ActionCompleted(false);
            }

            return handled;
        }

        private byte mLocalIdentifier;
    }


}

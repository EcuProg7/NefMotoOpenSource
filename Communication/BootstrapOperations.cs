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

//#define LOG_PERFORMANCE

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Data;

using Shared;

namespace Communication
{
	public abstract class BootstrapOperation : CommunicationOperation
	{
		public const byte DEFAULT_MAX_BLOCK_SIZE = 254;

		public BootstrapOperation(BootstrapInterface commInterface)
            : base(commInterface)
		{	

		}

        protected override void ResetOperation()
        {
            mState = State.Begin;

            base.ResetOperation();
        }

        protected override CommunicationAction NextAction()
        {
            CommunicationAction nextAction = null;

            if (IsRunning)
            {
                nextAction = base.NextAction();

                if (nextAction == null)
                {
                    //determine the next state
                    switch (mState)
                    {
                        case State.Begin:
                        {

                            mState = State.Finished;
                            break;
                        }
                    }

                    //start the action for the new state
                    switch (mState)
                    {
                        case State.Configure4MB:
                            {
                                nextAction = new Configure4MBActionBoot(BootstrapCommInterface);
                                break;
                            }
                    }
                }
            }

			mMyLastStartedAction = nextAction;

            return nextAction;
        }

        protected override void OnActionCompleted(CommunicationAction action, bool success)
        {
			if (action == mMyLastStartedAction)
			{
                #region Configure4MB
                if (mState == State.Configure4MB)
                {
                    success = action.CompletedWithoutCommunicationError;
                }
                #endregion
            }

            mMyLastStartedAction = null;

            base.OnActionCompleted(action, success);
        }
     

        private enum State
        {
            Begin,
            Configure4MB,
            Finished            
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

        private State mState;

		private CommunicationAction mMyLastStartedAction;
	};

    public class ReadExternalFlashBootOperation : BootstrapOperation
    {
        public class ReadExternalFlashBootSettings
        {
            public bool CheckIfSectorReadRequired = true;
            public bool OnlyReadNonMatchingSectors = false;
            public bool VerifyReadData = true;

            public SecurityAccessAction.SecurityAccessSettings SecuritySettings = new SecurityAccessAction.SecurityAccessSettings();
        }

        public ReadExternalFlashBootOperation(BootstrapInterface commInterface, ReadExternalFlashBootSettings readSettings, IEnumerable<MemoryImage> flashBlockList)
            : base(commInterface)
        {

            mFlashBlockList = flashBlockList;
            mCurrentBlock = mFlashBlockList.GetEnumerator();
            mCurrentBlock.MoveNext();
            mNumAttemptsForCurrentBlock = 0;
            mMaxBlockSize = (byte)BootstrapBlockLength.Block128;

            mHasVerfiedRequestUploadSupported = false;

            mState = ReadingState.Start;

            bool shouldValidateMemoryLayout = true;

            if (shouldValidateMemoryLayout)
            {
               // mState = ReadingState.ValidateMemoryLayout;
            }

            CheckIfSectorsRequireRead = readSettings.CheckIfSectorReadRequired;
            OnlyReadRequiredSectors = readSettings.OnlyReadNonMatchingSectors;
            ShouldVerifyReadSectors = readSettings.VerifyReadData;

            ElapsedTimeReadingUnrequiredSectors = TimeSpan.Zero;
            ElapsedTimeCheckingIfSectorsRequireReading = TimeSpan.Zero;
            ElapsedTimeVerifyingReadSectors = TimeSpan.Zero;

            mTotalBytesValidated = 0;
            mTotalBytesToRead = 0;

            foreach (var currentBlock in flashBlockList)
            {
                Debug.Assert(currentBlock.StartAddress % 2 == 0, "start address is not a multiple of 2");
                Debug.Assert(currentBlock.Size > 0, "size is zero");
                Debug.Assert(currentBlock.Size % 2 == 0, "size is not a multiple of 2");

                mTotalBytesToRead += currentBlock.Size;
            }
        }

        public bool CheckIfSectorsRequireRead { get; private set; }
        public bool OnlyReadRequiredSectors { get; private set; }
        public bool ShouldVerifyReadSectors { get; private set; }

        public TimeSpan ElapsedTimeReadingUnrequiredSectors { get; private set; }
        public TimeSpan ElapsedTimeCheckingIfSectorsRequireReading { get; private set; }
        public TimeSpan ElapsedTimeVerifyingReadSectors { get; private set; }

        public IEnumerable<MemoryImage> FlashBlockList
        {
            get { return mFlashBlockList; }
        }

        protected override CommunicationAction NextAction()
        {
            var nextAction = base.NextAction();

            if (nextAction == null)
            {
                var currentMemoryImage = mCurrentBlock.Current;

                if (currentMemoryImage != null)
                {
                    if (mState == ReadingState.Start)
                    {
                        CommInterface.DisplayStatusMessage("Starting to read data block.", StatusMessageType.USER);

                        mCurrentSectorRequiresRead = true;

                        //we can only do checksum calculations if we are sure we can do a successful request upload.
                        //otherwise we can cause a "Programming Not Finished" error code to be stored by an incorrect checksum calculation.
                        /*                        if (CheckIfSectorsRequireRead && mHasVerfiedRequestUploadSupported)
                                                {
                                                    mState = ReadingState.CheckIfReadRequired;
                                                }*/
                        //else
                        //{
                        //mState = ReadingState.RequestUpload;

                        mState = ReadingState.FinishedAll;
                        // }
                    }

                    switch (mState)
                    {


                        case ReadingState.FinishedAll:
                            {
                                nextAction = null;
                                break;
                            }

                        default:
                            {
                                Debug.Fail("Unknown reading state");
                                break;
                            }
                    }
                }

                mMyLastStartedAction = nextAction;
            }

            return nextAction;
        }

        protected override void OnActionCompleted(CommunicationAction action, bool success)
        {
            //only pay attention to actions this code started
            if (action == mMyLastStartedAction)
            {

            }

            mMyLastStartedAction = null;

            base.OnActionCompleted(action, success);
        }

        private void NotifyBytesReadHandler(UInt32 newAmountWritten, UInt32 totalWritten, UInt32 totalToWrite)
        {
            mCurrentBlockBytesRead += newAmountWritten;

            OnUpdatePercentComplete(((float)mCurrentBlockBytesRead + mTotalBytesValidated) / ((float)mTotalBytesToRead) * 100.0f);
        }

        private enum ReadingState
        {

            Start,
            RequestUpload,
            TransferData,
            FinishedBlock,
            FinishedAll,
            CompleteFailedReadWithChecksumCalculation
        }

        private CommunicationAction mMyLastStartedAction;
        private ReadingState mState;
        private IEnumerable<MemoryImage> mFlashBlockList;
        private IEnumerator<MemoryImage> mCurrentBlock;
        private byte mMaxBlockSize;
        private bool mHasVerfiedRequestUploadSupported;
        private int mNumAttemptsForCurrentBlock;
        private uint mCurrentBlockBytesRead;
        private uint mTotalBytesToRead;
        private uint mTotalBytesValidated;

        private bool mCurrentSectorRequiresRead;
    };

}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonetaVerdeWalletC
{
    class BlockChainManager
    {
        public static void Sync()
        {
            int blockHeightDaemon = BlockChainManager.GetDaemonBlockHeight();   //Gets current daemonHeight
            int blockHeightWallet = Convert.ToInt32(SettingsManager.getAppSetting("blockHeightWallet"));

            //The sole purpose of this method is to check the current maxBlockHeight on the daemon and keep building the blocks for the wallet untill this level
            do
            {
                blockHeightWallet++;
                BlockChainManager.GetBlock(blockHeightWallet);  //Update block
                SettingsManager.setAppSetting("blockHeightWallet", Convert.ToString(blockHeightWallet));

            } while (blockHeightDaemon > blockHeightWallet);
            //Here we need to program a refresh polling mechanism which will poll for example every minute if the blockheight of the daemon updated and if we need to sync again
        }       
        public static int GetDaemonBlockHeight()
        {
            //Get current blockheight from Daemon
            string BlockCountStr = "190000000";//TODO, real value from Daemon RPCConnectionManager.TestRPCJson("getblockcount", "");

            return Convert.ToInt32(BlockCountStr);  //We expect a 32 bit int to be sufficient, could be altered to INT64 here
        }
        public static void GetBlock(int _blockHeightToFetch)
        {
            //Fetch block from Daemon and add it to file
        } 
    }
}
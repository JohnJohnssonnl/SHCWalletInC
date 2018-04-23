﻿using System.Security.Cryptography;
using System.Text;
using System;
using Org.BouncyCastle.Crypto.Digests;


namespace MonetaVerdeWalletC
{
    class KeyManager
    {
		/*OK, as far as I figured out now we need to do the following:
		- Step 1: Generate private spend key (random 32 bit 64 chars)
		- Step 2: Generate private view key (random 32 bit 64 chars)
		- Step 3: Generate public spend key (ed25519 scalarmult)
		- Step 4: Generate public view key (ed25519 scalarmult)
		- Step 5: Add network byte to public spend and view key (For monetaverde: 0x1A2B // addresses start with "Vd")
		- Step 6: this 65 byte needs to be hashed with keccak 256 
		- Step 7: Add first 4 bytes from hashed value to the 65 bytes (at the end) 
		- Step 8: Convert the 69 bytes to Base58
		- Step 9: Maybe we'd like to have one "superkey" which unlocks private spend and view key...who knows
		- Known bug: prefix
		*/
		public static String GenerateKeySet(string _passCode = "12345"/*TODO: Implement*/, string _userId = "123546789"/*TODO: Implement*/)
		{
			byte[] privateSpendKey	= KeyManager.GeneratePrivateSpendKey(_passCode, _userId);				//Done	
			byte[] privateViewKey	= KeyManager.GeneratePrivateViewKey(_passCode, privateSpendKey);		//Done
			byte[] publicSpendKey	= KeyManager.GeneratePubSpendKey(privateSpendKey);						//Done
			byte[] publicViewKey	= KeyManager.GeneratePubViewKey(privateViewKey);                        //Done
			byte[] networkByte = new byte[1];//KeyManager.StringToByteArray("0x12");//("0x1A2B");			//Done (Should start with Vd)
			networkByte[0] = 18;
			byte[] hashedKey		= KeyManager.HashKeccak256(publicSpendKey, publicViewKey, networkByte); //Done
			string publicAddress    = KeyManager.ConvertToPubAddressChunked(hashedKey);                     //Done
			
			return publicAddress;   //Return the public address, the rest we have to store somewhere safe...
		}

		public static byte[] GeneratePrivateSpendKey(string _passCode, string _userId)
		{
			byte[] bytes = Encoding.Unicode.GetBytes(_passCode+ _userId);
			SHA256Managed hashstring = new SHA256Managed();
			byte[] hash = hashstring.ComputeHash(bytes);
			return hash;
		}

		public static byte[] GeneratePrivateViewKey(string _passCode, byte[] privateSpendKey)
		{
			byte[] passCodeByte = Encoding.Unicode.GetBytes(_passCode);
			SHA256Managed hashstring = new SHA256Managed();
			byte[] hash = hashstring.ComputeHash(passCodeByte);
			
			byte[] ret = new byte[32];		//32 byte result (4 bytes pass + 28 bytes spendkey)

			Array.Copy(passCodeByte, 0, ret, 0, 4);
			Array.Copy(privateSpendKey, 0, ret, 3, 28);

			return ret;
		}
		public static byte[] GeneratePubSpendKey(byte[] privateSpendKey)
		{
			RNGCryptoServiceProvider.Create().GetBytes(privateSpendKey);
			byte[] publicKey = Cryptographic.Ed25519.PublicKey(privateSpendKey);

			return publicKey;
		}
		public static byte[] GeneratePubViewKey(byte[] privateViewKey)
		{
			RNGCryptoServiceProvider.Create().GetBytes(privateViewKey);
			byte[] publicKey = Cryptographic.Ed25519.PublicKey(privateViewKey);

			return publicKey;
		}

		public static byte[] HashKeccak256(byte[] publicSpendKey, byte[] publicViewKey, byte[] networkByte)
		{
			//We need 69 bytes: 65 from the public spend key, public view key and networkbyte + 4 from the hashing of keccak
			byte[] origByteSet = new byte[65];	//Monero is 65, but we have 67 for MonetaVerde due to its hex network byte structure
			byte[] hashFirst4 = new byte[4];
			byte[] ret = new byte[69];
			System.Buffer.BlockCopy(networkByte, 0, origByteSet, 0, networkByte.Length);
			System.Buffer.BlockCopy(publicSpendKey, 0, origByteSet, networkByte.Length, publicSpendKey.Length);
			System.Buffer.BlockCopy(publicViewKey, 0, origByteSet, networkByte.Length + publicSpendKey.Length, publicViewKey.Length);

			byte[] resultHashedKec256 = KeyManager.Keccak256Helper(origByteSet);	//Run keccak

			Array.Copy(resultHashedKec256, 0, hashFirst4, 0, 4); //Copy the first 4 bytes from the hash

			System.Buffer.BlockCopy(origByteSet, 0, ret, 0, origByteSet.Length);  //Add the 4 bytes to complete the 69 bytes array
			System.Buffer.BlockCopy(hashFirst4, 0, ret, publicSpendKey.Length + publicViewKey.Length + networkByte.Length, hashFirst4.Length);	//Add the 4 bytes to complete the 69 bytes array

			return ret;	//Return the hashed array
		}

		public static string ConvertToPubAddressChunked(byte[] array)
		{
			int arrayLength = array.Length;
			string ret = "";
			int numOfCopyInt;
			byte[] subArray = new byte[8];	//Max 8 bytes

			for (int I = 0;I< arrayLength; I+=8)
			{
				numOfCopyInt = arrayLength - I > 7 ? 8 : arrayLength - I;
				subArray = new byte[numOfCopyInt];
				Array.Copy(array, I, subArray, 0, numOfCopyInt);
				ret += KeyManager.ConvertToPubAddress(subArray);
			}

			return ret;
		}

		public static string ConvertToPubAddress(byte[] array)
		{
			const string ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
			string retString = string.Empty;
			System.Numerics.BigInteger encodeSize = ALPHABET.Length;
			System.Numerics.BigInteger arrayToInt = 0;
			for (int i = 0; i < array.Length; ++i)
			{
				arrayToInt = arrayToInt * 256 + array[i];
			}
			while (arrayToInt > 0)
			{
				int rem = (int)(arrayToInt % encodeSize);
				arrayToInt /= encodeSize;
				retString = ALPHABET[rem] + retString;
			}
			for (int i = 0; i < array.Length && array[i] == 0; ++i)
				retString = ALPHABET[0] + retString;

			return retString;
		}

		//Helping method(s)
		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			Console.WriteLine(hex.ToString());
			return hex.ToString();
		}

		public static byte[] Keccak256Helper(byte[] _input)
		{
			KeccakDigest Kec256 = new KeccakDigest(256);
			Kec256.Reset();
			byte[] resultHashedKec256 = new byte[32];
			Kec256.BlockUpdate(_input, 0, _input.Length);
			Kec256.DoFinal(resultHashedKec256, 0);

			return resultHashedKec256;
		}

		public static byte[] StringToByteArray(string hex)
		{
			if (hex.Length % 2 == 1)
				throw new Exception("The binary key cannot have an odd number of digits");

			byte[] arr = new byte[hex.Length >> 1];

			for (int i = 0; i < hex.Length >> 1; ++i)
			{
				arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
			}

			return arr;
		}
		public static int GetHexVal(char hex)
		{
			int val = (int)hex;
			//For uppercase A-F letters:
			//return val - (val < 58 ? 48 : 55);
			//For lowercase a-f letters:
			//return val - (val < 58 ? 48 : 87);
			//Or the two combined, but a bit slower:
			return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
		}
    }
}
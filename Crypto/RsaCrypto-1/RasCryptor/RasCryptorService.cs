using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Cryptor
{
    public class RasCryptorService
    {
        private Encoding _encode = Encoding.UTF8;

        public Encoding Encode
        {
            get { return _encode; }
            set { _encode = value; }
        }

        private string _privateKey;

        public string PrivateKey
        {
            get { return _privateKey; }
            set { _privateKey = value; }
        }

        private string _publicKey;

        public string PublicKey
        {
            get { return _publicKey; }
            set { _publicKey = value; }
        }

        private string _KeyContainerName;

        public string KeyContainerName
        {
            get { return _KeyContainerName; }
            set { _KeyContainerName = value; }
        }

        private RSACryptoServiceProvider _rsaCrypto;
        private CspParameters _cspParameters = null;
        private static readonly int KEY_SIZE = 1024;

        public RasCryptorService()
            : this("RsaCspParameters_Key")
        {
        }

        public RasCryptorService(string KeyContainerName)
        {
            this.KeyContainerName = KeyContainerName;
            this.initial();
        }

        private void initial()
        {
            if (this._rsaCrypto != null)
            {
                return;
            }
            //使用金鑰容器
            this._cspParameters = new CspParameters();
            this._cspParameters.KeyContainerName = this.KeyContainerName;
            this._cspParameters.Flags = CspProviderFlags.UseMachineKeyStore;
            this._cspParameters.ProviderName = "Microsoft Strong Cryptographic Provider";
            this._cspParameters.ProviderType = 1;

            this._rsaCrypto = new RSACryptoServiceProvider(KEY_SIZE, this._cspParameters);
            this.PrivateKey = this._rsaCrypto.ToXmlString(true);
            this.PublicKey = this._rsaCrypto.ToXmlString(false);
        }

        public void GenerateKey()
        {
            if (this._rsaCrypto != null)
            {
                this._rsaCrypto.PersistKeyInCsp = false;
                this._rsaCrypto.Clear();
            }
            this.initial();
        }

        public byte[] Encryptor(byte[] OriginalData)
        {
            if (OriginalData == null || OriginalData.Length <= 0)
            {
                throw new NotSupportedException();
            }
            if (this._rsaCrypto == null)
            {
                throw new ArgumentNullException();
            }
            this._rsaCrypto.FromXmlString(this.PublicKey);

            int bufferSize = (this._rsaCrypto.KeySize / 8) - 11;
            byte[] buffer = new byte[bufferSize];

            //分段加密
            using (MemoryStream input = new MemoryStream(OriginalData))
            using (MemoryStream ouput = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, bufferSize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] encrypt = this._rsaCrypto.Encrypt(temp, false);
                    ouput.Write(encrypt, 0, encrypt.Length);
                }
                return ouput.ToArray();
            }
        }

        public byte[] Decryptor(byte[] EncryptDada)
        {
            if (EncryptDada == null || EncryptDada.Length <= 0)
            {
                throw new NotSupportedException();
            }

            this._rsaCrypto.FromXmlString(this.PrivateKey);
            int keySize = this._rsaCrypto.KeySize / 8;
            byte[] buffer = new byte[keySize];

            using (MemoryStream input = new MemoryStream(EncryptDada))
            using (MemoryStream output = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, keySize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] decrypt = this._rsaCrypto.Decrypt(temp, false);
                    output.Write(decrypt, 0, decrypt.Length);
                }
                return output.ToArray();
            }
        }

        #region encrypt

        public string EncryptString(string OriginalString)
        {
            if (string.IsNullOrEmpty(OriginalString))
            {
                throw new NotSupportedException();
            }
            var originalData = this.Encode.GetBytes(OriginalString);
            var encryptData = this.Encryptor(originalData);
            var base64 = Convert.ToBase64String(encryptData);
            return base64;
        }

        public void EncryptFile(string OriginalFile, string EncryptFile)
        {
            using (FileStream originalStream = new FileStream(OriginalFile, FileMode.Open, FileAccess.Read))
            using (FileStream encryptStream = new FileStream(EncryptFile, FileMode.Create, FileAccess.Write))
            {
                //加密
                var dataByteArray = new byte[originalStream.Length];
                originalStream.Read(dataByteArray, 0, dataByteArray.Length);
                var encryptData = Encryptor(dataByteArray);
                //寫檔
                encryptStream.Write(encryptData, 0, encryptData.Length);
            }
        }

        public byte[] EncryptObject(object OriginalObject)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                //物件轉為Byte[]
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memory, OriginalObject);
                memory.Seek(0, SeekOrigin.Begin);
                var encrypt = Encryptor(memory.ToArray());
                return encrypt;
            }
        }

        #endregion encrypt

        #region decrypt

        public string DecryptString(string EncryptString)
        {
            if (string.IsNullOrEmpty(EncryptString))
            {
                throw new NotSupportedException();
            }
            var encryptData = Convert.FromBase64String(EncryptString);
            var decryptData = this.Decryptor(encryptData);
            var decryptString = this.Encode.GetString(decryptData);
            return decryptString;
        }

        public void DecryptFile(string EncrytpFile, string DecrytpFile)
        {
            using (FileStream encrytpStream = new FileStream(EncrytpFile, FileMode.Open, FileAccess.Read))
            using (FileStream decrytpStream = new FileStream(DecrytpFile, FileMode.Create, FileAccess.Write))
            {
                //解密
                var dataByteArray = new byte[encrytpStream.Length];
                encrytpStream.Read(dataByteArray, 0, dataByteArray.Length);
                var decryptData = this.Decryptor(dataByteArray);
                //寫檔
                decrytpStream.Write(decryptData, 0, decryptData.Length);
            }
        }

        public T DecryptObject<T>(byte[] EncryptData)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                var decrypt = Decryptor(EncryptData);
                memory.Write(decrypt, 0, decrypt.Length);
                memory.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(memory);
            }
        }

        #endregion decrypt

        public string GetSignature(string OriginalString)
        {
            if (string.IsNullOrEmpty(OriginalString))
            {
                throw new ArgumentNullException();
            }
            this._rsaCrypto.FromXmlString(this.PrivateKey);
            var originalData = this.Encode.GetBytes(OriginalString);
            var singData = this._rsaCrypto.SignData(originalData, new SHA1CryptoServiceProvider());
            var SignatureString = Convert.ToBase64String(singData);
            return SignatureString;
        }

        public bool VerifySignature(string OriginalString, string SignatureString)
        {
            if (string.IsNullOrEmpty(OriginalString))
            {
                throw new ArgumentNullException();
            }

            this._rsaCrypto.FromXmlString(this.PublicKey);
            var originalData = this.Encode.GetBytes(OriginalString);
            var signatureData = Convert.FromBase64String(SignatureString);
            var isVerify = this._rsaCrypto.VerifyData(originalData, new SHA1CryptoServiceProvider(), signatureData);
            return isVerify;
        }
    }
}
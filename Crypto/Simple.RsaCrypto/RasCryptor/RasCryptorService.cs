using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RasCryptor
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
        private static readonly int BLOCK_SIZE = 1024;

        public RasCryptorService()
            : this("RsaCspParameters_Key")
        {
        }

        public RasCryptorService(string KeyContainerName)
        {
            this.KeyContainerName = KeyContainerName;
            this.GenerateKey();
        }

        public void GenerateKey()
        {
            //使用金鑰容器
            this._cspParameters = new CspParameters(BLOCK_SIZE);
            this._cspParameters.KeyContainerName = this.KeyContainerName;
            this._cspParameters.Flags = CspProviderFlags.UseMachineKeyStore;
            this._cspParameters.ProviderName = "Microsoft Strong Cryptographic Provider";
            this._cspParameters.ProviderType = 1;

            if (this._rsaCrypto != null)
            {
                this._rsaCrypto.PersistKeyInCsp = false;
                this._rsaCrypto.Clear();
            }

            this._rsaCrypto = new RSACryptoServiceProvider(this._cspParameters);
            this.PrivateKey = this._rsaCrypto.ToXmlString(true);
            this.PublicKey = this._rsaCrypto.ToXmlString(false);
        }

        public string EncryptString(string OriginalString)
        {
            if (string.IsNullOrEmpty(OriginalString))
            {
                throw new NotSupportedException();
            }
            var originalData = this.Encode.GetBytes(OriginalString);
            var encryptData = this.encryptor(originalData);
            var base64 = Convert.ToBase64String(encryptData);
            return base64;
        }

        public void EncryptFile(string OriginalFile, string EncrytpFile)
        {
            using (FileStream originalStream = new FileStream(OriginalFile, FileMode.Open, FileAccess.Read))
            using (FileStream encrytpStream = new FileStream(EncrytpFile, FileMode.Create, FileAccess.Write))
            {
                //加密
                var dataByteArray = new byte[originalStream.Length];
                originalStream.Read(dataByteArray, 0, dataByteArray.Length);
                var encryptData = encryptor(dataByteArray);
                //寫檔
                encrytpStream.Write(encryptData, 0, encryptData.Length);
            }
        }

        private byte[] encryptor(byte[] OriginalData)
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

            var encryptData = this._rsaCrypto.Encrypt(OriginalData, false);
            return encryptData;
        }

        public string DecryptString(string EncryptString)
        {
            if (string.IsNullOrEmpty(EncryptString))
            {
                throw new NotSupportedException();
            }
            var encryptData = Convert.FromBase64String(EncryptString);
            var decryptData = this.decryptor(encryptData);
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
                var decryptData = this.decryptor(dataByteArray);
                //寫檔
                decrytpStream.Write(decryptData, 0, decryptData.Length);
            }
        }

        private byte[] decryptor(byte[] EncryptDada)
        {
            if (EncryptDada == null || EncryptDada.Length <= 0)
            {
                throw new NotSupportedException();
            }

            this._rsaCrypto.FromXmlString(this.PrivateKey);
            var decrtpyData = this._rsaCrypto.Decrypt(EncryptDada, false);
            return decrtpyData;
        }

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
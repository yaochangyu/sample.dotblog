using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Cryptor
{
    public class AesCryptorService
    {
        private byte[] _keyData;

        public byte[] KeyData
        {
            get { return _keyData; }
            set { _keyData = value; }
        }

        private byte[] _ivData;

        public byte[] IvData
        {
            get { return _ivData; }
            set { _ivData = value; }
        }

        private string _iv = "12345678";

        public string IV
        {
            get
            {
                this.IvData = _md5.ComputeHash(this.Encoding.GetBytes(this._iv));
                return _iv;
            }
            set { _iv = value; }
        }

        private string _key = "12345678";

        public string Key
        {
            get
            {
                this.KeyData = _sha256.ComputeHash(this.Encoding.GetBytes(this._key));
                return _key;
            }
            set { _key = value; }
        }

        private Encoding _encoding = Encoding.UTF8;

        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        private AesCryptoServiceProvider _aes = new AesCryptoServiceProvider();
        private MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider();
        private SHA256CryptoServiceProvider _sha256 = new SHA256CryptoServiceProvider();

        public AesCryptorService()
        {
            this._aes = new AesCryptoServiceProvider();

            this.KeyData = _sha256.ComputeHash(this.Encoding.GetBytes(this._key));
            this.IvData = _md5.ComputeHash(this.Encoding.GetBytes(this._iv));
            this._aes.Key = KeyData;
            this._aes.IV = IvData;
        }

        public byte[] Encryptor(byte[] originalData)
        {
            _aes.Key = KeyData;
            _aes.IV = IvData;
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, _aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(originalData, 0, originalData.Length);
                cs.FlushFinalBlock();
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        public byte[] Decryptor(byte[] encryptData)
        {
            _aes.Key = KeyData;
            _aes.IV = IvData;
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, _aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(encryptData, 0, encryptData.Length);
                cs.FlushFinalBlock();
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        #region encrypt

        public string EncryptString(string OriginalString)
        {
            var originalData = this.Encoding.GetBytes(OriginalString);
            var encryptData = this.Encryptor(originalData);
            return Convert.ToBase64String(encryptData);
        }

        public void EncryptFile(string OriginalFile, string EncryptFile)
        {
            using (FileStream originalStream = new FileStream(OriginalFile, FileMode.Open, FileAccess.Read))
            using (FileStream encryptStream = new FileStream(EncryptFile, FileMode.Create, FileAccess.Write))
            {
                var dataByteArray = new byte[originalStream.Length];
                originalStream.Read(dataByteArray, 0, dataByteArray.Length);
                //加密
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
            var encryptData = Convert.FromBase64String(EncryptString);
            var decryptData = this.Decryptor(encryptData);
            return this.Encoding.GetString(decryptData);
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
    }
}
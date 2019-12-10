using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cryptor;

namespace Cryptor
{
    public class CryptorService
    {
        private RasCryptorService _rsa = new RasCryptorService("RsaCspParameters_Key");
        private AesCryptorService _aes;

        private static readonly byte[] DEFAULT_SALT = new byte[] { 0x0A, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0xF1 };
        private Rfc2898DeriveBytes _rfcKey;
        private static readonly string DEFAULT_KEY = "Ej1ww4YznZ0I73ONOAFJBihx6BL/i1VBBm9JvOH8nmYkKIG4G+mZw+IpTLRFTLO6fr2Eo2I3DtiX9pQq8qmWQNZtNQlK5nM+v22yW7RleyylgVWAbm+Vlf+Y30CZTtj9lsqKycydSJin/NOcV0lQf0A0oCgyjSNT5f24HSNTV5OtqD6Oomc7YUL3lMc1/Jti4+AiAMHSiXbulWR17Pjr2nx+NZXGJteWipCsr/jF+dTR98bEQztZD7O0vSiM5D5O5m7Wzb4VSv5JSeAACS4HE8Mo4M4sKgy1BOVS5+Lsiq1RZqW8v2tBWw9i/rGmhbbHNaWt1yQZj3ckbLVgfd4LuQ==";

        private string _key = DEFAULT_KEY;

        internal string Key
        {
            get
            {
                var base64 = Convert.FromBase64String(_key);
                var decrypt = this._rsa.Decryptor(base64);
                return Encoding.Default.GetString(decrypt);
            }
            set
            {
                _key = value;
            }
        }

        public string GenerateNewKey(string Key)
        {
            //this._rfcKey = new Rfc2898DeriveBytes(Key, DEFAULT_SALT);
            //var key = this._rfcKey.GetBytes(8);
            var encryptKey = this._rsa.EncryptString(Key);
            return encryptKey;
        }

        public AesCryptorService CreateCryptService()
        {
            return this.CreateCryptService(DEFAULT_KEY);
        }

        public AesCryptorService CreateCryptService(string Key)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return this._aes;
            }

            this.Key = Key;
            this._aes = new AesCryptorService();
            this._aes.Key = this.Key;
            return this._aes;
        }
    }
}
using System;

namespace Lab.CertFromCA
{
    [Flags]
    public enum Format
    {
        CR_IN_FORMATANY = 0x0,
        CR_IN_PKCS10    = 0x100,
        CR_IN_KEYGEN    = 0x200,
        CR_IN_PKCS7     = 0x300,
        CR_IN_CMC       = 0x400,
        CR_OUT_BASE64   = 0x1,
        CR_OUT_CHAIN    = 0x100,
    }
}
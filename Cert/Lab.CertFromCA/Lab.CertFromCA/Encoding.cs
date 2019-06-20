namespace Lab.CertFromCA
{
    public enum Encoding
    {
        CR_IN_BASE64HEADER  = 0x0,
        CR_IN_BASE64        = 0x1,
        CR_IN_BINARY        = 0x2,
        CR_IN_ENCODEANY     = 0xff,
        CR_OUT_BASE64HEADER = 0x0,
        CR_OUT_BASE64       = 0x1,
        CR_OUT_BINARY       = 0x2,
    }
}
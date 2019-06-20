namespace Lab.CertFromCA
{
    public enum RequestDisposition
    {
        CR_DISP_INCOMPLETE         = 0,
        CR_DISP_ERROR              = 0x1,
        CR_DISP_DENIED             = 0x2,
        CR_DISP_ISSUED             = 0x3,
        CR_DISP_ISSUED_OUT_OF_BAND = 0x4,
        CR_DISP_UNDER_SUBMISSION   = 0x5,
        CR_DISP_REVOKED            = 0x6,
        CCP_DISP_INVALID_SERIALNBR = 0x7,
        CCP_DISP_CONFIG            = 0x8,
        CCP_DISP_DB_FAILED         = 0x9
    }
}
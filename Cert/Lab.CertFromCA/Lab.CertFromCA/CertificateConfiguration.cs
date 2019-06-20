namespace Lab.CertFromCA
{
    public enum CertificateConfiguration:int
    {
        CC_DEFAULTCONFIG           = 0x0,
        CC_UIPICKCONFIG            = 0x1,
        CC_FIRSTCONFIG             = 0x2,
        CC_LOCALCONFIG             = 0x3,
        CC_LOCALACTIVECONFIG       = 0x4,
        CC_UIPICKCONFIGSKIPLOCALCA = 0x5
    }
}
namespace Lab.HangfireManager.AspNetCore31
{
    public class HangfireServiceOption
    {
        public bool   IsUseHangfireDashboard { get; set; }
        public bool   IsUseHangfireServer    { get; set; }
        public string ServiceName            { get; set; }
        //public string ServiceDisplayName { get; set; }
        //public string ServiceDescription { get; set; }
        public int         WorkerCount            { get; set; } = 20;
        public string      Queues                 { get; set; } = "default";
        public StorageType StorageType            { get; set; } = StorageType.LocalStorage;
        public string      nameOrConnectionString { get; set; }
        public string      HangfireDashboardUrl   { get; set; } = "/hangfire";
        //public string AppWebSite { get; set; }
        //public string LoginUser { get; set; }
        //public string LoginPwd { get; set; }
    }
}
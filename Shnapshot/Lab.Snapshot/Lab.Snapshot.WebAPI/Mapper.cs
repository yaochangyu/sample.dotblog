namespace Lab.Snapshot.WebAPI;

public class Mapper
{
    public class DomainToResponseMappingProfile : AutoMapper.Profile
    {
        public DomainToResponseMappingProfile()
        {
            this.CreateMap<Lab.Snapshot.DB.MemberDataEntity, Lab.Snapshot.WebAPI.ServiceModels.MemberResponse>();

            this.CreateMap<Lab.Snapshot.DB.Account, Lab.Snapshot.WebAPI.ServiceModels.Account>();
            this.CreateMap<Lab.Snapshot.DB.Profile, Lab.Snapshot.WebAPI.ServiceModels.Profile>();
            this.CreateMap<Lab.Snapshot.WebAPI.ServiceModels.Account, Lab.Snapshot.DB.Account>();
            this.CreateMap<Lab.Snapshot.WebAPI.ServiceModels.Profile, Lab.Snapshot.DB.Profile>();
        }
    }
}
namespace Lab.ChangeTracking.Domain;

public class EmployeeAggregate
{
    private IEmployeeRepository _repository;
    private IUUIdProvider _idProvider;
    private ISystemClock _systemClock;
    private IAccessContext _accessContext;
    private EmployeeEntity _employeeEntity;

    public void Create()
    {
    }
    public EmployeeAggregate(IEmployeeRepository repository,
                             IUUIdProvider idProvider,
                             ISystemClock systemClock,
                             IAccessContext accessContext)
    {
        this._repository = repository;
        this._idProvider = idProvider;
        this._systemClock = systemClock;
        this._accessContext = accessContext;
    }

    void Save()
    {
        
    }
}
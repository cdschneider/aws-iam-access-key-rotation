using AccessKeyActions.Models;

namespace AccessKeyActions.Repositories;

public interface IAccessKeyRepository
{
    Task<AccessKeyEntity> GetByIdAsync(string id);
    Task<IEnumerable<AccessKeyEntity>> GetByUsernameAsync(string username);
}
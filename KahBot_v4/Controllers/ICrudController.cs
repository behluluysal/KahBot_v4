using Core.Models;

namespace KahBot_v4.Controllers
{
    public interface ICrudController
    {
        Task<bool> Add(Counter book);
        Task<bool> Put(Guid guid, Counter counter);
    }
}
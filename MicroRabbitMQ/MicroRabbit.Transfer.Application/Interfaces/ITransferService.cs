using MicroRabbit.Transfer.Domain.Models;

namespace MicroRabbit.Transer.Application.Interfaces
{
    public interface ITransferService
    {
        IEnumerable<TransferLog> GetTransferLogs();
    }
}

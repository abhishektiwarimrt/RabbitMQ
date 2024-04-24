using MicroRabbit.Transfer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroRabbit.Transfer.Data.Context
{
    public class TransferDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<TransferLog> TransferLogs { get; set; }
    }
}

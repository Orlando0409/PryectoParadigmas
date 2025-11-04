using Microsoft.EntityFrameworkCore;
using payments_api.Models;

namespace Payments.API.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<Card> Cards { get; set; }

  
}

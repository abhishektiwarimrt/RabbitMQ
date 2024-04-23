using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRabbit.Banking.Domain.Models
{
    public class Account
    {
        public int Id { get; set; } 
        public string AccountType { get; set; }
        [Column(TypeName = "decimal(18,5)")]
        public decimal AccountBalance { get; set; }
    }
}

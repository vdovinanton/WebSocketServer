using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TryFleck.Models
{
    public class Customer
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Id { get; set; }
        public decimal Price { get; set; }
    }
}
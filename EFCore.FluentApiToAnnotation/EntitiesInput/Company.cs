using System;
using System.Collections.Generic;

namespace CoreTemplate.Entities
{
    public partial class Company
    {
        public Company()
        {
            Item = new HashSet<Item>();
        }

        public Guid CompanyId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Item> Item { get; set; }
    }
}

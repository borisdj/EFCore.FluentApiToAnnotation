using System;
using System.Collections.Generic;

namespace CoreTemplate.Entities
{
    public partial class Item
    {
        public Guid ItemId { get; set; }
        public int CategoryId { get; set; }
        public Guid? CompanyId { get; set; }
        public string Name { get; set; }
        public DateTime? Timer { get; set; }
        public decimal? Value { get; set; }

        public virtual Company Company { get; set; }
    }
}

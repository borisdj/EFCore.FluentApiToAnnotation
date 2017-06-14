using System;
using System.Collections.Generic;

namespace CoreTemplate.Entities
{
    public partial class Item
    {
        public Guid ItemId { get; set; }
        public Guid CompanyId { get; set; }
        public string Description { get; set; }
        public int GroupId { get; set; }
        public decimal Price { get; set; }
        public decimal? PriceExtended { get; set; }
        public DateTime TimeCreated { get; set; }
        public DateTime? TimeExpire { get; set; }

        public virtual Company Company { get; set; }
        public virtual Group Group { get; set; }
    }
}

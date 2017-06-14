using System;
using System.Collections.Generic;

namespace CoreTemplate.Entities
{
    public partial class Group
    {
        public Group()
        {
            Item = new HashSet<Item>();
        }

        public int GroupId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Item> Item { get; set; }
    }
}

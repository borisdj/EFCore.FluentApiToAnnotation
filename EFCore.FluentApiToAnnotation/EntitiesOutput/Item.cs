using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EfCore.Shaman;

namespace CoreTemplate.Entities2
{
    [Table(nameof(Item), Schema = "fin")]
    public class Item
    {
        public Guid ItemId { get; set; }

        public Guid CompanyId { get; set; }

        [UniqueIndex]
        [Required]
        [MaxLength(255)]
        public string Description { get; set; }

        [ForeignKey("Group"/*, DeleteBehavior.Restrict*/)]
        public int GroupId { get; set; }

        public decimal Price { get; set; }

        [DecimalType(20,4)]
        public decimal? PriceExtended { get; set; }

        public DateTime TimeCreated { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? TimeExpire { get; set; }

        public virtual Company Company { get; set; }

        public virtual Group Group { get; set; }
    }
}

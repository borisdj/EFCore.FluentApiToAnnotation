using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EfCore.Shaman;

namespace CoreTemplate.Entities2
{
    public class Group
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int GroupId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}

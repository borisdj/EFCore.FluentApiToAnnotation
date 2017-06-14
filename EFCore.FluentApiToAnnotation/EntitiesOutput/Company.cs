using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EfCore.Shaman;

namespace CoreTemplate.Entities
{
    public class Company
    {
        public Guid CompanyId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}

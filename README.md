# EFCore.FluentApiToAnnotation
Console app in NetCore for converting FluentApi configuration to Annotations (Attributes extended with [EfCore.Shaman](https://github.com/isukces/EfCore.Shaman) lib)  
Latest version 1.0.3 

When using EntityFrameworkCore Code First approach specific config can be defined with [FluentApi](https://msdn.microsoft.com/en-us/library/jj591620(v=vs.113).aspx) or with [Annotations](https://msdn.microsoft.com/en-us/library/jj591583(v=vs.113).aspx).<br>
I prefere Annotations because it requires less code, it's all in one place and configs are directly on Property they refer, similar like in database itself. Also nice thing here is that there is a lot of convention so we often get desired model without having to configure everything explicitly, like when PK is named *Id* or *TableId* we don't need `[Key]` attribute.<br>
Only problem with Annotations was that EFCore does not have Attributes for everything, but with the help of EfCore.Shaman library that problem is solved.<br>
This works well when creating new App, but sometimes we are migrating existing App to new Framework.<br>
In that situation EFCore have built-in reverse engineering functionality:<br>
`Scaffold-DbContext "Server=localhost;Database=DbName;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Entities`<br>
Now this creates pure POCO classes and Context file that has all configuration in FluentApi.<br>

If we still want to have it in Annotations we would need to retype it and add appropriate Attributes to Entity classes.<br>
Since database could be pretty large regarding number of tables this would be a of lot boring work.<br>
So this application actuality automates that conversion.<br>
It reads all files of Entity classes creating its models, parses FluentApi configs from Context, than adds apropriate Attributes to model, and writes again new files. Class models and writing them is implemented with [CsCodeGenerator](https://github.com/borisdj/CsCodeGenerator) library.<br>
Additionally ICollections are omitted and DbSets changed to plural: `DbSet<Company> Company` -> `DbSet<Company> Companies`.<br>
Here in repository there is exe.zip file which contains built app and 2 folders: `EntitiesInput` where we should put input files and the app will generate new files in `EntitiesOutput` folder.

REMARK:
Currently there is no Attribute for custom  [*DeleteBehaviour(Rule)*](https://github.com/isukces/EfCore.Shaman/issues/7) options.
When having FK with DeleteBehaviour that is not default, it has to be configured in FluentApi explicitly. This will be updated when that feature gets implemented.

[![License](https://img.shields.io/npm/l/express.svg)](https://github.com/borisdj/EFCore.FluentApiToAnnotation/blob/master/LICENSE)  
Also take a look into others packages:</br>
-Open source (MIT or cFOSS) authored [.Net libraries](https://infopedia.io/dot-net-libraries/) (@**Infopedia.io** personal blog post)
| №  | .Net library             | Description                                              |
| -  | ------------------------ | -------------------------------------------------------- |
| 1  | [EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions) | EF Core Bulk CRUD Ops (**Flagship** Lib) |
| 2  | [EFCore.UtilExtensions](https://github.com/borisdj/EFCore.UtilExtensions) | EF Core Custom Annotations and AuditInfo |
| 3* | [EFCore.FluentApiToAnnotation](https://github.com/borisdj/EFCore.FluentApiToAnnotation) | Converting FluentApi configuration to Annotations |
| 4  | [FixedWidthParserWriter](https://github.com/borisdj/FixedWidthParserWriter) | Reading & Writing fixed-width/flat data files |
| 5  | [CsCodeGenerator](https://github.com/borisdj/CsCodeGenerator) | C# code generation based on Classes and elements |
| 6  | [CsCodeExample](https://github.com/borisdj/CsCodeExample) | Examples of C# code in form of a simple tutorial |

## Support
If you find this project useful you can mark it by leaving a Github **Star** :star:  
And even with community license, if you want help development, you can make a DONATION:  
[<img src="https://www.buymeacoffee.com/assets/img/custom_images/yellow_img.png" alt="Buy Me A Coffee" height=28>](https://www.buymeacoffee.com/boris.dj) _ or _ 
[![Button](https://img.shields.io/badge/donate-Bitcoin-orange.svg?logo=bitcoin):zap:](https://borisdj.net/donation/donate-btc.html)

## Contributing
Please read [CONTRIBUTING](CONTRIBUTING.md) for details on code of conduct, and the process for submitting pull requests.  
When opening issues do write detailed explanation of the problem or feature with reproducible example.  
Want to **Contact** for Development & Consulting: [www.codis.tech](http://www.codis.tech) (*Quality Delivery*) 

## Usage
EXAMPLE<br>
DB tables: **dbo.Company**, **dbo.Group**, **fin.Item**

| Column Name  | Data Type          | AllowNulls | Configuration            |
| ------------ | ------------------ | ---------- | ------------------------ |
| CompanyId    | int                | False      | PK (Identity: False)     |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| GroupId      | uniqueidentifier   | False      | PK (Identity: False)     |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| ItemId       | uniqueidentifier   | False      | PK (Identity: False)     |
| CompanyId    | uniqueidentifier   | False      | FKTable: Company(Cascade)|
| Description  | nvarchar(255)      | False      | UniqueIndex              |
| GroupId      | int                | False      | FKTable: Group(Restrict) |
| Price        | decimal(18, 2)     | False      |                          |
| PriceExtended| decimal(20, 4)     | True       |                          |
| TimeCreated  | datetime2(7)       | False      |                          |
| TimeExpire   | datetime           | True       |                          |

Scaffolded:
```csharp
public partial class AppContext : DbContext
{
    public virtual DbSet<Company> Company { get; set; }
    public virtual DbSet<Group> Group { get; set; }
    public virtual DbSet<Item> Item { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.CompanyId).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
        });
        
        modelBuilder.Entity<Group>(entity =>
        {
            entity.Property(e => e.GroupId).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
        });
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Item", "fin");
            
            entity.HasIndex(e => e.CompanyId).HasName("IX_Item_CompanyId");
            entity.HasIndex(e => e.Description).HasName("IX_Item_Description").IsUnique();
            entity.HasIndex(e => e.GroupId).HasName("IX_Item_GroupId");

            entity.Property(e => e.ItemId).ValueGeneratedNever();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal");
            entity.Property(e => e.PriceExtended).HasColumnType("decimal(20,4)");
            entity.Property(e => e.TimeExpire).HasColumnType("datetime");

            entity.HasOne(d => d.Company).WithMany(p => p.Item).HasForeignKey(d => d.CompanyId);
            entity.HasOne(d => d.Group).WithMany(p => p.Item).HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    ...
}

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
```

Converted:
```csharp
public partial class AppContext : DbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Item> Items { get; set; }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        this.FixOnModelCreating(modelBuilder);
        modelBuilder.Entity<Item>().HasOne(p => p.Group).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}

public class Company
{
    public Guid CompanyId { get; set; }

    [Required]
    public string Name { get; set; }
}

public class Group
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int GroupId { get; set; }

    [Required]
    public string Name { get; set; }
}

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

```

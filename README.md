# EFCore.FluentApiToAnnotation
Console app in NetCore for converting FluentApi configuration to Annotations(Attributes extended with [EfCore.Shaman](https://github.com/isukces/EfCore.Shaman) lib)

When using EntityFrameworkCore Code First approach specific config can be defined with [FluentApi](https://msdn.microsoft.com/en-us/library/jj591620(v=vs.113).aspx) or with [Annotations](https://msdn.microsoft.com/en-us/library/jj591583(v=vs.113).aspx).<br>
I prefere Annotations because it requires less code, it's all in one place and configs are directly on Property they refer, similar like in database itself.<br>
Only problem with Annotations was that EFCore does not have Attributes for everything, but with the help of EfCore.Shaman library that problem is solved.<br>
This works well when creating new App, but sometimes we are migrating existing App to new Framework.<br>
In that situation EFCore have built-in reverse engineering functionality:<br>
`Scaffold-DbContext "Server=localhost;Database=DbName;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Entities`<br>
Now this creates pure POCO classes and Context file that has all specifics in FluentApi.<br>

If we still want to have it in Annotations we would need to retype it and add appropriate Attributes to Entity classes.<br>
Since database could be pretty large regarding number of tables this would be a of lot boring work.<br>
So this application actuality automates that conversion.<br>
It reads all files of Entity classes creating its models, parses FluentApi configs from Context, than adds apropriate Attributes to model, and writes again new files. Class models and writing them is implemented with [CsCodeGenerator](https://github.com/borisdj/CsCodeGenerator) library.<br>
Here in repository there is exe.zip file which contains built app and 2 folders: `EntitiesInput` where we should put input files and the app will generate new files in `EntitiesOutput` folder.

REMARK:
Currently there is no Attribute for custom  [*DeleteBehaviour(Rule)*](https://github.com/isukces/EfCore.Shaman/issues/7) options.
When having FK with DeleteBehaviour that is not default, it has to be configured in FluentApi explicitly.

EXAMPLE<br>
DB tables: **dbo.Company**, **dbo.Group**, **fin.Item**

| Column Name  | Data Type          | AllowNulls | Specifics                |
| ------------ | ------------------ | ---------- | ------------------------ |
| CompanyId    | int                | False      | PK (Identity: False)      |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| GroupId      | uniqueidentifier   | False      | PK (Identity: False)      |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| ItemId       | uniqueidentifier   | False      | PK (Identity: False)      |
| CompanyId    | uniqueidentifier   | False      | FKTable: Company (Cascade)|
| Description  | nvarchar(255)      | False      | UniqueIndex              |
| GroupId      | int                | False      | FKTable: Group (Restrict)|
| Price        | decimal(18, 2)     | False      |                          |
| PriceExtended| decimal(20, 4)     | True       |                          |
| TimeCreated  | datetime2(7)       | False      |                          |
| TimeExpire   | datetime           | True       |                          |

Scaffolded:
```csharp
public partial class CoreTemplateContext : DbContext
{
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
            entity.HasOne(d => d.Group).WithMany(p => p.Item).HasForeignKey(d => d.GroupId).OnDelete(DeleteBehavior.Restrict);
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
public partial class CoreTemplateContext : DbContext
{
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

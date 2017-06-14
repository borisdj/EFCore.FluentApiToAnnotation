# EFCore.FluentApiToAnnotation
Console application for converting FluentApi configuration to Annotations (Attributes extended with [EfCore.Shaman](https://github.com/isukces/EfCore.Shaman) library).

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
DB tables: **Group**, **Company**, **Item**

| Column Name  | Data Type          | AllowNulls | Specifics                |
| ------------ | ------------------ | ---------- | ------------------------ |
| GroupId      | int                | False      | PK (Identity:False)      |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| CompanyId    | uniqueidentifier   | False      | PK (Identity:False)      |
| Name         | nvarchar(MAX)      | False      |                          |
|--------------|--------------------|------------| ------------------------ |
| ItemId       | uniqueidentifier   | False      | PK (Identity:False)      |
| CompanyId    | uniqueidentifier   | False      | FKTable: Company(Cascade)|
| Description  | nvarchar(255)      | False      | UniqueIndex              |
| GroupId      | int                | False      | FKTable: Group (Restrict)|
| Price        | decimal(18, 2)     | False      |                          |
| PriceExtended| decimal(20, 4)     | True       |                          |
| TimeCreated  | datetime2(7)       | False      |                          |
| TimeExpire   | datetime           | True       |                          |

Scaffolded:

Converted:

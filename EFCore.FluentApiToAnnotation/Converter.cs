using System;
using System.Collections.Generic;
using System.IO;
using CsCodeGenerator;
using CsCodeGenerator.Enums;
using EFCore.FluentApiToAnnotation.Extensions;
using Humanizer;

namespace EFCore.FluentApiToAnnotation
{
    public class Converter
    {
        private string entityEndText = ">(entity =>";
        private string entityToTableText = "entity.ToTable(";
        private string entityHasKeyText = "entity.HasKey(e => ";
        private string entityHasIndexText = "entity.HasIndex(e => ";
        private string entityPropertyText = "entity.Property(e => e.";
        private string entityHasOneText = "entity.HasOne(d => d.";
        private string keyText = "Key";
        private string indexText = "Index";

        private string PublicText => AccessModifier.Public.ToTextLower();
        private string PublicPartialClassText => $"{PublicText} {KeyWord.Partial.ToTextLower()} {Util.Class} ";
        private string PublicVirtualText => $"{PublicText} {KeyWord.Virtual.ToTextLower()} ";
        private string GetSetText  => " { get; set; }";
        
        private Dictionary<string, string> ApiToAnnotationDict = new Dictionary<string, string>();

        private List<string> FluentApiLines = new List<string>();

        public string InputFolder { get; set; } = "EntitiesInput";
        public string OutputFolder { get; set; } = "EntitiesOutput";
        public string DbContextText { get; set; } = "DbContext";
        public string SaveChangesText { get; set; } = "SaveChanges";

        public string ColonDbContext => $" : {DbContextText}";

        public bool DoWriteCollections { get; set; }
        public bool DoWriteSaveChangesMethod { get; set; }

        public CsGenerator CsGenerator { get; set; } = new CsGenerator();

        public List<string[]> Files { get; set; } = new List<string[]>();

        public void LoadFiles()
        {
            CsGenerator.OutputDirectory = this.OutputFolder;
            LoadPropertyAnnotationDict();

            string path = $@"{CsGenerator.DefaultPath}\{InputFolder}";
            foreach (var filePath in Directory.EnumerateFiles(path))
            {
                List<string> codeLines = new List<string>();

                string[] fileLines = System.IO.File.ReadAllLines(filePath);
                int codeLineCounter = 0;
                for (int i = 0; i < fileLines.Length; i++)
                {
                    string codeLine = fileLines[i].Trim();

                    if (String.IsNullOrWhiteSpace(codeLine) && fileLines[i + 1].Trim().StartsWith("entity."))
                    {
                        continue; // skip empty line between "entity."
                    }
                    else if (codeLine.StartsWith("."))
                    {
                        codeLines[codeLineCounter - 1] += codeLine;
                    }
                    else
                    {
                        codeLines.Add(codeLine);
                        codeLineCounter++;
                    }
                }
                Files.Add(codeLines.ToArray());
            }
        }

        public void Run()
        {
            bool dbContextIsLoaded = false;
            string[] dbContextFileLines = null;

            foreach (var fileLines in Files)
            {
                if (!IsDbContent(fileLines))
                {
                    LoadEntityFile(fileLines);
                }
                else if (!dbContextIsLoaded) // since usually there will be only one DbContext file, bool dbContextIsLoaded can be used to stop search when first is found
                {
                    dbContextFileLines = fileLines;
                    dbContextIsLoaded = true;
                }
            }

            LoadDbContent(dbContextFileLines);
        }

        protected bool IsDbContent(string[] fileLines)
        {
            foreach (var line in fileLines)
            {
                if (line.EndsWith(ColonDbContext))
                    return true;
            }
            return false;
        }

        protected void LoadEntityFile(string[] entityFileLines)
        {
            FileModel fileModel = new FileModel();
            ClassModel classModel = null;
            fileModel.LoadUsingDirectives(Utility.GetModelUsingDirectives());

            foreach (var line in entityFileLines)
            {
                if (LineIsIgnored(line, fileModel.Name)) // To be skipped
                {
                    continue;
                }
                else if (line.StartsWith(Util.Namespace)) // Namespace
                {
                    fileModel.Namespace = line;
                }
                else if (line.StartsWith(PublicPartialClassText)) // Class declaration
                {
                    fileModel.Name = line.Replace(PublicPartialClassText, ""); //.Split(new string[] { " class " }, StringSplitOptions.None)[1];
                    classModel = new ClassModel(fileModel.Name);
                }
                else if (DoWriteCollections && line.Contains(" = new HashSet<")) // Constructor body
                {
                    string hashSetName = line.Split(" ")[0];
                    string hashSetPluralLine = line.Replace(hashSetName + " ", hashSetName.Pluralize() + " ");
                    classModel.DefaultConstructor.BodyLines.Add(hashSetPluralLine); // HashSets initialisations
                    classModel.DefaultConstructor.IsVisible = true;
                }
                else if (line.EndsWith(GetSetText)) // { get; set; }
                {
                    string[] typeAndName = line.Replace(PublicText, "").Replace(GetSetText, "").Trim().Split(' ');
                    Property property = null;
                    bool isICollection = typeAndName[1].StartsWith("ICollection");

                    if (typeAndName[0] != KeyWord.Virtual.ToTextLower()) // Property (... type Column)
                    {
                        property = new Property(typeAndName[0], typeAndName[1]);
                    }
                    else // Navigation Property (... virtual ICollection<Table> Table)
                    {
                        var propertyName = isICollection ? typeAndName[2].Pluralize() : typeAndName[2];
                        property = new Property(typeAndName[1], propertyName);
                        property.KeyWords.Add(KeyWord.Virtual);
                    }
                    if(!isICollection || DoWriteCollections)
                    {
                        classModel.Properties.Add(property.Name, property);
                    }
                }
            }

            fileModel.Classes.Add(classModel.Name, classModel);
            CsGenerator.Files.Add(fileModel.Name, fileModel);
        }

        protected bool LineIsIgnored(string line, string fileName)
        {
            bool isIgnored = (line == "{" || line == "}"
                || line.StartsWith(Util.Using) || String.IsNullOrEmpty(line)
                || line.EndsWith($"{AccessModifier.Public} {fileName}()")  // Constructor declaration
            );
            return isIgnored;
        }

        protected void LoadDbContent(string[] dbContextFileLines)
        {
            string modelBuilderEntityText = "modelBuilder.Entity<";
            string onConfiguringText = "OnConfiguring";
            string onModelCreatingText = "OnModelCreating";
            
            FileModel fileModel = new FileModel();
            ClassModel classModel = new ClassModel();

            fileModel.LoadUsingDirectives(Utility.GetContextUsingDirectives());

            int dbContextFileLinesLength = dbContextFileLines.Length;
            for (int i = 0; i < dbContextFileLinesLength; i++)
            {
                string line = dbContextFileLines[i];

                if (LineIsIgnored(line, fileModel.Name)) // To be skipped
                {
                    continue;
                }
                else if (line.StartsWith(Util.Namespace)) // Namespace
                {
                    fileModel.Namespace = line;
                }
                else if (line.StartsWith(PublicPartialClassText)) // Class declaration
                {
                    fileModel.Name = line.Replace(PublicPartialClassText, "").Replace(ColonDbContext, ""); //.Split(new string[] { " class " }, StringSplitOptions.None)[1];
                    classModel = new ClassModel(fileModel.Name);
                    classModel.KeyWords.Add(KeyWord.Partial);
                    classModel.BaseClass = DbContextText;
                    classModel.HasPropertiesSpacing = false;
                }
                else if (line.Contains("DbSet<")) // DbSets
                {
                    string[] typeAndName = line.Replace(PublicVirtualText, "").Replace(GetSetText, "").Split(' ');
                    string dbSetName = typeAndName[1].Pluralize();
                    classModel.Properties.Add(dbSetName, new Property(typeAndName[0], dbSetName));
                }
                else if (line.Contains(onConfiguringText)) // OnConfiguring -> Constructor
                {
                    // ConnectionString is in config and will be injected with DI
                    var constructor = new Constructor(classModel.Name)
                    {
                        IsVisible = true,
                        BaseParameters = "options",
                        BracesInNewLine = false
                    };
                    constructor.Parameters.Add(new Parameter(customDataType: "DbContextOptions", name: "options"));
                    classModel.Constructors.Add(constructor);
                }
                else if (line.Contains(onModelCreatingText)) // OnModelCreating
                {
                    var method = new Method(AccessModifier.Protected, KeyWord.Override, BuiltInDataType.Void, onModelCreatingText);
                    method.BodyLines.Add("base.OnModelCreating(modelBuilder);");
                    method.BodyLines.Add("this.FixOnModelCreating(modelBuilder);");
                    method.BodyLines.Add("");

                    method.Parameters.Add(new Parameter(customDataType: "ModelBuilder", name: "modelBuilder"));
                    classModel.Methods.Add(onModelCreatingText, method);
                }
                // FluentAPI
                else if (line.StartsWith(modelBuilderEntityText)) // modelBuilder.Entity<"||EntityName||>(entity =>
                {
                    string entityName = line.Replace(modelBuilderEntityText, "").Replace(entityEndText, "");
                    i++;
                    ClassModel entityClass = CsGenerator.Files[entityName].Classes[entityName];
                    while (!dbContextFileLines[i].StartsWith(modelBuilderEntityText))
                    {
                        line = dbContextFileLines[i];
                        if (line.StartsWith(entityToTableText))
                        {
                            ParseToTable(line, entityClass);
                        }
                        else if (line.StartsWith(entityHasKeyText) || line.StartsWith(entityHasIndexText))
                        {
                            ParseKeyOrIndex(line, entityClass);
                        }
                        else if (line.StartsWith(entityPropertyText))
                        {
                            ParseProperty(line, entityClass);
                        }
                        else if (line.StartsWith(entityHasOneText))
                        {
                            ParseHasOne(line, entityClass);
                        }

                        i++;
                        if (i == dbContextFileLinesLength)
                            break;
                    }
                    i--;
                }
            }

            foreach (var line in FluentApiLines)
            {
                classModel.Methods[onModelCreatingText].BodyLines.AddRange(FluentApiLines);
            }

            if (DoWriteSaveChangesMethod)
            {
                var commit = new Method(AccessModifier.Protected, KeyWord.Override, BuiltInDataType.Int, SaveChangesText);
                commit.BodyLines.Add("base.SaveChanges();");
                classModel.Methods.Add(SaveChangesText, commit);
            }

            fileModel.Classes.Add(classModel.Name, classModel);
            CsGenerator.Files.Add(fileModel.Name, fileModel);
        }

        private void ParseToTable(string line, ClassModel entityClass)
        {
            //FORMAT: entity.ToTable(||"TableName"||, ||"SchemaName"||);
            string[] tableAndSchema = line.Remove(new string[] { entityToTableText, ");" }).Split(", ");
            var tableAttribute = new AttributeModel("Table");
            string tableNameQuoted = tableAndSchema[0];
            string tableName = tableNameQuoted.Remove("\"");
            string tableAttributeName = tableName == entityClass.Name ? $"nameof({tableName})" : tableNameQuoted; // if same name as entity use nameof(Entity)

            tableAttribute.Parameters.Add(new Parameter(tableAttributeName));
            tableAttribute.Parameters.Add(new Parameter("Schema =", tableAndSchema[1])); // schemaName
            entityClass.Attributes.Add(tableAttribute.Name, tableAttribute);
        }

        private void ParseKeyOrIndex(string line, ClassModel entityClass)
        {
            // FORMAT_K1: entity.HasKey(e => ||e.TableId||).||HasName("PK_Table");
            // FORMAT_K2: entity.HasKey(e => ||new { e.TableId, e.SecondId }||).||HasName("PK_Table");

            // FORMAT_I1: entity.HasIndex(e => ||e.Column||).||HasName("IX_Table_Column"||).||IsUnique();
            // FORMAT_I2: entity.HasIndex(e => ||new { e.Column1, e.Column2 }||).||HasName("IX_Table_Column1_Column2");

            bool isKey = line.StartsWith(entityHasKeyText);
            string entityStartText = isKey ? entityHasKeyText : entityHasIndexText;
            string attributeName = isKey ? keyText : indexText;

            string[] entityConfigs = line.Remove(new string[] { entityStartText, ");", "(" }).Split(").");
            string[] attributeProperties = entityConfigs[0].Remove(new string[] { "new { ", "e.", " }" }).Split(", ");

            string keyOrIndexName = entityConfigs[1].Remove("HasName");
            string keyOrIndexNamePrefix = isKey ? "PK" : "IX";
            string keyOrIndexNameSufix = isKey ? "" : "_" + String.Join("_", attributeProperties);
            string keyOrIndexDefaultName = $@"""{keyOrIndexNamePrefix}_{entityClass.Name}{keyOrIndexNameSufix}""";

            bool isUnique = !isKey && entityConfigs.Length > 2 && entityConfigs[2] == "IsUnique";
            bool isMultiColumn = attributeProperties.Length > 1;

            foreach (var attributeProperty in attributeProperties)
            {
                var attribute = new AttributeModel((isUnique ? "Unique" : "") + attributeName);
                if (keyOrIndexName != keyOrIndexDefaultName || isMultiColumn)
                {
                    attribute.Parameters.Add(new Parameter(value: keyOrIndexName));
                }
                entityClass.Properties[attributeProperty].Attributes.Add(attribute.Name, attribute);
            }
        }

        private void ParseProperty(string line, ClassModel entityClass)
        {
            // FORMAT_1: entity.Property(e => e.||PropertyName||).||HasColumnName("Column"||);
            // FORMAT_2: entity.Property(e => e.||PropertyName||).||IsRequired(||).||HasMaxLength(100||);
            // FORMAT_3: entity.Property(e => e.||PropertyName||).||HasDefaultValueSql("getdate()"||);
            // ...
            string[] entityConfigs = line.Remove(new string[] { entityPropertyText, ");" }).Split(").");
            string attributeProperty = entityConfigs[0];

            for (int k = 1; k < entityConfigs.Length; k++)
            {
                bool isDefaultConfig = entityConfigs[k] == @"HasColumnType(""decimal"""
                                    || entityConfigs[k] == @"HasColumnType(""decimal(18,2)""" // EF SCAFFOLD bug - no precision
                                    || entityConfigs[k] == @"HasColumnType(""decimal(18, 2)""" // ? (18,2) : (18, 2)
                                    || entityConfigs[k] == "ValueGeneratedNever(" && entityClass.Properties[attributeProperty].CustomDataType?.ToString() == "Guid"
                                    || entityConfigs[k] == "ValueGeneratedOnAdd(" && entityClass.Properties[attributeProperty].BuiltInDataType?.ToString() == "int";
                // isDefaultConfig means that explicit Attribute is not required since Property names already defines based on EF naming convention
                if (!isDefaultConfig)
                {
                    string[] configTypeAndParam = entityConfigs[k].Split("(", 2);
                    var attribute = new AttributeModel(ApiToAnnotationDict[configTypeAndParam[0]]);
                    string parameterValue = configTypeAndParam[1];
                    if (!String.IsNullOrWhiteSpace(parameterValue))
                    {
                        attribute.Parameters.Add(new Parameter(parameterValue));
                    }
                    entityClass.Properties[attributeProperty].AddAttribute(attribute);
                }
            }
        }
        private void ParseHasOne(string line, ClassModel entityClass)
        {
            // FORMAT: entity.HasOne(d => d.||Genre||).||WithMany(p => p.Movie||).||HasForeignKey(d => d.GenreId); // ).||OnDelete(DeleteBehavior.Restrict);

            string withManyText = "WithMany(p => p.";
            string hasForeignKeyText = "HasForeignKey(d => d.";
            string onDeleteDeleteBehavior = "OnDelete(DeleteBehavior.";
            string hasConstraintName = "HasConstraintName(";

            string[] entityConfigs = line.Remove(new string[] { entityHasOneText, ");" }).Split(").");
            string foreignTable = entityConfigs[0];
            string foreignTableId = foreignTable + "Id";
            string primaryTable = entityConfigs[1].Remove(withManyText);
            string foreignKey = entityConfigs[2].Remove(hasForeignKeyText);
            string deleteBehavior = (entityConfigs.Length > 3  && entityConfigs[3].Contains(onDeleteDeleteBehavior)) ? entityConfigs[3].Remove(onDeleteDeleteBehavior) : null;

            int position = 0;
            if (deleteBehavior == null && entityConfigs.Length > 3)
                position = 3;
            else if (entityConfigs.Length > 4)
                position = 4;
            string constraintName = (entityConfigs[position].Contains(hasConstraintName)) ? entityConfigs[position].Remove(hasConstraintName) : null;

            // removes [Index] attributes since in annotation FK has it by default.
            var property = entityClass.Properties[foreignKey];
            property.Attributes.TryRemove("Index");
            // TODO: consider situation when no Index, so [ForeignKey] attribute should have explicit parameter 'HasIndex = false'

            // not required when FK relationship comes from default convention 'virtual RelationTable' and 'virtual ICollection<RelationTable>'
            if (!foreignTableId.EndsWith(foreignKey) || deleteBehavior != null || constraintName != null)
            {
                var attribute = new AttributeModel("ForeignKey");
                string aditionalParameters = "";
                if (deleteBehavior != null)
                {
                    aditionalParameters = $"/*, DeleteBehavior.{deleteBehavior}";
                }
                if (constraintName != null)
                {
                    aditionalParameters += $", ConstraintName = {constraintName}";
                }
                if(String.IsNullOrEmpty(aditionalParameters))
                    aditionalParameters += "*/";

                attribute.Parameters.Add(new Parameter(value: $@"""{foreignTable}""{aditionalParameters}"));
                entityClass.Properties[foreignKey].Attributes.Add(attribute.Name, attribute);

                // Have to keep FluentApi when DeleteBehavior not default (for example FK notNull but with DeleteBehavior.Restrict)
                string fluentApiLine = $"modelBuilder.Entity<{primaryTable}>().HasOne(p => p.{foreignTable}).WithMany()";
                if (deleteBehavior != null)
                    fluentApiLine += $".OnDelete(DeleteBehavior.{deleteBehavior})";
                if (constraintName != null)
                    fluentApiLine += $".HasConstraintName({constraintName})";
                fluentApiLine += ";";
                FluentApiLines.Add(fluentApiLine);
            }
        }

        private void LoadPropertyAnnotationDict()
        {
            ApiToAnnotationDict.Add("ValueGeneratedNever", "DatabaseGenerated(DatabaseGeneratedOption.None)"); // EF SCAFFOLD bug - ignores Delete Rule of FK
            ApiToAnnotationDict.Add("ValueGeneratedOnAdd", "DatabaseGenerated(DatabaseGeneratedOption.Identity)");
            ApiToAnnotationDict.Add("ValueGeneratedOnAddOrUpdate", "DatabaseGenerated(DatabaseGeneratedOption.Computed)");
            ApiToAnnotationDict.Add("HasColumnName", "Column");
            ApiToAnnotationDict.Add("IsRequired", "Required");
            ApiToAnnotationDict.Add("HasMaxLength", "MaxLength");
            ApiToAnnotationDict.Add("HasDefaultValue", "DefaultValue");
            ApiToAnnotationDict.Add("HasDefaultValueSql", "DefaultValueSql");
            ApiToAnnotationDict.Add("HasColumnType", "DecimalType"); // curently has only DecimalType custom precision support

            // Attribute is not required when DEFAULT
            /*
            ValueGeneratedNever();          [DatabaseGenerated(DatabaseGeneratedOption.None)] // DEFAULT if on Guid
            ValueGeneratedOnAdd();          [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // DEFAULT if on int
            ValueGeneratedOnAddOrUpdate();  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
            HasColumnName("SomeColumn");    [Column("SomeColumn")]
            IsRequired();                   [Required]
            HasMaxLength(100);              [MaxLength(100)]
            HasDefaultValue(true);          [DefaultValue(true)]
            HasDefaultValueSql("getdate()");[DefaultValueSql("getdate()")]
            HasColumnType("decimal(20,4)"); [DecimalType(20, 4)] // DEFAULT if decimal or decimal(18, 2)
            */
        }
    }
}
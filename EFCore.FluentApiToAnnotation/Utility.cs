using System.Collections.Generic;

namespace EFCore.FluentApiToAnnotation
{
    public static class Utility
    {
        public static List<string> GetContextUsingDirectives()
        {
            var usingDirectives = new List<string>
            {
                "using EfCore.Shaman;",
                "using Microsoft.EntityFrameworkCore;",
                "using Microsoft.EntityFrameworkCore.Metadata;"
        };
            return usingDirectives;
        }

        public static List<string> GetModelUsingDirectives()
        {
            var usingDirectives = new List<string>
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.ComponentModel;",
                "using System.ComponentModel.DataAnnotations;",
                "using System.ComponentModel.DataAnnotations.Schema;",
                "using EfCore.Shaman;"

                // Following is alternative way, more dynamic string concatenation but not required in this simple case - it would be overkill
                /*var usingString = "using";
                var system = "System";
                var collections = "Collections";
                var generic = "Generic";
                var componentModel = "ComponentModel";
                var dataAnnotations = "DataAnnotations";
                var schema = "Schema";
                var efCoreShaman = "EfCore.Shaman;"

                var usingSystem = $"{usingString} {system}";
                var usingSystCollGeneric = $"{usingSystem}.{collections}.{generic}";
                var usingSystComponentModel = $"{usingSystem}{componentModel}";
                var usingCompModDataAnnotations = $"{usingSystComponentModel}.{dataAnnotations}";
                var usingCompModDataAnnSchema = $"{usingSystComponentModel}.{schema}";
                var usingEfCoreShaman = $"{usingString} {efCoreShaman}"; */
            };
            return usingDirectives;
        }
    }
}

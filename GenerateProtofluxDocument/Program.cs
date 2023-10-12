using Microsoft.Collections.Extensions;
using Mono.Cecil;

namespace GenerateProtoFluxDocument;

internal static class Program
{
    private static void Main(string[] args)
    {
        var baseDir = args[0];
        var resolver = new DefaultAssemblyResolver();
        // 
        resolver.AddSearchDirectory(baseDir);

        var loadedAssembly = Directory.EnumerateFiles(baseDir)
            .SelectMany(x =>
            {
                try
                {
                    return new List<AssemblyDefinition> {
                        AssemblyDefinition.ReadAssembly(x, new ReaderParameters(ReadingMode.Deferred)
                        {
                            AssemblyResolver = resolver
                        })
                    };
                }
                catch (BadImageFormatException _)
                {
                    Console.Error.WriteLine($"Cecil: {x} does not contain CLR metadata");
                    // Console.WriteLine(e);
                    return new List<AssemblyDefinition>();
                }
            })
            .Select(x => x.MainModule)
            .ToList();

        // TODO: do we need this? 
        // TODO: this should be other collection than List. Maybe BTreeSet or something similar
        var typeUniverse = new List<TypeDefinition>();
        foreach (var moduleDefinition in loadedAssembly)
        {
            Console.WriteLine($"read assembly: {moduleDefinition}");
            typeUniverse.AddRange(moduleDefinition.Types);
        }

        var nodeNameAttribute = typeUniverse.Find(x => x.FullName == "ProtoFlux.Core.NodeNameAttribute") 
                                ?? throw new MissingMemberException("[NodeName] is ProtoFlux.Core could not be found. Maybe disappeared?");
        Console.WriteLine("[NodeName]: found");
        var categoryAttribute = typeUniverse.Find(x => x.FullName == "ProtoFlux.Core.NodeCategoryAttribute") 
                                ?? throw new MissingMemberException("[Category] in FrooxEngine could not be found. Maybe disappeared?");
        Console.WriteLine("[Category]: found");
        
        var nodeCategory = new MultiValueDictionary<NestedCategoryName, TypeReference>();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        // TODO: not yet
        foreach (var td in typeUniverse)
        {
            if (td.FullName == "<Module>")
            {
                // this is pseudo type, implies top-level module. skipping.
                continue;
            }

            if (!td.IsClass) continue;
            if (!td.Namespace.StartsWith("ProtoFlux.")) continue;
            
            Console.WriteLine($"{td.NameWithTypeVar()}:");
                    
            var ca = td.CustomAttributes;
            // plain == compares addresses, not their contents.
            var nodeNameAttr = ca.FirstOrDefault(x => x.AttributeType.FullName == nodeNameAttribute.FullName);

            if (nodeNameAttr != null)
            {
                Console.WriteLine("    is a Node");
                Console.Write("        inputs: ");
                var inputs = td.Fields.Where(x => x.IsExposedToProtoFluxUsers()).ToList();
                        
                if (inputs.Count == 0)
                {
                    Console.WriteLine("<none>");
                }
                else
                {
                    Console.WriteLine();
                    foreach (var input in inputs)
                    {
                        var fieldTy = input.FieldType;
                        // Console.WriteLine($"    field: {input.Name}: {fieldTy.FullName}");
                        // assumption: input must have exactly single TypeVar.
                        // Console.WriteLine($"          decl: {fieldTy.HasGenericParameters}");
                        if (!fieldTy.IsGenericInstance)
                        {
                            throw new MissingMemberException(
                                $"{fieldTy} does not have generic argument.");
                        }

                        // this cast is important, don't try to remove it
                        var ty = (GenericInstanceType) fieldTy;
                        var name = input.Name;
                                
                        Console.WriteLine($"        - {name}: {ty.GenericArguments.ToList()[0]}");
                    }
                }
                        
            }

            var categoryAttr = ca.FirstOrDefault(x => x.AttributeType.FullName == categoryAttribute.FullName);

            if (categoryAttr != null)
            {
                var categoryName = (string) categoryAttr.ConstructorArguments[0].Value;
                
                nodeCategory.Add(new NestedCategoryName(categoryName), td);
                Console.WriteLine("    set Category");
            }
                    
            foreach (var attr in ca)
            {
                var name = attr.AttributeType.FullName;
                // TODO: handle ConstructorArguments; those are marshaled into boxed form, we need to unbox them
                var attrArgumentTypes =
                    string.Join(", ", attr.ConstructorArguments.Select(x => x.Value));
                Console.WriteLine($"    attr[{name}]: {attrArgumentTypes}");
            }
        }
        
        Console.WriteLine($"detected {nodeCategory.Count} entries");
        var sorted = nodeCategory.OrderBy(x => x.Key);
        
        foreach (var (key, value) in sorted)
        {
            Console.WriteLine($"category '{key}' ({value.Count}): ");
            foreach (var typeReference in value)
            {
                Console.WriteLine("    " + typeReference.FullNameWithoutGenericArguments());
            }
        }
        Console.WriteLine();
    }
    
    private static string NameWithTypeVar(this TypeReference td)
    {
        var genericArgs = string.Join(", ", td.GenericParameters.ToArray().Select(x => x.FullName));
        var typeVars = genericArgs == "" ? "" : $"<{genericArgs}>";
        return td.FullName + typeVars;
    }

    private static bool IsExposedToProtoFluxUsers(this FieldDefinition fd)
    {
        return fd is { IsPublic: true, IsStatic: false } && fd.FieldType.IsProtoFluxInput();
    }

    private static bool IsProtoFluxInput(this TypeReference td)
    {
        // notify "`1": this is important because .NET can define multiple member with same name and different type-var arity.
        return td.FullNameWithoutGenericArguments() is "ProtoFlux.Core.ValueArgument`1" or "ProtoFlux.Core.ObjectArgument`1" or "ProtoFlux.Core.ObjectArgumentList`1";
    }

    private static string FullNameWithoutGenericArguments(this TypeReference td)
    {
        if (td.IsGenericParameter)
        {
            return td.Name;
        }
        
        return td.Namespace + "." + td.Name;
    }
    
    // TODO: discover output
    // TODO: how can we handle the Continuation and async?
    // TODO: should we handle those (ugly) FrooxEngine.ProtoFlux.CoreNodes (which has different bound on each type)?
}

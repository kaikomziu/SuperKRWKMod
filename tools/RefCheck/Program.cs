// Verifies every type/member referenced by a mod DLL exists in the game's (stripped) Managed assemblies.
// Usage: dotnet run --project tools/RefCheck -- <mod.dll> <BongoCat_Data/Managed>
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var modPath = args[0];
var managedDir = args[1];

// Index: "Assembly|Namespace.Type" -> set of member names (with param count for methods)
var defs = new Dictionary<string, HashSet<string>>();
foreach (var dll in Directory.GetFiles(managedDir, "*.dll"))
{
    try
    {
        using var pe = new PEReader(File.OpenRead(dll));
        if (!pe.HasMetadata) continue;
        var md = pe.GetMetadataReader();
        if (!md.IsAssembly) continue;
        var asm = md.GetString(md.GetAssemblyDefinition().Name);
        foreach (var th in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(th);
            var name = TypeDefName(md, td);
            var set = new HashSet<string>();
            foreach (var mh in td.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                var sig = m.DecodeSignature(new CountProvider(), null);
                set.Add(md.GetString(m.Name) + "/" + sig.ParameterTypes.Length);
            }
            foreach (var fh in td.GetFields()) set.Add(md.GetString(md.GetFieldDefinition(fh).Name));
            defs[asm + "|" + name] = set;
        }
        // Type forwarders
        foreach (var eh in md.ExportedTypes)
        {
            var et = md.GetExportedType(eh);
            if (et.Implementation.Kind == HandleKind.AssemblyReference)
            {
                var target = md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)et.Implementation).Name);
                var n = Join(md.GetString(et.Namespace), md.GetString(et.Name));
                defs[asm + "|" + n] = new HashSet<string> { "->" + target };
            }
        }
    }
    catch { }
}

HashSet<string> Lookup(string asm, string type)
{
    for (int i = 0; i < 5; i++)
    {
        if (!defs.TryGetValue(asm + "|" + type, out var s)) return null;
        var fwd = s.FirstOrDefault(x => x.StartsWith("->"));
        if (fwd == null) return s;
        asm = fwd.Substring(2);
    }
    return null;
}

using var modPe = new PEReader(File.OpenRead(modPath));
var mod = modPe.GetMetadataReader();
int problems = 0, checkedCount = 0;
foreach (var th in mod.TypeReferences)
{
    var (asm, type) = ResolveTypeRef(mod, th);
    if (asm == null) continue;
    checkedCount++;
    // Attribute types used only as assembly/compiler metadata are never loaded by the game.
    if (type.EndsWith("Attribute") || type.Contains("Attribute/")) continue;
    if (Lookup(asm, type) == null) { Console.WriteLine($"MISSING TYPE   [{asm}] {type}"); problems++; }
}
foreach (var mh in mod.MemberReferences)
{
    var mr = mod.GetMemberReference(mh);
    string asm = null, type = null;
    if (mr.Parent.Kind == HandleKind.TypeReference) (asm, type) = ResolveTypeRef(mod, (TypeReferenceHandle)mr.Parent);
    else if (mr.Parent.Kind == HandleKind.TypeSpecification)
    {
        var ts = mod.GetTypeSpecification((TypeSpecificationHandle)mr.Parent);
        var br = mod.GetBlobReader(ts.Signature);
        if (br.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
        {
            br.ReadSignatureTypeCode();
            var h = br.ReadTypeHandle();
            if (h.Kind == HandleKind.TypeReference) (asm, type) = ResolveTypeRef(mod, (TypeReferenceHandle)h);
        }
    }
    if (asm == null) continue;
    var members = Lookup(asm, type);
    if (members == null) continue; // already reported as missing type
    var name = mod.GetString(mr.Name);
    string key = name;
    if (mr.GetKind() == MemberReferenceKind.Method)
        key = name + "/" + mr.DecodeMethodSignature(new CountProvider(), null).ParameterTypes.Length;
    checkedCount++;
    if (!members.Contains(key)) { Console.WriteLine($"MISSING MEMBER [{asm}] {type}::{key}"); problems++; }
}
Console.WriteLine($"Checked {checkedCount} references, {problems} problem(s).");
return problems == 0 ? 0 : 1;

static string Join(string ns, string n) => string.IsNullOrEmpty(ns) ? n : ns + "." + n;

static string TypeDefName(MetadataReader md, TypeDefinition td)
{
    var name = Join(md.GetString(td.Namespace), md.GetString(td.Name));
    var decl = td.GetDeclaringType();
    return decl.IsNil ? name : TypeDefName(md, md.GetTypeDefinition(decl)) + "/" + md.GetString(td.Name);
}

static (string, string) ResolveTypeRef(MetadataReader md, TypeReferenceHandle h)
{
    var tr = md.GetTypeReference(h);
    var name = Join(md.GetString(tr.Namespace), md.GetString(tr.Name));
    switch (tr.ResolutionScope.Kind)
    {
        case HandleKind.AssemblyReference:
            return (md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)tr.ResolutionScope).Name), name);
        case HandleKind.TypeReference:
            var (a, outer) = ResolveTypeRef(md, (TypeReferenceHandle)tr.ResolutionScope);
            return (a, outer + "/" + md.GetString(tr.Name));
        default:
            return (null, null);
    }
}

class CountProvider : ISignatureTypeProvider<int, object>
{
    public int GetArrayType(int e, ArrayShape s) => 0; public int GetByReferenceType(int e) => 0;
    public int GetFunctionPointerType(MethodSignature<int> s) => 0; public int GetGenericInstantiation(int g, System.Collections.Immutable.ImmutableArray<int> a) => 0;
    public int GetGenericMethodParameter(object c, int i) => 0; public int GetGenericTypeParameter(object c, int i) => 0;
    public int GetModifiedType(int m, int u, bool r) => 0; public int GetPinnedType(int e) => 0; public int GetPointerType(int e) => 0;
    public int GetPrimitiveType(PrimitiveTypeCode t) => 0; public int GetSZArrayType(int e) => 0;
    public int GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => 0;
    public int GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => 0;
    public int GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => 0;
}

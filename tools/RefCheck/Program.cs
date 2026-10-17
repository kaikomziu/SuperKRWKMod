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
            var name = Meta.TypeDefName(md, td);
            var set = new HashSet<string>();
            foreach (var mh in td.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                var sig = m.DecodeSignature(new NameProvider(), null);
                set.Add(MethodKey(md.GetString(m.Name), sig));
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
                var n = Meta.Join(md.GetString(et.Namespace), md.GetString(et.Name));
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
    var (asm, type) = Meta.ResolveTypeRef(mod, th);
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
    if (mr.Parent.Kind == HandleKind.TypeReference) (asm, type) = Meta.ResolveTypeRef(mod, (TypeReferenceHandle)mr.Parent);
    else if (mr.Parent.Kind == HandleKind.TypeSpecification)
    {
        var ts = mod.GetTypeSpecification((TypeSpecificationHandle)mr.Parent);
        var br = mod.GetBlobReader(ts.Signature);
        if (br.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
        {
            br.ReadSignatureTypeCode();
            var h = br.ReadTypeHandle();
            if (h.Kind == HandleKind.TypeReference) (asm, type) = Meta.ResolveTypeRef(mod, (TypeReferenceHandle)h);
        }
    }
    if (asm == null) continue;
    var members = Lookup(asm, type);
    if (members == null) continue; // already reported as missing type
    var name = mod.GetString(mr.Name);
    string key = name;
    if (mr.GetKind() == MemberReferenceKind.Method)
        key = MethodKey(name, mr.DecodeMethodSignature(new NameProvider(), null));
    checkedCount++;
    if (!members.Contains(key)) { Console.WriteLine($"MISSING MEMBER [{asm}] {type}::{key}"); problems++; }
}
Console.WriteLine($"Checked {checkedCount} references, {problems} problem(s).");
return problems == 0 ? 0 : 1;

// Method identity = name + parameter types, so an overload with the same parameter count but
// different types (e.g. StackTrace(bool) vs StackTrace(int)) is not mistaken for the one used.
static string MethodKey(string name, MethodSignature<string> sig) => name + "(" + string.Join(",", sig.ParameterTypes) + ")";

class NameProvider : ISignatureTypeProvider<string, object>
{
    public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
    public string GetByReferenceType(string e) => e + "&";
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
    public string GetGenericInstantiation(string g, System.Collections.Immutable.ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object c, int i) => "!" + i;
    public string GetModifiedType(string m, string u, bool r) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
    public string GetSZArrayType(string e) => e + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => Meta.TypeDefName(r, r.GetTypeDefinition(h));
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => Meta.ResolveTypeRef(r, h).Item2 ?? "?";
    public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => "spec";
}

static class Meta
{
    public static string Join(string ns, string n) => string.IsNullOrEmpty(ns) ? n : ns + "." + n;

    public static string TypeDefName(MetadataReader md, TypeDefinition td)
    {
        var name = Join(md.GetString(td.Namespace), md.GetString(td.Name));
        var decl = td.GetDeclaringType();
        return decl.IsNil ? name : TypeDefName(md, md.GetTypeDefinition(decl)) + "/" + md.GetString(td.Name);
    }

    public static (string, string) ResolveTypeRef(MetadataReader md, TypeReferenceHandle h)
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
}


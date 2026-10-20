using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

static class Transplant {
 public static void Run(string official,string donor,string output) {
  using var target=ModuleDefMD.Load(official);
  using var source=ModuleDefMD.Load(donor);
  var mapper=new Mapper(target,source);
  var importer=new Importer(target,ImporterOptions.TryToUseDefs,default,mapper);
  string[] names={"PCL.PageDownloadInstall::LoaderInit", "PCL.ModModpack::ModpackInstall", "PCL.PageDownloadModify::LoadInstanceFinished", "PCL.PageDownloadResourceDetail::InitializeStackResourceVersions"};
  var candidates=source.GetTypes().SelectMany(t=>t.Methods).Where(m=>names.Contains(m.DeclaringType.FullName+"::"+m.Name)).ToList();
  if(candidates.Count!=5)throw new Exception("Expected 5 methods, got "+candidates.Count);
  foreach(var from in candidates) {
   if(!mapper.Methods.TryGetValue(from.FullName,out var existing) || !existing.HasBody)
    throw new Exception("Incompatible 2.99.0.0 method structure: "+from.FullName+". Original file was not replaced.");
  }
  foreach(var from in candidates){
   var to=mapper.Methods[from.FullName];
   var body=new CilBody { InitLocals=from.Body.InitLocals,MaxStack=from.Body.MaxStack,KeepOldMaxStack=true };
   var locals=new Dictionary<Local,Local>();
   foreach(var l in from.Body.Variables){var n=new Local(importer.Import(l.Type));locals.Add(l,n);body.Variables.Add(n);}
   var instructions=from.Body.Instructions.ToDictionary(i=>i,i=>new Instruction(i.OpCode));
   foreach(var old in from.Body.Instructions){
    object? operand=old.Operand;
    operand=operand switch {
     Instruction ins=>instructions[ins],
     IList<Instruction> list=>list.Select(i=>instructions[i]).ToArray(),
     Local l=>locals[l],
     Parameter p=>to.Parameters[p.Index],
     ITypeDefOrRef t=>importer.Import(t),
     IMethod m=>importer.Import(m),
     IField f=>importer.Import(f),
     MethodSig sig=>importer.Import(sig),
     _=>operand
    };
    instructions[old].Operand=operand;body.Instructions.Add(instructions[old]);
   }
   Instruction? Get(Instruction? i)=>i==null?null:instructions[i];
   foreach(var eh in from.Body.ExceptionHandlers)body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType){TryStart=Get(eh.TryStart),TryEnd=Get(eh.TryEnd),HandlerStart=Get(eh.HandlerStart),HandlerEnd=Get(eh.HandlerEnd),FilterStart=Get(eh.FilterStart),CatchType=eh.CatchType==null?null:importer.Import(eh.CatchType)});
   to.Body=body;
   Console.WriteLine("RESTORED "+to.FullName);
  }
  var options=new ModuleWriterOptions(target);
  options.MetadataOptions.Flags|=MetadataFlags.PreserveAll;
  target.Write(output,options);
 }
 sealed class Mapper : ImportMapper {
  readonly ModuleDef source;
  readonly Dictionary<string,TypeDef> types;
  public readonly Dictionary<string,MethodDef> Methods;
  readonly Dictionary<string,FieldDef> fields;
  public Mapper(ModuleDef target,ModuleDef source){this.source=source;types=target.GetTypes().ToDictionary(t=>t.FullName);Methods=target.GetTypes().SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);fields=target.GetTypes().SelectMany(t=>t.Fields).ToDictionary(f=>f.FullName);}
  public override ITypeDefOrRef? Map(ITypeDefOrRef t){if(types.TryGetValue(t.FullName,out var result))return result;if(t is TypeDef td && td.Module==source)throw new Exception("Missing local type "+t.FullName);return null;}
  public override IMethod? Map(MethodDef m){if(m.Module!=source)return null;return Methods.TryGetValue(m.FullName,out var result)?result:throw new Exception("Missing local method "+m.FullName);}
  public override IField? Map(FieldDef f){if(f.Module!=source)return null;return fields.TryGetValue(f.FullName,out var result)?result:throw new Exception("Missing local field "+f.FullName);}
 }
}

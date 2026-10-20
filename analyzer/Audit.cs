using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Security.Cryptography;
static class Audit {
 public static void Run(string original,string patched){
  using var a=ModuleDefMD.Load(original);using var b=ModuleDefMD.Load(patched);
  var aa=a.GetTypes().SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);var bb=b.GetTypes().SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);
  if(aa.Count!=bb.Count)throw new Exception("Method count changed");
  var expected=new HashSet<string>{"PCL.ModModpack::ModpackInstall","PCL.PageDownloadInstall::LoaderInit","PCL.PageDownloadModify::LoadInstanceFinished","PCL.PageDownloadResourceDetail::InitializeStackResourceVersions"};
  int changed=0;
  foreach(var item in aa){var right=bb[item.Key];if(Render(item.Value)==Render(right))continue;if(!expected.Contains(item.Value.DeclaringType.FullName+"::"+item.Value.Name))throw new Exception("Unexpected change: "+item.Key);changed++;Console.WriteLine("CHANGED "+item.Key);}
  if(changed>5)throw new Exception("Unexpected changed method count: "+changed);
  if(a.Resources.Count!=b.Resources.Count)throw new Exception("Resource count changed");
  foreach(var r in a.Resources.OfType<EmbeddedResource>()){
   var s=b.Resources.OfType<EmbeddedResource>().Single(s=>s.Name==r.Name);
   if(!r.CreateReader().ToArray().SequenceEqual(s.CreateReader().ToArray()))throw new Exception("Resource changed: "+r.Name);
  }
  var bFields=b.GetTypes().SelectMany(t=>t.Fields).ToDictionary(f=>f.FullName);
  foreach(var field in a.GetTypes().SelectMany(t=>t.Fields)){
   var other=bFields[field.FullName];
   if(!Equals(field.Constant?.Value,other.Constant?.Value))throw new Exception("Field constant changed: "+field.FullName);
  }
  Console.WriteLine($"AUDIT PASS: {changed} target method bodies changed; embedded resources and field constants unchanged.");
 }
 static string Render(MethodDef m){
  if(!m.HasBody)return "NO BODY";
  var body=m.Body;int Id(Instruction? i)=>i==null?-1:body.Instructions.IndexOf(i);
  string Op(object? o)=>o switch {Instruction i=>"@"+Id(i),IList<Instruction> list=>string.Join(",",list.Select(Id)),Local l=>"L"+l.Index,Parameter p=>"P"+p.Index,_=>o?.ToString()??""};
  return body.InitLocals+"|"+string.Join("|",body.Variables.Select(l=>l.Type.FullName))+"|"+string.Join("|",body.Instructions.Select(i=>i.OpCode+":"+Op(i.Operand)))+"|"+string.Join("|",body.ExceptionHandlers.Select(e=>$"{e.HandlerType}:{Id(e.TryStart)}:{Id(e.TryEnd)}:{Id(e.HandlerStart)}:{Id(e.HandlerEnd)}:{Id(e.FilterStart)}:{e.CatchType}"));
 }
}

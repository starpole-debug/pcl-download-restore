using System.Security.Cryptography;
try {
 if(args.Length!=3)throw new Exception("Usage: PclRestore.exe original.exe restore-methods.dat output.exe");
 string Hash(string p){using var sha=SHA256.Create();using var f=File.OpenRead(p);return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","");}
 using(var original=dnlib.DotNet.ModuleDefMD.Load(args[0])) {
  if(original.Assembly?.Version!=new Version(2,99,0,0))throw new Exception("Only PCL 2.99.0.0 is supported. No files changed.");
 }
 if(Hash(args[1])!="40111EED22034E44EE0AAC1C1097DD2C87A556CF22E7C0C5EB24B846FD844810")throw new Exception("Restore payload checksum mismatch.");
 if(File.Exists(args[2]))throw new Exception("Output already exists.");
 Transplant.Run(args[0],args[1],args[2]);
 Audit.Run(args[0],args[2]);
 Environment.ExitCode=0;
} catch(Exception e){Console.Error.WriteLine(e.Message);Environment.ExitCode=1;}

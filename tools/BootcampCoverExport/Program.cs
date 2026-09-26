using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Rasa.ClientData;
if(args.Length != 3) throw new ArgumentException("usage: BootcampCoverExport <client-data-directory> <entity-meshes-csv> <output-cover-json>");
var data=args[0];
string Sha256(string path) { using var stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
var mapHash=Sha256(data+"/maps/adv_bootcamp/adv_bootcamp.map");
var archiveHash=Sha256(data+"/mesh06.glm");
if(mapHash!="2876982ba3473dcb57d5cc4a1b88d75710349a71948d85fafb499b2a8f5106f3" ||
   archiveHash!="bd5d20ea31f97e11fd41319f3d6d2ae52f20f1b3896663e7772a4057b04d71be")
 throw new InvalidDataException("Client map or mesh06.glm differs from the recovered 1.16.5.0 artifact; inspect provenance before exporting.");
var map=MapFile.Load(data+"/maps/adv_bootcamp/adv_bootcamp.map");
using var meshes=new MeshLibrary(data,args[1]);
var indices=new[]{1040,1041,1042,1045,1053,1056};
var objects=indices.Select(index=>
{
 var e=map.Entities[index]; meshes.TryGetMeshName(e.ClassId,out var name);var mesh=meshes.Get(name);
 var v=mesh.Vertices.Select(p=>Vector3.Transform(p*e.Scale,e.Rotation)+e.Position).ToArray();
 var t=Enumerable.Range(0,mesh.Triangles.Count/3).Select(i=>
 {
  var a=v[mesh.Triangles[i*3]];var b=v[mesh.Triangles[i*3+1]];var c=v[mesh.Triangles[i*3+2]];
  return new[]{a.X,a.Y,a.Z,b.X,b.Y,b.Z,c.X,c.Y,c.Z};
 }).ToArray();
 return new {index, class_id=e.ClassId, mesh=name, triangles=t};
}).ToArray();
var result=new {schema_version=1,map="adv_bootcamp",source_map_sha256=mapHash,source_archive_sha256=archiveHash,objects};
var path=args[2];
File.WriteAllText(path,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"wrote {path}: {objects.Length} objects, {objects.Sum(o=>o.triangles.Length)} triangles");

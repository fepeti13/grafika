using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace PirateShootingGame
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateFromObjFile(GL Gl, string path, float[] color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices = new();
            List<int[]> objFaces = new();
            List<float[]> objNormals = new();
            List<float[]> faceColors = new(); 

            string currentMaterial = "";
            Dictionary<string, float[]> materialColors = new()
            {
                {"default", color},
                {"Trank_bark", new float[] {0.4f, 0.2f, 0.1f, 1f}}, 
                {"polySurface1SG1", new float[] {0.1f, 0.5f, 0.1f, 1f}}, 
                {"14052PirateShipmateMuscular_cloth", new float[] {0.6f, 0.3f, 0.1f, 1f}}, 
                {"14052PirateShipmateMuscular_body", new float[] {0.9f, 0.7f, 0.5f, 1f}}, 
                {"14053_Pirate_Shipmate_Old", new float[] {0.8f, 0.6f, 0.4f, 1f}}, 
                
                {"14053PirateShipmateOld", new float[] {0.8f, 0.6f, 0.4f, 1f}},
                {"Material__25", new float[] {0.8f, 0.6f, 0.4f, 1f}}
            };

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    switch (parts[0])
                    {
                        case "v":
                            if (parts.Length >= 4)
                            {
                                objVertices.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                            }
                            break;
                        case "vn":
                            if (parts.Length >= 4)
                            {
                                objNormals.Add(parts.Skip(1).Take(3).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray());
                            }
                            break;
                        case "usemtl":
                            if (parts.Length >= 2)
                            {
                                currentMaterial = parts[1];
                                
                                if (currentMaterial.ToLower().Contains("ground") || 
                                    currentMaterial.ToLower().Contains("plane") ||
                                    currentMaterial.ToLower().Contains("floor"))
                                {
                                    Console.WriteLine($"Skipping ground material: {currentMaterial}");
                                }
                            }
                            break;
                        case "f":
                            if (parts.Length >= 4)
                            {
                                
                                if (currentMaterial.ToLower().Contains("ground") || 
                                    currentMaterial.ToLower().Contains("plane") ||
                                    currentMaterial.ToLower().Contains("floor"))
                                {
                                    continue; 
                                }

                                
                                var faceColor = materialColors.ContainsKey(currentMaterial) 
                                    ? materialColors[currentMaterial] 
                                    : color;

                                
                                List<int> vertexIndices = new List<int>();
                                
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    if (parts[i].Contains("/"))
                                    {
                                        
                                        var tokens = parts[i].Split('/');
                                        if (tokens.Length >= 1 && int.TryParse(tokens[0], out int vertexIndex))
                                        {
                                            vertexIndices.Add(vertexIndex);
                                        }
                                    }
                                    else
                                    {
                                        
                                        if (int.TryParse(parts[i], out int vertexIndex))
                                        {
                                            vertexIndices.Add(vertexIndex);
                                        }
                                    }
                                }
                                
                                
                                if (vertexIndices.Count >= 3)
                                {
                                    for (int i = 1; i < vertexIndices.Count - 1; i++)
                                    {
                                        objFaces.Add(new int[] { vertexIndices[0], vertexIndices[i], vertexIndices[i + 1] });
                                        faceColors.Add(faceColor); 
                                    }
                                }
                            }
                            break;
                        
                        case "mtllib":
                        case "g":
                        case "s":
                            
                            break;
                    }
                }
            }

            
            Console.WriteLine($"Loaded OBJ: {objVertices.Count} vertices, {objFaces.Count} faces");

            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromObjArrays(faceColors, objVertices, objFaces, objNormals, glVertices, glColors, glIndices);
            
            if (glIndices.Count == 0)
            {
                Console.WriteLine("Warning: No valid geometry found in OBJ file");
                
                return GlCube.CreateCubeWithFaceColors(Gl, color, color, color, color, color, color);
            }

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            return new GlObject(vao, vertices, colors, indices, (uint)glIndices.Count, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(List<float[]> faceColors, List<float[]> objVertices, List<int[]> objFaces, List<float[]> objNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new();

            for (int faceIndex = 0; faceIndex < objFaces.Count; faceIndex++)
            {
                var face = objFaces[faceIndex];
                var faceColor = faceIndex < faceColors.Count ? faceColors[faceIndex] : new float[] {0.8f, 0.4f, 0.2f, 1f};
                
                
                if (face.All(index => index > 0 && index <= objVertices.Count))
                {
                    var aObjVertex = objVertices[face[0] - 1];
                    var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                    var bObjVertex = objVertices[face[1] - 1];
                    var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                    var cObjVertex = objVertices[face[2] - 1];
                    var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);
                    
                    var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));

                    for (int i = 0; i < 3; i++)
                    {
                        var v = objVertices[face[i] - 1];
                        var glVertex = new List<float>(v)
                        {
                            normal.X, normal.Y, normal.Z
                        };

                        string key = string.Join(" ", glVertex) + "_" + string.Join("", faceColor);
                        if (!glVertexIndices.ContainsKey(key))
                        {
                            glVertices.AddRange(glVertex);
                            glColors.AddRange(faceColor);
                            glVertexIndices[key] = glVertexIndices.Count;
                        }

                        glIndices.Add((uint)glVertexIndices[key]);
                    }
                }
            }
        }
    }
}
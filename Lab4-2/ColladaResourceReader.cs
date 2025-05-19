using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Xml;

namespace Szeminarium1_24_02_17_2
{
    internal class ColladaResourceReader
    {
         
        private class NodeTransform
        {
            public Matrix4X4<float> Matrix { get; set; } = Matrix4X4<float>.Identity;
            public string? GeometryId { get; set; }
            public List<NodeTransform> Children { get; set; } = new List<NodeTransform>();
        }

        public static unsafe GlObject CreateFromColladaFile(GL Gl, string path, float[] color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            Dictionary<string, (List<float[]> vertices, List<int[]> faces, List<float[]> normals)> geometries = new();
            List<NodeTransform> sceneNodes = new();

            ReadColladaData(path, geometries, sceneNodes);

             
             
            List<float[]> objVertices = new();
            List<int[]> objFaces = new();
            List<float[]> objNormals = new();

             
            foreach (var node in sceneNodes)
            {
                ProcessNode(node, Matrix4X4<float>.Identity, geometries, objVertices, objFaces, objNormals);
            }

            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromColladaArrays(color, objVertices, objFaces, objNormals, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static void ProcessNode(
    NodeTransform node, 
    Matrix4X4<float> parentTransform, 
    Dictionary<string, (List<float[]> vertices, List<int[]> faces, List<float[]> normals)> geometries,
    List<float[]> objVertices,
    List<int[]> objFaces,
    List<float[]> objNormals)
{
     
    var combinedTransform = node.Matrix * parentTransform;

     
    if (!string.IsNullOrEmpty(node.GeometryId) && geometries.ContainsKey(node.GeometryId))
    {
        var geometry = geometries[node.GeometryId];
        int vertexOffset = objVertices.Count;

         
        foreach (var vertex in geometry.vertices)
        {
            var v = new Vector4D<float>(vertex[0], vertex[1], vertex[2], 1.0f);
            var transformedV = Vector4D.Transform(v, combinedTransform);
            objVertices.Add(new float[] { transformedV.X, transformedV.Y, transformedV.Z });
        }

         
        foreach (var face in geometry.faces)
        {
            int[] adjustedFace = new int[face.Length];
            for (int i = 0; i < face.Length; i++)
            {
                adjustedFace[i] = face[i] + vertexOffset;
            }
            objFaces.Add(adjustedFace);
        }

         
         
        Matrix4X4<float> normalMatrix = combinedTransform;
        normalMatrix.M41 = normalMatrix.M42 = normalMatrix.M43 = 0;  
        
         
        Matrix4X4.Invert(normalMatrix, out var inverted);
        Matrix4X4<float> transposedInverted = Matrix4X4.Transpose(inverted);

        foreach (var normal in geometry.normals)
        {
             
            var n = new Vector4D<float>(normal[0], normal[1], normal[2], 0.0f);
            
             
            var transformedN = Vector4D.Transform(n, transposedInverted);
            
             
            var normalVec3 = new Vector3D<float>(transformedN.X, transformedN.Y, transformedN.Z);
            normalVec3 = Vector3D.Normalize(normalVec3);
            
            objNormals.Add(new float[] { normalVec3.X, normalVec3.Y, normalVec3.Z });
        }
    }

     
    foreach (var child in node.Children)
    {
        ProcessNode(child, combinedTransform, geometries, objVertices, objFaces, objNormals);
    }
}

        private static void ReadColladaData(
            string path, 
            Dictionary<string, (List<float[]> vertices, List<int[]> faces, List<float[]> normals)> geometries,
            List<NodeTransform> sceneNodes)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

             
            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("collada", "http://www.collada.org/2005/11/COLLADASchema");

             
            XmlNodeList? geometryNodes = doc.SelectNodes("//collada:geometry", nsManager);
            if (geometryNodes != null)
            {
                foreach (XmlNode geometryNode in geometryNodes)
                {
                    if (geometryNode.Attributes?["id"] != null)
                    {
                        string geometryId = geometryNode.Attributes["id"].Value;
                        var (vertices, faces, normals) = ReadGeometry(geometryNode, nsManager);
                        geometries[geometryId] = (vertices, faces, normals);
                    }
                }
            }

             
            XmlNode? sceneNode = doc.SelectSingleNode("//collada:scene/collada:instance_visual_scene", nsManager);
            if (sceneNode?.Attributes?["url"] != null)
            {
                string visualSceneUrl = sceneNode.Attributes["url"].Value.TrimStart('#');
                XmlNode? visualSceneNode = doc.SelectSingleNode($"//collada:visual_scene[@id='{visualSceneUrl}']", nsManager);
                
                if (visualSceneNode != null)
                {
                     
                    XmlNodeList? nodeElements = visualSceneNode.SelectNodes("./collada:node", nsManager);
                    if (nodeElements != null)
                    {
                        foreach (XmlNode nodeElement in nodeElements)
                        {
                            NodeTransform nodeTransform = ReadNodeTransform(nodeElement, nsManager);
                            sceneNodes.Add(nodeTransform);
                        }
                    }
                }
            }
            
             
            if (sceneNodes.Count == 0 && geometries.Count > 0)
            {
                foreach (var geometry in geometries)
                {
                    sceneNodes.Add(new NodeTransform 
                    { 
                        GeometryId = geometry.Key,
                        Matrix = Matrix4X4<float>.Identity
                    });
                }
            }
        }

        private static (List<float[]> vertices, List<int[]> faces, List<float[]> normals) ReadGeometry(XmlNode geometryNode, XmlNamespaceManager nsManager)
        {
            List<float[]> vertices = new List<float[]>();
            List<int[]> faces = new List<int[]>();
            List<float[]> normals = new List<float[]>();
            
             
            XmlNode? positionsSource = geometryNode.SelectSingleNode(".//collada:source[contains(@id, 'position') or contains(@id, 'positions') or contains(@id, 'vertex')]", nsManager);
            if (positionsSource == null)
            {
                 
                XmlNode? meshNode = geometryNode.SelectSingleNode(".//collada:mesh", nsManager);
                if (meshNode != null)
                {
                    XmlNode? positionInput = meshNode.SelectSingleNode(".//collada:input[@semantic='POSITION']", nsManager);
                    if (positionInput?.Attributes?["source"] != null)
                    {
                        string sourceId = positionInput.Attributes["source"].Value.TrimStart('#');
                        positionsSource = geometryNode.SelectSingleNode($".//collada:source[@id='{sourceId}']", nsManager);
                    }
                }
            }

            if (positionsSource != null)
            {
                XmlNode? floatArray = positionsSource.SelectSingleNode(".//collada:float_array", nsManager);
                if (floatArray != null)
                {
                    string[] values = floatArray.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                     
                    int stride = 3;
                    XmlNode? accessor = positionsSource.SelectSingleNode(".//collada:accessor", nsManager);
                    if (accessor?.Attributes?["stride"] != null)
                    {
                        stride = int.Parse(accessor.Attributes["stride"].Value);
                    }

                    for (int i = 0; i < values.Length; i += stride)
                    {
                        if (i + 2 < values.Length)  
                        {
                            float[] vertex = new float[3];
                            for (int j = 0; j < 3; j++)
                            {
                                vertex[j] = float.Parse(values[i + j], CultureInfo.InvariantCulture);
                            }
                            vertices.Add(vertex);
                        }
                    }
                }
            }

             
            XmlNode? normalsSource = geometryNode.SelectSingleNode(".//collada:source[contains(@id, 'normal')]", nsManager);
            if (normalsSource == null)
            {
                 
                XmlNode? meshNode = geometryNode.SelectSingleNode(".//collada:mesh", nsManager);
                if (meshNode != null)
                {
                    XmlNode? normalInput = meshNode.SelectSingleNode(".//collada:input[@semantic='NORMAL']", nsManager);
                    if (normalInput?.Attributes?["source"] != null)
                    {
                        string sourceId = normalInput.Attributes["source"].Value.TrimStart('#');
                        normalsSource = geometryNode.SelectSingleNode($".//collada:source[@id='{sourceId}']", nsManager);
                    }
                }
            }

            if (normalsSource != null)
            {
                XmlNode? floatArray = normalsSource.SelectSingleNode(".//collada:float_array", nsManager);
                if (floatArray != null)
                {
                    string[] values = floatArray.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                     
                    int stride = 3;
                    XmlNode? accessor = normalsSource.SelectSingleNode(".//collada:accessor", nsManager);
                    if (accessor?.Attributes?["stride"] != null)
                    {
                        stride = int.Parse(accessor.Attributes["stride"].Value);
                    }

                    for (int i = 0; i < values.Length; i += stride)
                    {
                        if (i + 2 < values.Length)
                        {
                            float[] normal = new float[3];
                            for (int j = 0; j < 3; j++)
                            {
                                normal[j] = float.Parse(values[i + j], CultureInfo.InvariantCulture);
                            }
                            normals.Add(normal);
                        }
                    }
                }
            }

             
            XmlNodeList? trianglesNodes = geometryNode.SelectNodes(".//collada:triangles | .//collada:polylist | .//collada:polygons", nsManager);
            if (trianglesNodes != null)
            {
                foreach (XmlNode triangleNode in trianglesNodes)
                {
                    XmlNode? pNode = triangleNode.SelectSingleNode(".//collada:p", nsManager);
                    if (pNode != null)
                    {
                        string[] indices = pNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        
                         
                        int stride = 1;
                        bool hasNormals = false;
                        Dictionary<string, int> semanticOffsets = new Dictionary<string, int>();
                        
                        XmlNodeList? inputs = triangleNode.SelectNodes(".//collada:input", nsManager);
                        if (inputs != null)
                        {
                            foreach (XmlNode input in inputs)
                            {
                                if (input.Attributes?["semantic"] != null && input.Attributes?["offset"] != null)
                                {
                                    string semantic = input.Attributes["semantic"].Value;
                                    int offset = int.Parse(input.Attributes["offset"].Value);
                                    semanticOffsets[semantic] = offset;
                                    stride = Math.Max(stride, offset + 1);
                                    
                                    if (semantic == "NORMAL")
                                    {
                                        hasNormals = true;
                                    }
                                }
                            }
                        }

                         
                        for (int i = 0; i < indices.Length; i += stride * 3)
                        {
                            if (i + (stride * 3) - 1 < indices.Length)
                            {
                                int[] face = new int[3];
                                for (int j = 0; j < 3; j++)
                                {
                                    int vertexOffset = semanticOffsets.ContainsKey("VERTEX") ? semanticOffsets["VERTEX"] : 0;
                                    int indexPos = i + (j * stride) + vertexOffset;
                                    if (indexPos < indices.Length)
                                    {
                                        face[j] = int.Parse(indices[indexPos]) + 1;  
                                    }
                                }
                                faces.Add(face);
                            }
                        }
                    }
                }
            }

             
            if (normals.Count == 0 && vertices.Count > 0 && faces.Count > 0)
            {
                CalculateNormals(vertices, faces, normals);
            }

            return (vertices, faces, normals);
        }

        private static NodeTransform ReadNodeTransform(XmlNode nodeElement, XmlNamespaceManager nsManager)
        {
            NodeTransform nodeTransform = new NodeTransform();
            
             
            XmlNode? instanceGeometry = nodeElement.SelectSingleNode(".//collada:instance_geometry", nsManager);
            if (instanceGeometry?.Attributes?["url"] != null)
            {
                string geometryUrl = instanceGeometry.Attributes["url"].Value.TrimStart('#');
                nodeTransform.GeometryId = geometryUrl;
            }
            else
            {
                 
                XmlNode? meshNode = nodeElement.SelectSingleNode(".//collada:mesh", nsManager);
                if (meshNode?.ParentNode?.Attributes?["id"] != null)
                {
                    nodeTransform.GeometryId = meshNode.ParentNode.Attributes["id"].Value;
                }
            }

             
            XmlNodeList? transformElements = nodeElement.SelectNodes("collada:matrix | collada:translate | collada:rotate | collada:scale", nsManager);
            if (transformElements != null)
            {
                foreach (XmlNode transformElement in transformElements)
                {
                    switch (transformElement.Name)
                    {
                        case "matrix":
                        case "collada:matrix":
                            nodeTransform.Matrix = ParseMatrixTransform(transformElement.InnerText);
                            break;
                            
                        case "translate":
                        case "collada:translate":
                            var translation = ParseVectorTransform(transformElement.InnerText);
                            nodeTransform.Matrix *= Matrix4X4.CreateTranslation(translation);
                            break;
                            
                        case "rotate":
                        case "collada:rotate":
                            var rotation = ParseRotationTransform(transformElement.InnerText);
                            nodeTransform.Matrix *= CreateRotationMatrix(rotation.Axis, rotation.Angle);
                            break;
                            
                        case "scale":
                        case "collada:scale":
                            var scale = ParseVectorTransform(transformElement.InnerText);
                            nodeTransform.Matrix *= Matrix4X4.CreateScale(scale);
                            break;
                    }
                }
            }

             
            XmlNodeList? childNodes = nodeElement.SelectNodes("collada:node", nsManager);
            if (childNodes != null)
            {
                foreach (XmlNode childNode in childNodes)
                {
                    NodeTransform childTransform = ReadNodeTransform(childNode, nsManager);
                    nodeTransform.Children.Add(childTransform);
                }
            }

            return nodeTransform;
        }

        private static Matrix4X4<float> ParseMatrixTransform(string matrixText)
        {
            string[] values = matrixText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 16)
            {
                float[] matrixValues = new float[16];
                for (int i = 0; i < 16; i++)
                {
                    matrixValues[i] = float.Parse(values[i], CultureInfo.InvariantCulture);
                }

                 
                return new Matrix4X4<float>(
                    matrixValues[0], matrixValues[4], matrixValues[8], matrixValues[12],
                    matrixValues[1], matrixValues[5], matrixValues[9], matrixValues[13],
                    matrixValues[2], matrixValues[6], matrixValues[10], matrixValues[14],
                    matrixValues[3], matrixValues[7], matrixValues[11], matrixValues[15]
                );
            }
            
            return Matrix4X4<float>.Identity;
        }

        private static Vector3D<float> ParseVectorTransform(string vectorText)
        {
            string[] values = vectorText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 3)
            {
                return new Vector3D<float>(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture),
                    float.Parse(values[2], CultureInfo.InvariantCulture)
                );
            }
            
            return Vector3D<float>.Zero;
        }

        private static (Vector3D<float> Axis, float Angle) ParseRotationTransform(string rotationText)
        {
            string[] values = rotationText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 4)
            {
                Vector3D<float> axis = new Vector3D<float>(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture),
                    float.Parse(values[2], CultureInfo.InvariantCulture)
                );
                
                float angle = float.Parse(values[3], CultureInfo.InvariantCulture) * (float)(Math.PI / 180.0);  
                return (axis, angle);
            }
            
            return (new Vector3D<float>(0, 1, 0), 0);  
        }

        private static Matrix4X4<float> CreateRotationMatrix(Vector3D<float> axis, float angle)
        {
             
            axis = Vector3D.Normalize(axis);
            
             
            if (Math.Abs(axis.X - 1) < 0.001f && Math.Abs(axis.Y) < 0.001f && Math.Abs(axis.Z) < 0.001f)
            {
                return Matrix4X4.CreateRotationX(angle);
            }
            else if (Math.Abs(axis.X) < 0.001f && Math.Abs(axis.Y - 1) < 0.001f && Math.Abs(axis.Z) < 0.001f)
            {
                return Matrix4X4.CreateRotationY(angle);
            }
            else if (Math.Abs(axis.X) < 0.001f && Math.Abs(axis.Y) < 0.001f && Math.Abs(axis.Z - 1) < 0.001f)
            {
                return Matrix4X4.CreateRotationZ(angle);
            }
            else
            {
                 
                float c = (float)Math.Cos(angle);
                float s = (float)Math.Sin(angle);
                float t = 1 - c;
                
                float x = axis.X;
                float y = axis.Y;
                float z = axis.Z;
                
                return new Matrix4X4<float>(
                    t*x*x + c,    t*x*y - s*z,  t*x*z + s*y,  0,
                    t*x*y + s*z,  t*y*y + c,    t*y*z - s*x,  0,
                    t*x*z - s*y,  t*y*z + s*x,  t*z*z + c,    0,
                    0,            0,            0,            1
                );
            }
        }

        private static void CalculateNormals(List<float[]> vertices, List<int[]> faces, List<float[]> normals)
        {
             
            for (int i = 0; i < vertices.Count; i++)
            {
                normals.Add(new float[] { 0, 0, 0 });
            }

             
            foreach (var face in faces)
            {
                if (face.Length >= 3)
                {
                    var a = new Vector3D<float>(vertices[face[0] - 1][0], vertices[face[0] - 1][1], vertices[face[0] - 1][2]);
                    var b = new Vector3D<float>(vertices[face[1] - 1][0], vertices[face[1] - 1][1], vertices[face[1] - 1][2]);
                    var c = new Vector3D<float>(vertices[face[2] - 1][0], vertices[face[2] - 1][1], vertices[face[2] - 1][2]);

                     
                    var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));

                     
                    for (int i = 0; i < 3; i++)
                    {
                        int vertexIndex = face[i] - 1;
                        if (vertexIndex < normals.Count)
                        {
                            normals[vertexIndex][0] += normal.X;
                            normals[vertexIndex][1] += normal.Y;
                            normals[vertexIndex][2] += normal.Z;
                        }
                    }
                }
            }

             
            for (int i = 0; i < normals.Count; i++)
            {
                float length = (float)Math.Sqrt(
                    normals[i][0] * normals[i][0] +
                    normals[i][1] * normals[i][1] +
                    normals[i][2] * normals[i][2]);

                if (length > 0.0001f)
                {
                    normals[i][0] /= length;
                    normals[i][1] /= length;
                    normals[i][2] /= length;
                }
            }
        }

        private static unsafe void CreateGlArraysFromColladaArrays(
            float[] faceColor,
            List<float[]> objVertices,
            List<int[]> objFaces,
            List<float[]> objNormals,
            List<float> glVertices,
            List<float> glColors,
            List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            foreach (var face in objFaces)
            {
                if (face.Length >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var vertexIndex = face[i] - 1;  
                        
                        if (vertexIndex < objVertices.Count)
                        {
                            var v = objVertices[vertexIndex];
                            var n = vertexIndex < objNormals.Count ? objNormals[vertexIndex] : new float[] { 0, 1, 0 };

                            var glVertex = new List<float>(v);
                            glVertex.AddRange(n);
                            
                            string key = string.Join(" ", glVertex);

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

        private static unsafe GlObject CreateOpenGlObject(
            GL Gl,
            uint vao,
            List<float> glVertices,
            List<float> glColors,
            List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, glVertices.ToArray(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, glColors.ToArray(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData<uint>(GLEnum.ElementArrayBuffer, glIndices.ToArray(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            return new GlObject(vao, vertices, colors, indices, (uint)glIndices.Count, Gl);
        }
    }
}